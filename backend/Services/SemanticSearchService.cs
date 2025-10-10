using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RecruiterApi.DTOs;
using RecruiterApi.Services.Scoring;

namespace RecruiterApi.Services
{
    /// <summary>
    /// Service for performing semantic search using vector embeddings
    /// </summary>
    public class SemanticSearchService
    {
        private readonly ILogger<SemanticSearchService> _logger;
        private readonly IEmbeddingService _embeddingService;
        private readonly IClientConfigService _configService;
        private readonly string _connectionString;
        private readonly ScoringStrategyFactory _scoringFactory;

        public SemanticSearchService(
            ILogger<SemanticSearchService> logger,
            IEmbeddingService embeddingService,
            IConfiguration configuration,
            IClientConfigService configService)
        {
            _logger = logger;
            _embeddingService = embeddingService;
            _configService = configService;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("ConnectionString not configured");
            _scoringFactory = new ScoringStrategyFactory();
        }

        /// <summary>
        /// Search candidates using semantic similarity
        /// </summary>
        public async Task<List<CandidateSearchDto>> SemanticSearchCandidatesAsync(
            string query, 
            int page = 1, 
            int pageSize = 20,
            double similarityThreshold = 0.7)
        {
            try
            {
                // 1. Generate embedding for the search query
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

                if (queryEmbedding == null || queryEmbedding.Length == 0)
                {
                    _logger.LogWarning("Failed to generate embedding for query: {Query}", query);
                    return new List<CandidateSearchDto>();
                }

                // 2. Convert float array to pgvector format
                var vectorString = FormatVectorForPostgres(queryEmbedding);

                // 3. Perform vector similarity search
                var results = new List<CandidateSearchDto>();

                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        c.id,
                        c.candidate_code,
                        c.first_name,
                        c.last_name,
                        c.full_name,
                        c.email,
                        c.phone,
                        c.current_title,
                        c.requisition_name,
                        c.total_years_experience,
                        c.current_status,
                        c.needs_sponsorship,
                        c.is_authorized_to_work,
                        -- Calculate cosine similarity (1 - cosine distance)
                        1 - (c.profile_embedding <=> @queryVector::vector) as similarity_score
                    FROM candidates c
                    WHERE c.is_active = true
                        AND c.profile_embedding IS NOT NULL
                        -- Only return results above threshold
                        AND (1 - (c.profile_embedding <=> @queryVector::vector)) >= @threshold
                    ORDER BY c.profile_embedding <=> @queryVector::vector
                    LIMIT @limit OFFSET @offset";

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@queryVector", vectorString);
                command.Parameters.AddWithValue("@threshold", similarityThreshold);
                command.Parameters.AddWithValue("@limit", pageSize);
                command.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new CandidateSearchDto
                    {
                        Id = reader.GetGuid(0),
                        CandidateCode = reader.GetString(1),
                        FirstName = reader.GetString(2),
                        LastName = reader.GetString(3),
                        FullName = reader.GetString(4),
                        Email = reader.GetString(5),
                        Phone = reader.IsDBNull(6) ? null : reader.GetString(6),
                        CurrentTitle = reader.IsDBNull(7) ? null : reader.GetString(7),
                        RequisitionName = reader.IsDBNull(8) ? null : reader.GetString(8),
                        TotalYearsExperience = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                        CurrentStatus = reader.IsDBNull(10) ? "New" : reader.GetString(10),
                        NeedsSponsorship = reader.GetBoolean(11),
                        IsAuthorizedToWork = reader.GetBoolean(12),
                        SimilarityScore = (double)reader.GetFloat(13) // Similarity score
                    });
                }

                _logger.LogInformation(
                    "Semantic search found {Count} candidates for query: {Query}", 
                    results.Count, 
                    query
                );

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing semantic search for query: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Hybrid search: Combine semantic search with traditional full-text search
        /// </summary>
        public async Task<List<CandidateSearchDto>> HybridSearchAsync(
            string query,
            int page = 1,
            int pageSize = 20,
            double semanticWeight = 0.7,
            double keywordWeight = 0.3)
        {
            try
            {
                // 1. Generate embedding
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
                var vectorString = FormatVectorForPostgres(queryEmbedding);

                // 2. Hybrid search combining semantic and keyword matching
                var results = new List<CandidateSearchDto>();

                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    WITH semantic_scores AS (
                        SELECT 
                            c.id,
                            (1 - (c.profile_embedding <=> @queryVector::vector)) as semantic_score
                        FROM candidates c
                        WHERE c.is_active = true 
                            AND c.profile_embedding IS NOT NULL
                    ),
                    keyword_scores AS (
                        SELECT 
                            csv.id,
                            ts_rank(csv.combined_search_vector, plainto_tsquery('english', @query)) as keyword_score
                        FROM candidate_search_view csv
                        WHERE csv.combined_search_vector @@ plainto_tsquery('english', @query)
                    )
                    SELECT 
                        c.id,
                        c.candidate_code,
                        c.first_name,
                        c.last_name,
                        c.full_name,
                        c.email,
                        c.phone,
                        c.current_title,
                        c.requisition_name,
                        c.total_years_experience,
                        c.current_status,
                        c.needs_sponsorship,
                        c.is_authorized_to_work,
                        COALESCE(ss.semantic_score, 0) * @semanticWeight + 
                        COALESCE(ks.keyword_score, 0) * @keywordWeight as hybrid_score
                    FROM candidates c
                    LEFT JOIN semantic_scores ss ON c.id = ss.id
                    LEFT JOIN keyword_scores ks ON c.id = ks.id
                    WHERE c.is_active = true
                        AND (ss.semantic_score IS NOT NULL OR ks.keyword_score IS NOT NULL)
                    ORDER BY hybrid_score DESC
                    LIMIT @limit OFFSET @offset";

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@queryVector", vectorString);
                command.Parameters.AddWithValue("@query", query);
                command.Parameters.AddWithValue("@semanticWeight", semanticWeight);
                command.Parameters.AddWithValue("@keywordWeight", keywordWeight);
                command.Parameters.AddWithValue("@limit", pageSize);
                command.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new CandidateSearchDto
                    {
                        Id = reader.GetGuid(0),
                        CandidateCode = reader.GetString(1),
                        FirstName = reader.GetString(2),
                        LastName = reader.GetString(3),
                        FullName = reader.GetString(4),
                        Email = reader.GetString(5),
                        Phone = reader.IsDBNull(6) ? null : reader.GetString(6),
                        CurrentTitle = reader.IsDBNull(7) ? null : reader.GetString(7),
                        RequisitionName = reader.IsDBNull(8) ? null : reader.GetString(8),
                        TotalYearsExperience = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                        CurrentStatus = reader.IsDBNull(10) ? "New" : reader.GetString(10),
                        NeedsSponsorship = reader.GetBoolean(11),
                        IsAuthorizedToWork = reader.GetBoolean(12),
                        SimilarityScore = (double)reader.GetFloat(13)
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing hybrid search for query: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Generate and store embeddings for all candidates
        /// </summary>
        public async Task<int> GenerateAllCandidateEmbeddingsAsync()
        {
            var count = 0;

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Get candidates without embeddings
                var sql = @"
                    SELECT c.id, c.full_name, c.current_title, r.resume_text
                    FROM candidates c
                    LEFT JOIN resumes r ON c.id = r.candidate_id
                    WHERE c.is_active = true 
                        AND c.profile_embedding IS NULL
                    LIMIT 100"; // Process in batches

                var candidatesToProcess = new List<(Guid id, string profileText)>();

                await using (var command = new NpgsqlCommand(sql, connection))
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var id = reader.GetGuid(0);
                        var fullName = reader.GetString(1);
                        var title = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        var resumeText = reader.IsDBNull(3) ? "" : reader.GetString(3);

                        // Combine relevant text for embedding
                        var profileText = $"{fullName} {title} {resumeText}";
                        candidatesToProcess.Add((id, profileText));
                    }
                }

                // Generate embeddings in batch
                var texts = candidatesToProcess.Select(c => c.profileText).ToList();
                var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts);

                // Store embeddings
                for (int i = 0; i < candidatesToProcess.Count; i++)
                {
                    var (id, _) = candidatesToProcess[i];
                    var embedding = embeddings[i];
                    var vectorString = FormatVectorForPostgres(embedding);

                    var updateSql = @"
                        UPDATE candidates 
                        SET profile_embedding = @vector::vector,
                            embedding_generated_at = NOW(),
                            embedding_model = @model
                        WHERE id = @id";

                    await using var updateCommand = new NpgsqlCommand(updateSql, connection);
                    updateCommand.Parameters.AddWithValue("@vector", vectorString);
                    updateCommand.Parameters.AddWithValue("@model", _embeddingService.GetModelName());
                    updateCommand.Parameters.AddWithValue("@id", id);

                    await updateCommand.ExecuteNonQueryAsync();
                    count++;
                }

                _logger.LogInformation("Generated embeddings for {Count} candidates", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating candidate embeddings");
                throw;
            }
        }

        /// <summary>
        /// Format float array as PostgreSQL vector string
        /// </summary>
        private string FormatVectorForPostgres(float[] vector)
        {
            return "[" + string.Join(",", vector.Select(v => v.ToString("G"))) + "]";
        }

        /// <summary>
        /// Extract keywords from search query
        /// Filters out common words and short terms
        /// </summary>
        private List<string> ExtractKeywords(string query)
        {
            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "and", "or", "the", "a", "an", "in", "on", "at", "to", "for", "of", "with"
            };

            return query
                .ToLower()
                .Split(new[] { " and ", " or ", ",", " ", "\t", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(k => k.Length > 2 && !stopWords.Contains(k))
                .Select(k => k.Trim())
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Calculate per-keyword match scores for a candidate
        /// Scoring logic:
        /// - Title match: 1.0 (highest priority)
        /// - Skills match: 0.95
        /// - High frequency in resume (5+ occurrences): 0.9
        /// - Medium frequency (2-4 occurrences): 0.7
        /// - Low frequency (1 occurrence): 0.5
        /// - No match: 0.0
        /// </summary>
        private async Task<Dictionary<string, double>> CalculateKeywordScoresAsync(
            Guid candidateId, 
            List<string> keywords,
            NpgsqlConnection connection)
        {
            var scores = new Dictionary<string, double>();

            foreach (var keyword in keywords)
            {
                var sql = @"
                    SELECT 
                        CASE 
                            -- Exact match in title (highest priority)
                            WHEN c.current_title ILIKE '%' || @keyword || '%' THEN 1.0
                            
                            -- Match in skills
                            WHEN EXISTS (
                                SELECT 1 FROM candidate_skills cs
                                JOIN skills s ON cs.skill_id = s.id
                                WHERE cs.candidate_id = c.id 
                                AND s.skill_name ILIKE '%' || @keyword || '%'
                            ) THEN 0.95
                            
                            -- High frequency in resume (5+ occurrences)
                            WHEN (
                                SELECT COUNT(*) 
                                FROM regexp_matches(LOWER(COALESCE(r.resume_text, '')), LOWER(@keyword), 'g')
                            ) >= 5 THEN 0.9
                            
                            -- Medium frequency in resume (2-4 occurrences)
                            WHEN (
                                SELECT COUNT(*) 
                                FROM regexp_matches(LOWER(COALESCE(r.resume_text, '')), LOWER(@keyword), 'g')
                            ) >= 2 THEN 0.7
                            
                            -- Low frequency in resume (1 occurrence)
                            WHEN COALESCE(r.resume_text, '') ILIKE '%' || @keyword || '%' THEN 0.5
                            
                            -- No match
                            ELSE 0.0
                        END as keyword_score
                    FROM candidates c
                    LEFT JOIN resumes r ON c.id = r.candidate_id
                    WHERE c.id = @candidateId";

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@candidateId", candidateId);
                command.Parameters.AddWithValue("@keyword", keyword);

                var result = await command.ExecuteScalarAsync();
                scores[keyword] = result != null && result != DBNull.Value 
                    ? Convert.ToDouble(result) 
                    : 0.0;
            }

            return scores;
        }

        /// <summary>
        /// Hybrid search with configurable scoring strategy
        /// Uses client configuration to determine scoring algorithm
        /// </summary>
        public async Task<(List<CandidateSearchDto> candidates, int totalCount)> HybridSearchWithConfigurableScoringAsync(
            string query,
            int page = 1,
            int pageSize = 20,
            string clientId = "GLOBAL")
        {
            try
            {
                // Get scoring configuration for the client
                var config = await _configService.GetSearchScoringConfigAsync(clientId);
                var scoringStrategy = _scoringFactory.GetStrategy(config.ScoringStrategy);

                _logger.LogInformation(
                    "Starting configurable hybrid search with strategy: {Strategy} for client: {ClientId}",
                    scoringStrategy.StrategyName,
                    clientId
                );

                // Extract keywords from query
                var keywords = ExtractKeywords(query);
                
                _logger.LogInformation(
                    "Extracted {Count} keywords: {Keywords}",
                    keywords.Count,
                    string.Join(", ", keywords)
                );

                // Generate embedding for semantic search
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
                if (queryEmbedding == null || queryEmbedding.Length == 0)
                {
                    _logger.LogWarning("Failed to generate embedding for query: {Query}", query);
                    return (new List<CandidateSearchDto>(), 0);
                }

                var vectorString = FormatVectorForPostgres(queryEmbedding);

                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Step 1: Get top candidates by semantic similarity (cast a wide net)
                var sql = @"
                    SELECT 
                        c.id,
                        c.candidate_code,
                        c.first_name,
                        c.last_name,
                        c.full_name,
                        c.email,
                        c.phone,
                        c.current_title,
                        c.requisition_name,
                        c.total_years_experience,
                        c.current_status,
                        c.needs_sponsorship,
                        c.is_authorized_to_work,
                        1 - (c.profile_embedding <=> @queryVector::vector) as semantic_score
                    FROM candidates c
                    WHERE c.is_active = true
                        AND c.profile_embedding IS NOT NULL
                        AND (1 - (c.profile_embedding <=> @queryVector::vector)) >= @threshold
                    ORDER BY c.profile_embedding <=> @queryVector::vector
                    LIMIT 100"; // Get top 100 for re-scoring

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@queryVector", vectorString);
                command.Parameters.AddWithValue("@threshold", config.SimilarityThreshold);

                var candidatesWithScores = new List<(CandidateSearchDto candidate, double semanticScore)>();

                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var candidate = new CandidateSearchDto
                        {
                            Id = reader.GetGuid(0),
                            CandidateCode = reader.GetString(1),
                            FirstName = reader.GetString(2),
                            LastName = reader.GetString(3),
                            FullName = reader.GetString(4),
                            Email = reader.GetString(5),
                            Phone = reader.IsDBNull(6) ? null : reader.GetString(6),
                            CurrentTitle = reader.IsDBNull(7) ? null : reader.GetString(7),
                            RequisitionName = reader.IsDBNull(8) ? null : reader.GetString(8),
                            TotalYearsExperience = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                            CurrentStatus = reader.IsDBNull(10) ? "New" : reader.GetString(10),
                            NeedsSponsorship = reader.GetBoolean(11),
                            IsAuthorizedToWork = reader.GetBoolean(12)
                        };
                        var semanticScore = Convert.ToDouble(reader.GetValue(13));
                        candidatesWithScores.Add((candidate, semanticScore));
                    }
                }

                _logger.LogInformation(
                    "Retrieved {Count} candidates for re-scoring",
                    candidatesWithScores.Count
                );

                // Step 2: Calculate per-keyword scores and apply scoring strategy
                var results = new List<CandidateSearchDto>();

                foreach (var (candidate, semanticScore) in candidatesWithScores)
                {
                    // Calculate keyword scores for this candidate
                    var keywordScores = await CalculateKeywordScoresAsync(
                        candidate.Id, 
                        keywords, 
                        connection
                    );

                    // Apply the configured scoring strategy
                    var finalScore = scoringStrategy.CalculateScore(
                        keywordScores,
                        semanticScore,
                        keywords.Count
                    );

                    candidate.SimilarityScore = finalScore;
                    results.Add(candidate);
                }

                // Step 3: Sort by final score and get total count BEFORE pagination
                var totalCount = results.Count;
                var paginatedResults = results
                    .OrderByDescending(c => c.SimilarityScore)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation(
                    "Configurable hybrid search ({Strategy}) returned {PageCount}/{TotalCount} candidates for query: {Query} (page {Page})",
                    scoringStrategy.StrategyName,
                    paginatedResults.Count,
                    totalCount,
                    query,
                    page
                );

                return (paginatedResults, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing configurable hybrid search for query: {Query}", query);
                throw;
            }
        }
    }
}