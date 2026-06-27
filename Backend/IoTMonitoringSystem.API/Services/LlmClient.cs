using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IoTMonitoringSystem.API.Services
{
    public class LlmClient : ILlmClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LlmClient> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public LlmClient(HttpClient httpClient, IConfiguration configuration, ILogger<LlmClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ResolveApiKey());

        public async Task<LlmChatResult> CompleteAsync(
            IReadOnlyList<LlmMessage> messages,
            IReadOnlyList<LlmToolDefinition> tools,
            CancellationToken cancellationToken = default)
        {
            var apiKey = ResolveApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "The AI assistant is not configured. For local dev, install Ollama and use appsettings.Development.json, " +
                    "or set Agent:Llm:ApiKey / OPENAI_API_KEY for cloud providers.");

            var model = _configuration["Agent:Model"] ?? "llama3.2";
            var baseUrl = _configuration["Agent:Llm:BaseUrl"] ?? "http://localhost:11434/v1";
            var timeoutSeconds = _configuration.GetValue("Agent:RequestTimeoutSeconds", 60);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var requestBody = new OpenAiChatRequest
            {
                Model = model,
                Messages = messages.Select(MapToApiMessage).ToList(),
                Tools = tools.Select(t => new OpenAiTool
                {
                    Type = "function",
                    Function = new OpenAiFunction
                    {
                        Name = t.Name,
                        Description = t.Description,
                        Parameters = t.ParametersSchema
                    }
                }).ToList()
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cts.Token);
            }
            catch (HttpRequestException ex) when (IsOllamaEndpoint(baseUrl))
            {
                throw new InvalidOperationException(
                    "Cannot reach Ollama at http://localhost:11434. Install from https://ollama.com, run `ollama pull llama3.2`, and ensure Ollama is running.",
                    ex);
            }

            using (response)
            {
            var responseJson = await response.Content.ReadAsStringAsync(cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("LLM API error {StatusCode}: {Body}", (int)response.StatusCode, Truncate(responseJson, 500));

                var hint = IsOllamaEndpoint(baseUrl)
                    ? "Check that Ollama is running, the model is pulled (`ollama pull llama3.2`), and Agent:Model matches."
                    : "Check Agent:Llm:ApiKey and model configuration.";
                throw new InvalidOperationException($"LLM API returned {(int)response.StatusCode}. {hint}");
            }

            var parsed = JsonSerializer.Deserialize<OpenAiChatResponse>(responseJson, JsonOptions)
                ?? throw new InvalidOperationException("LLM API returned an empty response.");

            var choice = parsed.Choices?.FirstOrDefault()?.Message
                ?? throw new InvalidOperationException("LLM API returned no choices.");

            var toolCalls = choice.ToolCalls?
                .Select(tc => new LlmToolCall
                {
                    Id = tc.Id ?? string.Empty,
                    Name = tc.Function?.Name ?? string.Empty,
                    ArgumentsJson = string.IsNullOrWhiteSpace(tc.Function?.Arguments) ? "{}" : tc.Function!.Arguments!
                })
                .Where(tc => !string.IsNullOrWhiteSpace(tc.Name))
                .ToList()
                ?? new List<LlmToolCall>();

            return new LlmChatResult
            {
                Content = choice.Content,
                ToolCalls = toolCalls
            };
            }
        }

        private static OpenAiMessage MapToApiMessage(LlmMessage message)
        {
            var apiMessage = new OpenAiMessage
            {
                Role = message.Role,
                Content = message.Content,
                ToolCallId = message.ToolCallId
            };

            if (message.ToolCalls is { Count: > 0 })
            {
                apiMessage.ToolCalls = message.ToolCalls.Select(tc => new OpenAiToolCall
                {
                    Id = tc.Id,
                    Type = "function",
                    Function = new OpenAiFunctionCall
                    {
                        Name = tc.Name,
                        Arguments = tc.ArgumentsJson
                    }
                }).ToList();
            }

            return apiMessage;
        }

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value[..max] + "...";

        private string? GetApiKey() =>
            _configuration["Agent:Llm:ApiKey"]
            ?? Environment.GetEnvironmentVariable("Agent__Llm__ApiKey")
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        private string? ResolveApiKey()
        {
            var key = GetApiKey();
            if (!string.IsNullOrWhiteSpace(key))
                return key;

            return IsOllamaEndpoint(_configuration["Agent:Llm:BaseUrl"]) ? "ollama" : null;
        }

        private static bool IsOllamaEndpoint(string? baseUrl) =>
            !string.IsNullOrWhiteSpace(baseUrl) &&
            (baseUrl.Contains("localhost:11434", StringComparison.OrdinalIgnoreCase) ||
             baseUrl.Contains("127.0.0.1:11434", StringComparison.OrdinalIgnoreCase));

        private sealed class OpenAiChatRequest
        {
            public string Model { get; set; } = string.Empty;
            public List<OpenAiMessage> Messages { get; set; } = new();
            public List<OpenAiTool> Tools { get; set; } = new();
        }

        private sealed class OpenAiChatResponse
        {
            public List<OpenAiChoice>? Choices { get; set; }
        }

        private sealed class OpenAiChoice
        {
            public OpenAiMessage? Message { get; set; }
        }

        private sealed class OpenAiMessage
        {
            public string Role { get; set; } = string.Empty;
            public string? Content { get; set; }
            public string? ToolCallId { get; set; }
            public List<OpenAiToolCall>? ToolCalls { get; set; }
        }

        private sealed class OpenAiTool
        {
            public string Type { get; set; } = "function";
            public OpenAiFunction Function { get; set; } = new();
        }

        private sealed class OpenAiFunction
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public object Parameters { get; set; } = new();
        }

        private sealed class OpenAiToolCall
        {
            public string? Id { get; set; }
            public string Type { get; set; } = "function";
            public OpenAiFunctionCall? Function { get; set; }
        }

        private sealed class OpenAiFunctionCall
        {
            public string? Name { get; set; }
            public string? Arguments { get; set; }
        }
    }
}
