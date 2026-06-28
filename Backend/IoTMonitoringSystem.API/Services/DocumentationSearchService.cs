namespace IoTMonitoringSystem.API.Services
{
    public sealed class DocumentationChunk
    {
        public string SourcePath { get; init; } = string.Empty;
        public string Heading { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
    }

    public sealed class DocumentationSearchResult
    {
        public string SourcePath { get; init; } = string.Empty;
        public string Heading { get; init; } = string.Empty;
        public string Excerpt { get; init; } = string.Empty;
        public double Score { get; init; }
    }

    public interface IDocumentationSearchService
    {
        bool IsEnabled { get; }
        int ChunkCount { get; }
        IReadOnlyList<DocumentationSearchResult> Search(string query, int? maxResults = null);
    }

    public class DocumentationSearchService : IDocumentationSearchService
    {
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "and", "are", "as", "at", "be", "by", "do", "for", "from", "how", "i", "in", "is",
            "it", "of", "on", "or", "the", "to", "what", "when", "where", "which", "with"
        };

        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DocumentationSearchService> _logger;
        private readonly object _indexLock = new();
        private List<DocumentationChunk> _chunks = new();
        private bool _indexed;

        public DocumentationSearchService(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<DocumentationSearchService> logger)
        {
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        public bool IsEnabled => _configuration.GetValue("Agent:Rag:Enabled", true);

        public int ChunkCount
        {
            get
            {
                EnsureIndexed();
                return _chunks.Count;
            }
        }

        public IReadOnlyList<DocumentationSearchResult> Search(string query, int? maxResults = null)
        {
            if (!IsEnabled)
                return Array.Empty<DocumentationSearchResult>();

            EnsureIndexed();
            if (_chunks.Count == 0)
                return Array.Empty<DocumentationSearchResult>();

            var terms = Tokenize(query);
            if (terms.Count == 0)
                return Array.Empty<DocumentationSearchResult>();

            var limit = maxResults ?? _configuration.GetValue("Agent:Rag:MaxChunks", 5);
            var results = new List<DocumentationSearchResult>();

            lock (_indexLock)
            {
                foreach (var chunk in _chunks)
                {
                    var score = ScoreChunk(chunk, terms);
                    if (score <= 0)
                        continue;

                    results.Add(new DocumentationSearchResult
                    {
                        SourcePath = chunk.SourcePath,
                        Heading = chunk.Heading,
                        Excerpt = TrimExcerpt(chunk.Content, 600),
                        Score = score
                    });
                }
            }

            return results
                .OrderByDescending(r => r.Score)
                .Take(Math.Clamp(limit, 1, 10))
                .ToList();
        }

        private void EnsureIndexed()
        {
            if (_indexed)
                return;

            lock (_indexLock)
            {
                if (_indexed)
                    return;

                _chunks = BuildIndex();
                _indexed = true;
                _logger.LogInformation("Documentation RAG index built with {ChunkCount} chunks.", _chunks.Count);
            }
        }

        private List<DocumentationChunk> BuildIndex()
        {
            var chunkSize = _configuration.GetValue("Agent:Rag:ChunkSizeChars", 1800);
            var overlap = _configuration.GetValue("Agent:Rag:ChunkOverlapChars", 150);
            var repoRelative = _configuration["Agent:Rag:RepositoryRootRelative"] ?? "../..";
            var repoRoot = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, repoRelative));

            var includePaths = _configuration.GetSection("Agent:Rag:IncludePaths").Get<string[]>() ??
                new[]
                {
                    "README.md",
                    "Documents/001_Overview.md",
                    "Documents/006_APIDocumentation.md",
                    "Documents/008_DeploymentGuide.md",
                    "Documents/009_ImplementationGuide.md",
                    "Documents/010_UserManual.md",
                    "Frontend/iot-monitoring-frontend/README.md"
                };

            var chunks = new List<DocumentationChunk>();
            foreach (var relativePath in includePaths)
            {
                var fullPath = Path.GetFullPath(Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
                if (!File.Exists(fullPath))
                {
                    _logger.LogDebug("RAG source not found: {Path}", fullPath);
                    continue;
                }

                try
                {
                    var text = File.ReadAllText(fullPath);
                    chunks.AddRange(ChunkMarkdown(relativePath.Replace('\\', '/'), text, chunkSize, overlap));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to index documentation file {Path}", fullPath);
                }
            }

            return chunks;
        }

        private static IEnumerable<DocumentationChunk> ChunkMarkdown(
            string sourcePath,
            string markdown,
            int chunkSize,
            int overlap)
        {
            var sections = SplitByHeadings(markdown);
            foreach (var (heading, body) in sections)
            {
                if (string.IsNullOrWhiteSpace(body))
                    continue;

                if (body.Length <= chunkSize)
                {
                    yield return new DocumentationChunk
                    {
                        SourcePath = sourcePath,
                        Heading = heading,
                        Content = body.Trim()
                    };
                    continue;
                }

                for (var start = 0; start < body.Length; start += Math.Max(1, chunkSize - overlap))
                {
                    var length = Math.Min(chunkSize, body.Length - start);
                    var slice = body.Substring(start, length).Trim();
                    if (slice.Length == 0)
                        break;

                    yield return new DocumentationChunk
                    {
                        SourcePath = sourcePath,
                        Heading = heading,
                        Content = slice
                    };

                    if (start + length >= body.Length)
                        break;
                }
            }
        }

        private static List<(string Heading, string Body)> SplitByHeadings(string markdown)
        {
            var lines = markdown.Split('\n');
            var sections = new List<(string Heading, string Body)>();
            var currentHeading = "Introduction";
            var currentBody = new List<string>();

            void Flush()
            {
                sections.Add((currentHeading, string.Join('\n', currentBody)));
                currentBody.Clear();
            }

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');
                if (line.StartsWith('#'))
                {
                    if (currentBody.Count > 0)
                        Flush();

                    currentHeading = line.TrimStart('#').Trim();
                    if (string.IsNullOrWhiteSpace(currentHeading))
                        currentHeading = "Section";
                    continue;
                }

                currentBody.Add(line);
            }

            if (currentBody.Count > 0)
                Flush();

            return sections;
        }

        private static double ScoreChunk(DocumentationChunk chunk, IReadOnlyList<string> terms)
        {
            var content = chunk.Content.ToLowerInvariant();
            var heading = chunk.Heading.ToLowerInvariant();
            var source = chunk.SourcePath.ToLowerInvariant();
            double score = 0;

            foreach (var term in terms)
            {
                if (heading.Contains(term, StringComparison.Ordinal))
                    score += 4;
                if (source.Contains(term, StringComparison.Ordinal))
                    score += 2;

                var index = 0;
                while ((index = content.IndexOf(term, index, StringComparison.Ordinal)) >= 0)
                {
                    score += 1;
                    index += term.Length;
                }
            }

            return score;
        }

        private static List<string> Tokenize(string query)
        {
            return query
                .ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\r', '\n', ',', '.', '?', '!', ';', ':', '-', '_', '/', '\\' },
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => t.Length > 1 && !StopWords.Contains(t))
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static string TrimExcerpt(string content, int maxLength)
        {
            var normalized = content.Replace('\r', ' ').Replace('\n', ' ').Trim();
            while (normalized.Contains("  ", StringComparison.Ordinal))
                normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);

            return normalized.Length <= maxLength
                ? normalized
                : normalized[..maxLength].TrimEnd() + "...";
        }
    }
}
