using RecruiterApi.DTOs;

namespace RecruiterApi.Services.CandidateSearchStrategies;

/// <summary>
/// Implements semantic search using embeddings and vector similarity
/// Optimized for skill-based and conceptual searches
/// </summary>
public class SemanticSearchStrategy : ICandidateSearchStrategy
{
    private readonly SemanticSearchService _semanticSearchService;
    private readonly ILogger<SemanticSearchStrategy> _logger;

    public string Name => "Semantic Search";
    public int Priority => 2;

    public SemanticSearchStrategy(SemanticSearchService semanticSearchService, ILogger<SemanticSearchStrategy> logger)
    {
        _semanticSearchService = semanticSearchService;
        _logger = logger;
    }

    public bool CanHandle(string searchMode)
    {
        return searchMode?.ToLowerInvariant() == "semantic";
    }

    public async Task<CandidateSearchResponse> SearchAsync(CandidateSearchRequest request)
    {
        _logger.LogInformation("Using SEMANTIC search for query: '{SearchTerm}'", request.SearchTerm);

        try
        {
            var semanticResults = await _semanticSearchService.SemanticSearchCandidatesAsync(
                request.SearchTerm ?? "",
                request.Page,
                request.PageSize,
                similarityThreshold: 0.3
            );

            // Apply filters
            var filteredResults = ApplyFilters(semanticResults, request);

            var totalPages = (int)Math.Ceiling(filteredResults.Count / (double)request.PageSize);

            return new CandidateSearchResponse
            {
                Candidates = filteredResults,
                TotalCount = filteredResults.Count,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Semantic search failed, will be handled by fallback strategy");
            throw; // Let the orchestrator handle fallback
        }
    }

    private List<CandidateSearchDto> ApplyFilters(List<CandidateSearchDto> candidates, CandidateSearchRequest request)
    {
        var filtered = candidates.AsEnumerable();

        // Sponsorship filter
        if (!string.IsNullOrEmpty(request.SponsorshipFilter))
        {
            if (request.SponsorshipFilter.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(c => c.NeedsSponsorship == true);
            }
            else if (request.SponsorshipFilter.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(c => c.NeedsSponsorship == false);
            }
        }

        return filtered.ToList();
    }
}