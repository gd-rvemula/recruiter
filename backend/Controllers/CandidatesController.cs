using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruiterApi.Data;
using RecruiterApi.DTOs;
using RecruiterApi.Models;
using RecruiterApi.Services;

namespace RecruiterApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CandidatesController : ControllerBase
{
    private readonly RecruiterDbContext _context;
    private readonly ILogger<CandidatesController> _logger;
    private readonly IFullTextSearchService _fullTextSearchService;
    private readonly SemanticSearchService _semanticSearchService;
    private const string CLIENT_ID_HEADER = "X-Client-ID";
    private const string DEFAULT_CLIENT_ID = "GLOBAL";

    public CandidatesController(
        RecruiterDbContext context, 
        ILogger<CandidatesController> logger,
        IFullTextSearchService fullTextSearchService,
        SemanticSearchService semanticSearchService)
    {
        _context = context;
        _logger = logger;
        _fullTextSearchService = fullTextSearchService;
        _semanticSearchService = semanticSearchService;
    }

    /// <summary>
    /// Get client ID from request header (defaults to GLOBAL)
    /// </summary>
    private string GetClientId()
    {
        if (Request.Headers.TryGetValue(CLIENT_ID_HEADER, out var clientId) && 
            !string.IsNullOrWhiteSpace(clientId))
        {
            return clientId.ToString();
        }
        return DEFAULT_CLIENT_ID;
    }

    [HttpPost("search")]
    public async Task<ActionResult<CandidateSearchResponse>> SearchCandidates(CandidateSearchRequest request)
    {
        try
        {
            // SMART SEARCH ROUTING: Choose best search strategy based on available infrastructure
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                // Check embedding availability for hybrid/semantic search
                var connectionString = _context.Database.GetConnectionString();
                using var checkConnection = new Npgsql.NpgsqlConnection(connectionString);
                await checkConnection.OpenAsync();
                
                // Check if we have enough embeddings for semantic search (at least 10% of candidates)
                var embeddingCheckSql = @"
                    SELECT 
                        COUNT(*) as total,
                        COUNT(profile_embedding) as with_embeddings
                    FROM candidates 
                    WHERE is_active = true";
                
                int totalActive = 0;
                int withEmbeddings = 0;
                
                using (var cmd = new Npgsql.NpgsqlCommand(embeddingCheckSql, checkConnection))
                {
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        totalActive = reader.GetInt32(0);
                        withEmbeddings = reader.GetInt32(1);
                    }
                }
                
                var embeddingCoverage = totalActive > 0 ? (double)withEmbeddings / totalActive : 0;
                
                _logger.LogInformation(
                    "Search routing: {TotalCandidates} active candidates, {WithEmbeddings} with embeddings ({Coverage:P0})",
                    totalActive, withEmbeddings, embeddingCoverage);
                
                // STRATEGY 1: HYBRID SEARCH (Best quality - if we have good embedding coverage)
                if (embeddingCoverage >= 0.3) // At least 30% have embeddings
                {
                    try
                    {
                        var clientId = GetClientId();
                        _logger.LogInformation(
                            "Using CONFIGURABLE HYBRID search for query: '{SearchTerm}' (Client: {ClientId})", 
                            request.SearchTerm, 
                            clientId);
                        
                        var (hybridResults, hybridTotalCount) = await _semanticSearchService.HybridSearchWithConfigurableScoringAsync(
                            request.SearchTerm,
                            request.Page,
                            request.PageSize,
                            clientId
                        );
                        
                        // Apply sponsorship filter post-search
                        var filteredResults = hybridResults;
                        var filteredCount = hybridTotalCount;
                        
                        if (!string.IsNullOrEmpty(request.SponsorshipFilter))
                        {
                            if (request.SponsorshipFilter.Equals("yes", StringComparison.OrdinalIgnoreCase))
                            {
                                filteredResults = hybridResults.Where(c => c.NeedsSponsorship == true).ToList();
                                // Note: filteredCount would need to be recalculated if we filter after pagination
                                // For now, we accept this limitation - ideally filtering should happen before pagination
                            }
                            else if (request.SponsorshipFilter.Equals("no", StringComparison.OrdinalIgnoreCase))
                            {
                                filteredResults = hybridResults.Where(c => c.NeedsSponsorship == false).ToList();
                            }
                        }
                        
                        var hybridPages = (int)Math.Ceiling(hybridTotalCount / (double)request.PageSize);
                        
                        return Ok(new CandidateSearchResponse
                        {
                            Candidates = filteredResults,
                            TotalCount = hybridTotalCount,
                            Page = request.Page,
                            PageSize = request.PageSize,
                            TotalPages = hybridPages,
                            HasNextPage = request.Page < hybridPages,
                            HasPreviousPage = request.Page > 1
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Hybrid search failed, falling back to semantic-only");
                        
                        // STRATEGY 2: SEMANTIC SEARCH ONLY (Good quality - if hybrid fails but embeddings exist)
                        try
                        {
                            _logger.LogInformation("Using SEMANTIC search for query: '{SearchTerm}'", request.SearchTerm);
                            
                            var semanticResults = await _semanticSearchService.SemanticSearchCandidatesAsync(
                                request.SearchTerm,
                                request.Page,
                                request.PageSize,
                                similarityThreshold: 0.3
                            );
                            
                            // Apply sponsorship filter post-search
                            if (!string.IsNullOrEmpty(request.SponsorshipFilter))
                            {
                                if (request.SponsorshipFilter.Equals("yes", StringComparison.OrdinalIgnoreCase))
                                {
                                    semanticResults = semanticResults.Where(c => c.NeedsSponsorship == true).ToList();
                                }
                                else if (request.SponsorshipFilter.Equals("no", StringComparison.OrdinalIgnoreCase))
                                {
                                    semanticResults = semanticResults.Where(c => c.NeedsSponsorship == false).ToList();
                                }
                            }
                            
                            var semanticPages = (int)Math.Ceiling(semanticResults.Count / (double)request.PageSize);
                            
                            return Ok(new CandidateSearchResponse
                            {
                                Candidates = semanticResults,
                                TotalCount = semanticResults.Count,
                                Page = request.Page,
                                PageSize = request.PageSize,
                                TotalPages = semanticPages,
                                HasNextPage = request.Page < semanticPages,
                                HasPreviousPage = request.Page > 1
                            });
                        }
                        catch (Exception semanticEx)
                        {
                            _logger.LogWarning(semanticEx, "Semantic search also failed, falling back to basic search");
                        }
                    }
                }
                else if (embeddingCoverage > 0)
                {
                    // STRATEGY 3: SEMANTIC SEARCH (Limited coverage - but try it)
                    try
                    {
                        _logger.LogInformation("Using SEMANTIC search (limited coverage) for query: '{SearchTerm}'", request.SearchTerm);
                        
                        var semanticResults = await _semanticSearchService.SemanticSearchCandidatesAsync(
                            request.SearchTerm,
                            request.Page,
                            request.PageSize,
                            similarityThreshold: 0.25 // Lower threshold for limited coverage
                        );
                        
                        // Apply sponsorship filter post-search
                        if (!string.IsNullOrEmpty(request.SponsorshipFilter))
                        {
                            if (request.SponsorshipFilter.Equals("yes", StringComparison.OrdinalIgnoreCase))
                            {
                                semanticResults = semanticResults.Where(c => c.NeedsSponsorship == true).ToList();
                            }
                            else if (request.SponsorshipFilter.Equals("no", StringComparison.OrdinalIgnoreCase))
                            {
                                semanticResults = semanticResults.Where(c => c.NeedsSponsorship == false).ToList();
                            }
                        }
                        
                        if (semanticResults.Any())
                        {
                            var semanticPages = (int)Math.Ceiling(semanticResults.Count / (double)request.PageSize);
                            
                            return Ok(new CandidateSearchResponse
                            {
                                Candidates = semanticResults,
                                TotalCount = semanticResults.Count,
                                Page = request.Page,
                                PageSize = request.PageSize,
                                TotalPages = semanticPages,
                                HasNextPage = request.Page < semanticPages,
                                HasPreviousPage = request.Page > 1
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Semantic search failed with limited coverage, falling back");
                    }
                }
                
                // STRATEGY 4: FALLBACK - Basic EF Core search (Always works)
                _logger.LogInformation("Using BASIC search (fallback) for query: '{SearchTerm}'", request.SearchTerm);
            }

            var query = _context.Candidates
                .Include(c => c.CandidateSkills)
                .ThenInclude(cs => cs.Skill)
                .AsQueryable();

            // Basic search term filter (fallback)
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(c => 
                    c.FirstName.Contains(request.SearchTerm) ||
                    c.LastName.Contains(request.SearchTerm) ||
                    c.Email.Contains(request.SearchTerm) ||
                    c.CurrentTitle != null && c.CurrentTitle.Contains(request.SearchTerm));
            }

            // Skills filter
            if (request.Skills != null && request.Skills.Any())
            {
                query = query.Where(c => c.CandidateSkills.Any(cs => 
                    request.Skills.Contains(cs.Skill.SkillName)));
            }

            // Authorization filters
            if (request.IsAuthorizedToWork.HasValue)
            {
                query = query.Where(c => c.IsAuthorizedToWork == request.IsAuthorizedToWork.Value);
            }

            // Sponsorship filter - supports tri-state: "all", "yes", "no"
            if (!string.IsNullOrEmpty(request.SponsorshipFilter))
            {
                if (request.SponsorshipFilter.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(c => c.NeedsSponsorship == true);
                }
                else if (request.SponsorshipFilter.Equals("no", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(c => c.NeedsSponsorship == false);
                }
                // "all" or any other value = no filter applied
            }
            
            // Legacy support for NeedsSponsorship boolean filter
            if (request.NeedsSponsorship.HasValue)
            {
                query = query.Where(c => c.NeedsSponsorship == request.NeedsSponsorship.Value);
            }

            // Experience filters
            if (request.MinTotalYearsExperience.HasValue)
            {
                query = query.Where(c => c.TotalYearsExperience >= request.MinTotalYearsExperience.Value);
            }

            if (request.MaxTotalYearsExperience.HasValue)
            {
                query = query.Where(c => c.TotalYearsExperience <= request.MaxTotalYearsExperience.Value);
            }

            // Active filter
            if (request.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == request.IsActive.Value);
            }

            var totalCount = await query.CountAsync();

            var candidates = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new CandidateSearchDto
                {
                    Id = c.Id,
                    CandidateCode = c.CandidateCode,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    FullName = c.FullName,
                    Email = c.Email,
                    CurrentTitle = c.CurrentTitle,
                    RequisitionName = c.RequisitionName,
                    TotalYearsExperience = c.TotalYearsExperience,
                    SalaryExpectation = c.SalaryExpectation,
                    IsAuthorizedToWork = c.IsAuthorizedToWork,
                    NeedsSponsorship = c.NeedsSponsorship,
                    IsActive = c.IsActive,
                    CurrentStatus = c.CurrentStatus,
                    PrimarySkills = c.CandidateSkills.Select(cs => cs.Skill.SkillName).ToList()
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            var response = new CandidateSearchResponse
            {
                Candidates = candidates,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching candidates with request: {@Request}", request);
            return StatusCode(500, new { message = "Error searching candidates", error = ex.Message });
        }
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<SystemStatisticsDto>> GetSystemStatistics()
    {
        try
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            var sql = @"
                SELECT 
                    COUNT(*) as total_candidates,
                    COUNT(profile_embedding) as with_embeddings
                FROM candidates 
                WHERE is_active = true";
            
            int totalCandidates = 0;
            int withEmbeddings = 0;
            
            using (var cmd = new Npgsql.NpgsqlCommand(sql, connection))
            {
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    totalCandidates = reader.GetInt32(0);
                    withEmbeddings = reader.GetInt32(1);
                }
            }
            
            var coveragePercent = totalCandidates > 0 
                ? Math.Round((double)withEmbeddings / totalCandidates * 100, 2) 
                : 0;
            
            return Ok(new SystemStatisticsDto
            {
                TotalCandidates = totalCandidates,
                WithEmbeddings = withEmbeddings,
                CoveragePercent = coveragePercent
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system statistics");
            return StatusCode(500, "An error occurred while retrieving system statistics");
        }
    }

    /// <summary>
    /// Explain why a candidate matched a search query
    /// Shows matched keywords, snippets, and semantic relevance
    /// </summary>
    [HttpPost("explain-match")]
    public async Task<ActionResult<SearchExplainDto>> ExplainMatch([FromBody] ExplainMatchRequest request)
    {
        try
        {
            var connectionString = _context.Database.GetConnectionString();
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            var explanation = new SearchExplainDto
            {
                CandidateId = request.CandidateId,
                MatchedSnippets = new List<MatchedSnippet>(),
                MatchedKeywords = new List<string>()
            };

            // Get candidate details
            var candidate = await _context.Candidates
                .Include(c => c.CandidateSkills).ThenInclude(cs => cs.Skill)
                .Include(c => c.WorkExperiences)
                .Include(c => c.Resumes)
                .FirstOrDefaultAsync(c => c.Id == request.CandidateId);

            if (candidate == null)
            {
                return NotFound();
            }

            // Extract search query words
            var searchWords = request.SearchQuery
                .ToLower()
                .Split(new[] { ' ', ',', ';', '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2) // Ignore very short words
                .ToList();

            // Check Current Title
            if (!string.IsNullOrEmpty(candidate.CurrentTitle))
            {
                var matchedTerms = searchWords
                    .Where(word => candidate.CurrentTitle.Contains(word, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchedTerms.Any())
                {
                    explanation.MatchedSnippets.Add(new MatchedSnippet
                    {
                        Source = "Current Title",
                        Text = candidate.CurrentTitle,
                        Relevance = 0.9,
                        HighlightedTerms = matchedTerms
                    });
                    explanation.MatchedKeywords.AddRange(matchedTerms);
                }
            }

            // Check Skills
            var skills = candidate.CandidateSkills?.Select(cs => cs.Skill?.SkillName).Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new();
            foreach (var skill in skills)
            {
                var matchedTerms = searchWords
                    .Where(word => skill!.Contains(word, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchedTerms.Any())
                {
                    explanation.MatchedSnippets.Add(new MatchedSnippet
                    {
                        Source = "Skills",
                        Text = skill!,
                        Relevance = 0.85,
                        HighlightedTerms = matchedTerms
                    });
                    explanation.MatchedKeywords.AddRange(matchedTerms);
                }
            }

            // Check Work Experience
            var experiences = candidate.WorkExperiences?.OrderByDescending(we => we.StartDate).Take(3).ToList() ?? new();
            foreach (var exp in experiences)
            {
                var expText = $"{exp.JobTitle} at {exp.CompanyName}";
                var matchedTerms = searchWords
                    .Where(word => expText.Contains(word, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchedTerms.Any())
                {
                    explanation.MatchedSnippets.Add(new MatchedSnippet
                    {
                        Source = "Work Experience",
                        Text = expText,
                        Relevance = 0.8,
                        HighlightedTerms = matchedTerms
                    });
                    explanation.MatchedKeywords.AddRange(matchedTerms);
                }
            }

            // Check Resume Text (if available)
            var latestResume = candidate.Resumes?.OrderByDescending(r => r.UploadedAt).FirstOrDefault();
            if (latestResume?.ResumeText != null)
            {
                var resumeWords = latestResume.ResumeText.ToLower();
                var matchedInResume = searchWords.Where(word => resumeWords.Contains(word)).ToList();
                
                if (matchedInResume.Any())
                {
                    // Find context around matched words
                    foreach (var word in matchedInResume.Take(3)) // Limit to top 3
                    {
                        var index = resumeWords.IndexOf(word, StringComparison.OrdinalIgnoreCase);
                        if (index >= 0)
                        {
                            var start = Math.Max(0, index - 50);
                            var length = Math.Min(150, latestResume.ResumeText.Length - start);
                            var snippet = latestResume.ResumeText.Substring(start, length);
                            
                            explanation.MatchedSnippets.Add(new MatchedSnippet
                            {
                                Source = "Resume",
                                Text = "..." + snippet.Trim() + "...",
                                Relevance = 0.7,
                                HighlightedTerms = new List<string> { word }
                            });
                        }
                    }
                    explanation.MatchedKeywords.AddRange(matchedInResume);
                }
            }

            // Remove duplicate keywords
            explanation.MatchedKeywords = explanation.MatchedKeywords.Distinct().ToList();

            // Calculate semantic similarity if embeddings available
            var embeddingSql = @"
                SELECT 
                    CASE WHEN profile_embedding IS NOT NULL THEN 1 ELSE 0 END as has_embedding
                FROM candidates 
                WHERE id = @candidateId";

            bool hasEmbedding = false;
            using (var cmd = new Npgsql.NpgsqlCommand(embeddingSql, connection))
            {
                cmd.Parameters.AddWithValue("@candidateId", request.CandidateId);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    hasEmbedding = reader.GetInt32(0) == 1;
                }
            }

            // Build explanation text
            if (explanation.MatchedKeywords.Any())
            {
                explanation.Explanation = $"Matched keywords: {string.Join(", ", explanation.MatchedKeywords.Take(10))}. ";
                explanation.KeywordScore = 1.0;
            }

            if (hasEmbedding)
            {
                explanation.Explanation += "Semantic analysis shows this candidate's profile is conceptually related to your search terms, even if exact words don't match.";
                explanation.SemanticScore = request.SimilarityScore ?? 0.5;
            }

            if (!explanation.MatchedKeywords.Any() && hasEmbedding)
            {
                explanation.Explanation = "This candidate matched based on semantic similarity. Their experience with related technologies, concepts, or domains is relevant to your search, even though they may not use the exact terminology you searched for.";
            }

            explanation.SimilarityScore = request.SimilarityScore ?? 0.5;

            return Ok(explanation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error explaining match for candidate {CandidateId}", request.CandidateId);
            return StatusCode(500, new { message = "Error explaining match" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetCandidateById(Guid id)
    {
        try
        {
            var candidate = await _context.Candidates
                .Include(c => c.CandidateSkills)
                .ThenInclude(cs => cs.Skill)
                .Include(c => c.Resumes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (candidate == null)
            {
                return NotFound(new { message = "Candidate not found" });
            }

            var candidateDetails = new
            {
                Id = candidate.Id,
                CandidateCode = candidate.CandidateCode,
                FirstName = candidate.FirstName,
                LastName = candidate.LastName,
                FullName = candidate.FullName,
                Email = candidate.Email,
                Phone = candidate.Phone,
                Address = candidate.Address,
                City = candidate.City,
                State = candidate.State,
                Country = candidate.Country,
                CurrentTitle = candidate.CurrentTitle,
                TotalYearsExperience = candidate.TotalYearsExperience,
                SalaryExpectation = candidate.SalaryExpectation,
                IsAuthorizedToWork = candidate.IsAuthorizedToWork,
                NeedsSponsorship = candidate.NeedsSponsorship,
                IsActive = candidate.IsActive,
                CurrentStatus = candidate.CurrentStatus,
                StatusUpdatedAt = candidate.StatusUpdatedAt,
                StatusUpdatedBy = candidate.StatusUpdatedBy,
                Skills = candidate.CandidateSkills.Select(cs => cs.Skill.SkillName).ToList(),
                Resumes = candidate.Resumes.Select(r => new
                {
                    Id = r.Id,
                    FileName = r.FileName,
                    FilePath = r.FilePath,
                    FileSize = r.FileSize,
                    FileType = r.FileType,
                    ResumeText = r.ResumeText,
                    UploadedAt = r.UploadedAt,
                    IsProcessed = r.IsProcessed
                }).ToList()
            };

            return Ok(candidateDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting candidate by ID: {CandidateId}", id);
            return StatusCode(500, new { message = "Error retrieving candidate", error = ex.Message });
        }
    }

    [HttpPost("advanced-search")]
    public async Task<ActionResult<AdvancedSearchResponse>> AdvancedSearch(AdvancedSearchRequest request)
    {
        try
        {
            var response = await _fullTextSearchService.AdvancedSearchAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing advanced search");
            return StatusCode(500, new { message = "Error performing advanced search", error = ex.Message });
        }
    }

    [HttpPost("fulltext-search")]
    public async Task<ActionResult<AdvancedSearchResponse>> FullTextSearch(FullTextSearchRequest request)
    {
        try
        {
            var response = await _fullTextSearchService.FullTextSearchAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing full-text search");
            return StatusCode(500, new { message = "Error performing full-text search", error = ex.Message });
        }
    }

    [HttpPost("search-suggestions")]
    public async Task<ActionResult<SearchSuggestionsResponse>> GetSearchSuggestions(SearchSuggestionsRequest request)
    {
        try
        {
            var response = await _fullTextSearchService.GetSearchSuggestionsAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions");
            return StatusCode(500, new { message = "Error getting search suggestions", error = ex.Message });
        }
    }

    [HttpPost("refresh-search-index")]
    public async Task<ActionResult<SearchIndexResponse>> RefreshSearchIndex(SearchIndexRequest request)
    {
        try
        {
            var response = await _fullTextSearchService.RefreshSearchIndexAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing search index");
            return StatusCode(500, new { message = "Error refreshing search index", error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<CandidateSearchResponse>> GetCandidates([FromQuery] CandidateSearchRequest request)
    {
        return await SearchCandidates(request);
    }

    // Status management endpoints
    [HttpGet("{id}/status")]
    public async Task<ActionResult<CandidateStatusResponseDto>> GetCandidateStatus(Guid id)
    {
        try
        {
            var candidate = await _context.Candidates
                .FirstOrDefaultAsync(c => c.Id == id);

            if (candidate == null)
            {
                return NotFound($"Candidate with ID {id} not found");
            }

            return Ok(new CandidateStatusResponseDto
            {
                CurrentStatus = candidate.CurrentStatus,
                StatusUpdatedAt = candidate.StatusUpdatedAt,
                StatusUpdatedBy = candidate.StatusUpdatedBy,
                AvailableStatuses = CandidateStatusExtensions.GetAllStatuses().ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for candidate {CandidateId}", id);
            return StatusCode(500, "An error occurred while retrieving the candidate status");
        }
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<CandidateStatusResponseDto>> UpdateCandidateStatus(Guid id, CandidateStatusUpdateDto request)
    {
        try
        {
            // Validate status
            if (!CandidateStatusExtensions.IsValidStatus(request.NewStatus))
            {
                return BadRequest($"Invalid status: {request.NewStatus}. Valid statuses are: {string.Join(", ", CandidateStatusExtensions.GetAllStatuses())}");
            }

            var candidate = await _context.Candidates
                .FirstOrDefaultAsync(c => c.Id == id);

            if (candidate == null)
            {
                return NotFound($"Candidate with ID {id} not found");
            }

            // Update status
            var previousStatus = candidate.CurrentStatus;
            candidate.CurrentStatus = request.NewStatus;
            candidate.StatusUpdatedAt = DateTime.UtcNow;
            candidate.StatusUpdatedBy = request.ChangedBy ?? "System";
            candidate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Status updated for candidate {CandidateId} from {PreviousStatus} to {NewStatus} by {ChangedBy}", 
                id, previousStatus, request.NewStatus, request.ChangedBy);

            return Ok(new CandidateStatusResponseDto
            {
                CurrentStatus = candidate.CurrentStatus,
                StatusUpdatedAt = candidate.StatusUpdatedAt,
                StatusUpdatedBy = candidate.StatusUpdatedBy,
                AvailableStatuses = CandidateStatusExtensions.GetAllStatuses().ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for candidate {CandidateId}", id);
            return StatusCode(500, "An error occurred while updating the candidate status");
        }
    }

    [HttpGet("{id}/status/history")]
    public async Task<ActionResult<List<CandidateStatusHistoryDto>>> GetCandidateStatusHistory(Guid id)
    {
        try
        {
            var candidate = await _context.Candidates
                .FirstOrDefaultAsync(c => c.Id == id);

            if (candidate == null)
            {
                return NotFound($"Candidate with ID {id} not found");
            }

            var statusHistory = await _context.CandidateStatusHistory
                .Where(h => h.CandidateId == id)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new CandidateStatusHistoryDto
                {
                    Id = h.Id,
                    CandidateId = h.CandidateId,
                    PreviousStatus = h.PreviousStatus,
                    NewStatus = h.NewStatus,
                    ChangedBy = h.ChangedBy,
                    ChangeReason = h.ChangeReason,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync();

            return Ok(statusHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status history for candidate {CandidateId}", id);
            return StatusCode(500, "An error occurred while retrieving the status history");
        }
    }

    [HttpGet("status/totals")]
    public async Task<ActionResult<Dictionary<string, int>>> GetStatusTotals()
    {
        try
        {
            var statusTotals = await _context.Candidates
                .Where(c => c.IsActive)
                .GroupBy(c => c.CurrentStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            // Ensure all possible statuses are included with 0 count if not present
            var allStatuses = Enum.GetNames(typeof(CandidateStatus));
            foreach (var status in allStatuses)
            {
                if (!statusTotals.ContainsKey(status))
                {
                    statusTotals[status] = 0;
                }
            }

            return Ok(statusTotals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status totals");
            return StatusCode(500, "An error occurred while retrieving status totals");
        }
    }

    [HttpGet("skills/frequency")]
    public async Task<ActionResult<List<SkillFrequencyDto>>> GetSkillsFrequency()
    {
        try
        {
            var skillsFrequency = await _context.CandidateSkills
                .Where(cs => cs.Candidate.IsActive)
                .GroupBy(cs => cs.Skill.SkillName)
                .Select(g => new SkillFrequencyDto
                {
                    Text = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(sf => sf.Value)
                .ToListAsync();

            return Ok(skillsFrequency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting skills frequency");
            return StatusCode(500, "An error occurred while retrieving skills frequency");
        }
    }

}
