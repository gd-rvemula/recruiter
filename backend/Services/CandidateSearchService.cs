using RecruiterApi.DTOs;
using RecruiterApi.Services.CandidateSearchStrategies;

namespace RecruiterApi.Services;

/// <summary>
/// Orchestrates candidate search operations by coordinating multiple search strategies
/// Implements strategy pattern for clean separation of search concerns
/// </summary>
public class CandidateSearchService
{
    private readonly IEnumerable<ICandidateSearchStrategy> _strategies;
    private readonly ILogger<CandidateSearchService> _logger;

    public CandidateSearchService(
        IEnumerable<ICandidateSearchStrategy> strategies, 
        ILogger<CandidateSearchService> logger)
    {
        _strategies = strategies;
        _logger = logger;
    }

    /// <summary>
    /// Performs candidate search using the appropriate strategy based on search mode
    /// Includes fallback logic for error handling
    /// </summary>
    public async Task<CandidateSearchResponse> SearchCandidatesAsync(CandidateSearchRequest request)
    {
        _logger.LogInformation("Starting search with mode '{SearchMode}' for term: '{SearchTerm}'", 
            request.SearchMode, request.SearchTerm);

        // Normalize search mode
        var searchMode = (request.SearchMode ?? "semantic").ToLowerInvariant();

        try
        {
            // Find primary strategy
            var strategy = FindStrategy(searchMode);
            
            if (strategy != null)
            {
                _logger.LogInformation("Using strategy: {StrategyName}", strategy.Name);
                return await strategy.SearchAsync(request);
            }

            // Fallback to semantic search if no strategy found
            _logger.LogWarning("No strategy found for mode '{SearchMode}', falling back to semantic", searchMode);
            return await FallbackToSemanticSearch(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Primary search strategy failed for mode '{SearchMode}', attempting fallback", searchMode);
            
            // Try fallback strategies
            return await HandleSearchFailure(request, searchMode, ex);
        }
    }

    /// <summary>
    /// Finds the appropriate search strategy based on search mode
    /// Strategies are prioritized by their Priority property
    /// </summary>
    private ICandidateSearchStrategy? FindStrategy(string searchMode)
    {
        return _strategies
            .Where(s => s.CanHandle(searchMode))
            .OrderBy(s => s.Priority)
            .FirstOrDefault();
    }

    /// <summary>
    /// Handles search failures by attempting fallback strategies
    /// </summary>
    private async Task<CandidateSearchResponse> HandleSearchFailure(
        CandidateSearchRequest request, 
        string originalMode, 
        Exception originalException)
    {
        // Try name match if original wasn't name match
        if (originalMode != "namematch")
        {
            try
            {
                _logger.LogInformation("Attempting name match fallback");
                var nameMatchStrategy = FindStrategy("namematch");
                if (nameMatchStrategy != null)
                {
                    var nameMatchRequest = new CandidateSearchRequest
                    {
                        SearchTerm = request.SearchTerm,
                        Page = request.Page,
                        PageSize = request.PageSize,
                        SponsorshipFilter = request.SponsorshipFilter,
                        SearchMode = "nameMatch"
                    };
                    return await nameMatchStrategy.SearchAsync(nameMatchRequest);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Name match fallback also failed");
            }
        }

        // Final fallback to semantic search
        if (originalMode != "semantic")
        {
            try
            {
                _logger.LogInformation("Attempting semantic search fallback");
                return await FallbackToSemanticSearch(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "All fallback strategies failed");
            }
        }

        // If all else fails, return empty results
        _logger.LogError("All search strategies failed, returning empty results");
        return CreateEmptyResponse(request);
    }

    /// <summary>
    /// Fallback to semantic search strategy
    /// </summary>
    private async Task<CandidateSearchResponse> FallbackToSemanticSearch(CandidateSearchRequest request)
    {
        var semanticStrategy = FindStrategy("semantic");
        if (semanticStrategy == null)
        {
            throw new InvalidOperationException("No semantic search strategy available");
        }

        var semanticRequest = new CandidateSearchRequest
        {
            SearchTerm = request.SearchTerm,
            Page = request.Page,
            PageSize = request.PageSize,
            SponsorshipFilter = request.SponsorshipFilter,
            SearchMode = "semantic"
        };
        return await semanticStrategy.SearchAsync(semanticRequest);
    }

    /// <summary>
    /// Creates an empty response when all search strategies fail
    /// </summary>
    private CandidateSearchResponse CreateEmptyResponse(CandidateSearchRequest request)
    {
        return new CandidateSearchResponse
        {
            Candidates = new List<CandidateSearchDto>(),
            TotalCount = 0,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = 0,
            HasNextPage = false,
            HasPreviousPage = false
        };
    }

    /// <summary>
    /// Gets information about all available search strategies
    /// Useful for debugging and monitoring
    /// </summary>
    public IEnumerable<object> GetAvailableStrategies()
    {
        return _strategies.Select(s => new
        {
            Name = s.Name,
            Priority = s.Priority,
            Type = s.GetType().Name
        }).OrderBy(s => s.Priority);
    }
}