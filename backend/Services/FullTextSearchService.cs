using Microsoft.EntityFrameworkCore;
using RecruiterApi.Data;
using RecruiterApi.DTOs;
using RecruiterApi.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RecruiterApi.Services;

public interface IFullTextSearchService
{
    Task<AdvancedSearchResponse> AdvancedSearchAsync(AdvancedSearchRequest request);
    Task<AdvancedSearchResponse> FullTextSearchAsync(FullTextSearchRequest request);
    Task<SearchSuggestionsResponse> GetSearchSuggestionsAsync(SearchSuggestionsRequest request);
    Task<SearchIndexResponse> RefreshSearchIndexAsync(SearchIndexRequest request);
    Task<SearchIndexResponse> RebuildSearchVectorsAsync(List<Guid>? candidateIds = null);
}

public class FullTextSearchService : IFullTextSearchService
{
    private readonly RecruiterDbContext _context;
    private readonly ILogger<FullTextSearchService> _logger;

    public FullTextSearchService(RecruiterDbContext context, ILogger<FullTextSearchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AdvancedSearchResponse> AdvancedSearchAsync(AdvancedSearchRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            switch (request.SearchType)
            {
                case SearchType.FullTextSearch:
                    return await PerformFullTextSearchAsync(request, stopwatch);
                
                case SearchType.BasicFilters:
                    return await PerformBasicFilterSearchAsync(request, stopwatch);
                
                case SearchType.Combined:
                default:
                    return await PerformCombinedSearchAsync(request, stopwatch);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing advanced search with request: {@Request}", request);
            throw;
        }
    }

    public async Task<AdvancedSearchResponse> FullTextSearchAsync(FullTextSearchRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var sanitizedQuery = SanitizeSearchQuery(request.Query);
            if (string.IsNullOrWhiteSpace(sanitizedQuery))
            {
                return new AdvancedSearchResponse
                {
                    SearchDurationMs = stopwatch.Elapsed.TotalMilliseconds,
                    SearchQuery = request.Query
                };
            }

            // Use the database function for FTS
            var results = await _context.Database
                .SqlQueryRaw<CandidateSearchFtsResult>(@"
                    SELECT * FROM search_candidates_fts({0}, {1}, {2})",
                    sanitizedQuery, request.PageSize, (request.Page - 1) * request.PageSize)
                .ToListAsync();

            // Get total count for pagination
            var totalCountResult = await _context.Database
                .SqlQueryRaw<int>(@"
                    SELECT COUNT(*) 
                    FROM candidate_search_view 
                    WHERE combined_search_vector @@ plainto_tsquery('english', {0})",
                    sanitizedQuery)
                .FirstOrDefaultAsync();

            var totalCount = totalCountResult;
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            // Convert to DTOs
            var candidateDtos = results.Select(r => new CandidateSearchResultDto
            {
                Id = r.CandidateId,
                CandidateCode = r.CandidateCode,
                FirstName = r.FirstName,
                LastName = r.LastName,  
                FullName = r.FullName,
                Email = r.Email,
                CurrentTitle = r.CurrentTitle,
                TotalYearsExperience = r.YearsOfExperience,
                SalaryExpectation = r.SalaryExpectation,
                IsAuthorizedToWork = r.IsAuthorizedToWork,
                NeedsSponsorship = r.NeedsSponsorship,
                IsActive = r.IsActive,
                SearchRank = r.SearchRank,
                SearchHeadline = request.IncludeHeadlines ? r.HighlightSnippet : null,
                PrimarySkills = !string.IsNullOrEmpty(r.SkillsText) ? 
                    r.SkillsText.Split(',').Select(s => s.Trim()).ToList() : 
                    new List<string>()
            }).ToList();

            stopwatch.Stop();

            return new AdvancedSearchResponse
            {
                Candidates = candidateDtos,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1,
                SearchQuery = request.Query,
                SearchType = SearchType.FullTextSearch,
                SearchDurationMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing full-text search with query: {Query}", request.Query);
            throw;
        }
    }

    public async Task<SearchSuggestionsResponse> GetSearchSuggestionsAsync(SearchSuggestionsRequest request)
    {
        try
        {
            var suggestions = new List<SearchSuggestion>();
            var sanitizedQuery = SanitizeSearchQuery(request.PartialQuery);

            if (string.IsNullOrWhiteSpace(sanitizedQuery))
                return new SearchSuggestionsResponse { Suggestions = suggestions };

            // Get skill suggestions using trigram similarity
            if (request.Type == SearchSuggestionType.Skills || request.Type == SearchSuggestionType.All)
            {
                var skillSuggestions = await _context.Skills
                    .Where(s => EF.Functions.TrigramsAreSimilar(s.SkillName, sanitizedQuery))
                    .Select(s => new SearchSuggestion
                    {
                        Text = s.SkillName,
                        Type = SearchSuggestionType.Skills,
                        Similarity = (float)EF.Functions.TrigramsSimilarity(s.SkillName, sanitizedQuery)
                    })
                    .OrderByDescending(s => s.Similarity)
                    .Take(request.MaxSuggestions)
                    .ToListAsync();

                suggestions.AddRange(skillSuggestions);
            }

            // Get job title suggestions
            if (request.Type == SearchSuggestionType.JobTitles || request.Type == SearchSuggestionType.All)
            {
                var jobTitleSuggestions = await _context.Candidates
                    .Where(c => c.CurrentTitle != null && 
                               EF.Functions.TrigramsAreSimilar(c.CurrentTitle, sanitizedQuery))
                    .GroupBy(c => c.CurrentTitle)
                    .Select(g => new SearchSuggestion
                    {
                        Text = g.Key!,
                        Type = SearchSuggestionType.JobTitles,
                        Frequency = g.Count(),
                        Similarity = (float)EF.Functions.TrigramsSimilarity(g.Key!, sanitizedQuery)
                    })
                    .OrderByDescending(s => s.Similarity)
                    .Take(request.MaxSuggestions)
                    .ToListAsync();

                suggestions.AddRange(jobTitleSuggestions);
            }

            return new SearchSuggestionsResponse
            {
                Suggestions = suggestions
                    .OrderByDescending(s => s.Similarity)
                    .Take(request.MaxSuggestions)
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for query: {Query}", request.PartialQuery);
            return new SearchSuggestionsResponse();
        }
    }

    public async Task<SearchIndexResponse> RefreshSearchIndexAsync(SearchIndexRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (request.RefreshMaterializedView)
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT refresh_candidate_search_view()");
            }

            if (request.RebuildIndexes)
            {
                await _context.Database.ExecuteSqlRawAsync("REINDEX INDEX CONCURRENTLY idx_candidate_search_view_combined");
            }

            stopwatch.Stop();

            return new SearchIndexResponse
            {
                Success = true,
                Message = "Search index refreshed successfully",
                ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing search index");
            return new SearchIndexResponse
            {
                Success = false,
                Message = $"Error refreshing search index: {ex.Message}",
                ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
    }

    public async Task<SearchIndexResponse> RebuildSearchVectorsAsync(List<Guid>? candidateIds = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var processedCount = 0;

        try
        {
            // Update search vectors for candidates
            if (candidateIds == null || candidateIds.Count == 0)
            {
                await _context.Database.ExecuteSqlRawAsync(@"
                    UPDATE candidates 
                    SET search_vector = to_tsvector('english', 
                        COALESCE(first_name, '') || ' ' ||
                        COALESCE(last_name, '') || ' ' ||
                        COALESCE(email, '') || ' ' ||
                        COALESCE(current_title, '') || ' ' ||
                        COALESCE(phone, '') || ' ' ||
                        COALESCE(city, '') || ' ' ||
                        COALESCE(state, '') || ' ' ||
                        COALESCE(country, '')
                    )");
                
                var totalCandidates = await _context.Candidates.CountAsync();
                processedCount = totalCandidates;
            }
            else
            {
                // Update specific candidates
                foreach (var candidateId in candidateIds)
                {
                    await _context.Database.ExecuteSqlRawAsync(@"
                        UPDATE candidates 
                        SET search_vector = to_tsvector('english', 
                            COALESCE(first_name, '') || ' ' ||
                            COALESCE(last_name, '') || ' ' ||
                            COALESCE(email, '') || ' ' ||
                            COALESCE(current_title, '') || ' ' ||
                            COALESCE(phone, '') || ' ' ||
                            COALESCE(city, '') || ' ' ||
                            COALESCE(state, '') || ' ' ||
                            COALESCE(country, '')
                        )
                        WHERE id = {0}", candidateId);
                }
                processedCount = candidateIds.Count;
            }

            // Update resume search vectors
            await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE resumes 
                SET resume_search_vector = to_tsvector('english', 
                    COALESCE(resume_text, '') || ' ' ||
                    COALESCE(file_name, '')
                )");

            // Refresh materialized view
            await RefreshSearchIndexAsync(new SearchIndexRequest { RefreshMaterializedView = true });

            stopwatch.Stop();

            return new SearchIndexResponse
            {
                Success = true,
                Message = "Search vectors rebuilt successfully",
                ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                ProcessedCandidates = processedCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding search vectors");
            return new SearchIndexResponse
            {
                Success = false,
                Message = $"Error rebuilding search vectors: {ex.Message}",
                ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                ProcessedCandidates = processedCount
            };
        }
    }

    private async Task<AdvancedSearchResponse> PerformFullTextSearchAsync(AdvancedSearchRequest request, Stopwatch stopwatch)
    {
        var ftsRequest = new FullTextSearchRequest
        {
            Query = request.SearchQuery ?? string.Empty,
            Page = request.Page,
            PageSize = request.PageSize,
            IncludeHeadlines = true,
            IncludeSnippets = true
        };

        var response = await FullTextSearchAsync(ftsRequest);
        response.SearchType = SearchType.FullTextSearch;
        return response;
    }

    private async Task<AdvancedSearchResponse> PerformBasicFilterSearchAsync(AdvancedSearchRequest request, Stopwatch stopwatch)
    {
        // Use existing search logic for basic filters
        var query = _context.Candidates
            .Include(c => c.CandidateSkills)
            .ThenInclude(cs => cs.Skill)
            .AsQueryable();

        // Apply traditional filters (existing logic from CandidatesController)
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(c => 
                c.FirstName.Contains(request.SearchTerm) ||
                c.LastName.Contains(request.SearchTerm) ||
                c.Email.Contains(request.SearchTerm) ||
                c.CurrentTitle != null && c.CurrentTitle.Contains(request.SearchTerm));
        }

        // Add other filters...
        // (Implementation similar to existing CandidatesController logic)

        var totalCount = await query.CountAsync();
        var candidates = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CandidateSearchResultDto
            {
                Id = c.Id,
                CandidateCode = c.CandidateCode,
                FirstName = c.FirstName,
                LastName = c.LastName,
                FullName = c.FullName,
                Email = c.Email,
                CurrentTitle = c.CurrentTitle,
                TotalYearsExperience = c.TotalYearsExperience,
                SalaryExpectation = c.SalaryExpectation,
                IsAuthorizedToWork = c.IsAuthorizedToWork,
                NeedsSponsorship = c.NeedsSponsorship,
                IsActive = c.IsActive,
                PrimarySkills = c.CandidateSkills.Select(cs => cs.Skill.SkillName).ToList()
            })
            .ToListAsync();

        stopwatch.Stop();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new AdvancedSearchResponse
        {
            Candidates = candidates,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasNextPage = request.Page < totalPages,
            HasPreviousPage = request.Page > 1,
            SearchType = SearchType.BasicFilters,
            SearchDurationMs = stopwatch.Elapsed.TotalMilliseconds
        };
    }

    private async Task<AdvancedSearchResponse> PerformCombinedSearchAsync(AdvancedSearchRequest request, Stopwatch stopwatch)
    {
        // Combine full-text search with traditional filters
        // This is a sophisticated implementation that merges results from both approaches
        
        // For now, if we have a search query, use FTS, otherwise use basic filters
        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            return await PerformFullTextSearchAsync(request, stopwatch);
        }
        else
        {
            return await PerformBasicFilterSearchAsync(request, stopwatch);
        }
    }

    private static string SanitizeSearchQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        // Remove potentially dangerous characters for PostgreSQL FTS
        var sanitized = Regex.Replace(query, @"[^\w\s-]", " ");
        
        // Normalize whitespace
        sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();
        
        return sanitized;
    }
}

// Helper class for database function results
public class CandidateSearchFtsResult
{
    public Guid CandidateId { get; set; }
    public string CandidateCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? CurrentTitle { get; set; }
    public int YearsOfExperience { get; set; }
    public decimal? SalaryExpectation { get; set; }
    public bool IsAuthorizedToWork { get; set; }
    public bool NeedsSponsorship { get; set; }
    public bool IsActive { get; set; }
    public string? SkillsText { get; set; }
    public float SearchRank { get; set; }
    public string? HighlightSnippet { get; set; }
}