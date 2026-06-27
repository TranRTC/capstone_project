using System.Text.Json;
using System.Text.RegularExpressions;

namespace IoTMonitoringSystem.API.Services
{
    /// <summary>
    /// Some local models (e.g. llama3.2 via Ollama) emit tool invocations as plain text
    /// instead of structured tool_calls. This parser recovers them so the agent loop can run.
    /// </summary>
    public static partial class AgentToolCallParser
    {
        private static readonly Regex NameRegex = NamePattern();
        private static readonly Regex DeviceIdRegex = DeviceIdPattern();
        private static readonly Regex HoursRegex = HoursPattern();

        public static bool LooksLikeToolCallText(string? content) =>
            !string.IsNullOrWhiteSpace(content) &&
            (content.Contains("\"name\"", StringComparison.Ordinal) ||
             content.Contains("get_device", StringComparison.OrdinalIgnoreCase) ||
             content.Contains("get_devices", StringComparison.OrdinalIgnoreCase) ||
             content.Contains("get_actuators", StringComparison.OrdinalIgnoreCase) ||
             content.Contains("parameters", StringComparison.OrdinalIgnoreCase) ||
             content.Contains("arguments", StringComparison.OrdinalIgnoreCase));

        public static IReadOnlyList<LlmToolCall> TryParseFromContent(
            string? content,
            IReadOnlyCollection<string> knownToolNames)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Array.Empty<LlmToolCall>();

            var known = new HashSet<string>(knownToolNames, StringComparer.Ordinal);
            var results = new List<LlmToolCall>();
            var segments = content.Contains(';')
                ? content.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                : new[] { content };

            foreach (var segment in segments)
            {
                var call = TryParseSegment(segment, known);
                if (call is not null)
                    results.Add(call);
            }

            if (results.Count == 0)
                results.AddRange(TryParseWithRegex(content, known));

            return Deduplicate(results);
        }

        private static LlmToolCall? TryParseSegment(string segment, HashSet<string> knownToolNames)
        {
            var json = NormalizeMalformedToolJson(segment);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            if (!json.StartsWith('{'))
                json = '{' + json;
            if (!json.EndsWith('}'))
                json += '}';

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (!root.TryGetProperty("name", out var nameProp))
                    return null;

                var name = nameProp.GetString();
                if (string.IsNullOrWhiteSpace(name) || !knownToolNames.Contains(name))
                    return null;

                return CreateCall(name, ExtractArgumentsJson(root));
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static IEnumerable<LlmToolCall> TryParseWithRegex(string content, HashSet<string> knownToolNames)
        {
            foreach (Match match in NameRegex.Matches(content))
            {
                var name = match.Groups["name"].Value;
                if (!knownToolNames.Contains(name))
                    continue;

                yield return CreateCall(name, BuildArgumentsJson(content));
            }
        }

        private static string BuildArgumentsJson(string content)
        {
            var deviceMatch = DeviceIdRegex.Match(content);
            var hoursMatch = HoursRegex.Match(content);

            if (!deviceMatch.Success)
                return "{}";

            if (hoursMatch.Success)
            {
                return $"{{\"deviceId\":{deviceMatch.Groups["id"].Value},\"hours\":{hoursMatch.Groups["h"].Value}}}";
            }

            return $"{{\"deviceId\":{deviceMatch.Groups["id"].Value}}}";
        }

        private static LlmToolCall CreateCall(string name, string argumentsJson) =>
            new()
            {
                Id = $"call_{Guid.NewGuid():N}"[..20],
                Name = name,
                ArgumentsJson = string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson
            };

        private static string NormalizeMalformedToolJson(string segment)
        {
            var json = segment.Trim().Trim(',');
            if (string.IsNullOrWhiteSpace(json))
                return json;

            json = ParametersPattern().Replace(json, "\"parameters\":{");
            json = ArgumentsPattern().Replace(json, "\"arguments\":{");
            json = TrailingQuotePattern().Replace(json, "}}");
            return json;
        }

        private static string ExtractArgumentsJson(JsonElement root)
        {
            if (root.TryGetProperty("arguments", out var arguments))
                return arguments.ValueKind == JsonValueKind.String
                    ? arguments.GetString() ?? "{}"
                    : arguments.GetRawText();

            if (root.TryGetProperty("parameters", out var parameters))
                return parameters.ValueKind == JsonValueKind.String
                    ? parameters.GetString() ?? "{}"
                    : parameters.GetRawText();

            return "{}";
        }

        private static List<LlmToolCall> Deduplicate(List<LlmToolCall> calls)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var unique = new List<LlmToolCall>();
            foreach (var call in calls)
            {
                var key = $"{call.Name}:{call.ArgumentsJson}";
                if (seen.Add(key))
                    unique.Add(call);
            }

            return unique;
        }

        [GeneratedRegex(@"""name""\s*:\s*""(?<name>[^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex NamePattern();

        [GeneratedRegex(@"deviceId""?\s*:\s*(?<id>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex DeviceIdPattern();

        [GeneratedRegex(@"hours""?\s*:\s*(?<h>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex HoursPattern();

        [GeneratedRegex(@"""parameters""?\s*\{", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex ParametersPattern();

        [GeneratedRegex(@"""arguments""?\s*\{", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex ArgumentsPattern();

        [GeneratedRegex(@"\}""\s*\}$", RegexOptions.Compiled)]
        private static partial Regex TrailingQuotePattern();
    }
}
