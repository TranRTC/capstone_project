namespace IoTMonitoringSystem.API.Mcp
{
    /// <summary>
    /// Optional API-key gate for the MCP HTTP endpoint (separate from dashboard JWT).
    /// </summary>
    public class McpApiKeyMiddleware
    {
        private const string ApiKeyHeader = "X-Mcp-Api-Key";
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<McpApiKeyMiddleware> _logger;

        public McpApiKeyMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            ILogger<McpApiKeyMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_configuration.GetValue("Mcp:RequireApiKey", true))
            {
                await _next(context);
                return;
            }

            var configuredKey = _configuration["Mcp:ApiKey"];
            if (string.IsNullOrWhiteSpace(configuredKey))
            {
                _logger.LogWarning("Mcp:RequireApiKey is true but Mcp:ApiKey is not set. Rejecting MCP request.");
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync("MCP API key is not configured on the server.");
                return;
            }

            var providedKey = context.Request.Headers[ApiKeyHeader].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(providedKey))
            {
                var auth = context.Request.Headers.Authorization.FirstOrDefault();
                if (auth?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                    providedKey = auth["Bearer ".Length..].Trim();
            }

            if (!string.Equals(providedKey, configuredKey, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid or missing MCP API key.");
                return;
            }

            await _next(context);
        }
    }
}
