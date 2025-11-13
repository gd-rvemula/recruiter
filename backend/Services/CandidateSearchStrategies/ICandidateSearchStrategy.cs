using RecruiterApi.DTOs;

namespace RecruiterApi.Services.CandidateSearchStrategies;

/// <summary>
/// Defines the contract for candidate search strategies
/// </summary>
public interface ICandidateSearchStrategy
{
    /// <summary>
    /// Executes the search using this strategy
    /// </summary>
    Task<CandidateSearchResponse> SearchAsync(CandidateSearchRequest request);
    
    /// <summary>
    /// Determines if this strategy can handle the given search mode
    /// </summary>
    bool CanHandle(string searchMode);
    
    /// <summary>
    /// Gets the priority order for strategy selection (lower = higher priority)
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Gets a human-readable name for this strategy
    /// </summary>
    string Name { get; }
}