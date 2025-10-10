namespace RecruiterApi.DTOs;

// Enhanced search request with full-text search capabilities
public class AdvancedSearchRequest
{
    // Full-text search query
    public string? SearchQuery { get; set; }
    
    // Traditional filters (existing)
    public string? SearchTerm { get; set; }
    public string? CurrentTitle { get; set; }
    public int? MinTotalYearsExperience { get; set; }
    public int? MaxTotalYearsExperience { get; set; }
    public bool? IsAuthorizedToWork { get; set; }
    public bool? NeedsSponsorship { get; set; }
    public decimal? MinSalaryExpectation { get; set; }
    public decimal? MaxSalaryExpectation { get; set; }
    public bool? IsActive { get; set; }
    
    // Enhanced search options
    public List<string> Skills { get; set; } = new();
    public List<string> Companies { get; set; } = new();
    public List<string> JobTitles { get; set; } = new();
    
    // Search type and options
    public SearchType SearchType { get; set; } = SearchType.Combined;
    public bool IncludeResumeContent { get; set; } = true;
    public bool IncludeSkills { get; set; } = true;
    public bool UseFullTextSearch { get; set; } = true;
    public bool UseFuzzyMatching { get; set; } = false;
    
    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    
    // Sorting
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc";
}

public enum SearchType
{
    BasicFilters,    // Traditional filters only
    FullTextSearch,  // Full-text search only
    Combined         // Both traditional filters and FTS
}

// Enhanced search response with ranking and highlighting
public class AdvancedSearchResponse
{
    public List<CandidateSearchResultDto> Candidates { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    
    // Search metadata
    public string? SearchQuery { get; set; }
    public SearchType SearchType { get; set; }
    public double SearchDurationMs { get; set; }
    public List<string> SearchSuggestions { get; set; } = new();
}

// Enhanced candidate result with search ranking and highlighting
public class CandidateSearchResultDto : CandidateSearchDto
{
    // Full-text search specific fields
    public float SearchRank { get; set; }
    public string? SearchHeadline { get; set; }
    public List<string> MatchedTerms { get; set; } = new();
    public string? ResumeSnippet { get; set; }
    
    // Additional computed fields
    public int ResumeMatchCount { get; set; }
    public int SkillMatchCount { get; set; }
    public List<string> HighlightedSkills { get; set; } = new();
}

// Full-text search specific request
public class FullTextSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeHeadlines { get; set; } = true;
    public bool IncludeSnippets { get; set; } = true;
    public float MinRank { get; set; } = 0.0f;
}

// Search analytics and suggestions
public class SearchSuggestionsRequest
{
    public string PartialQuery { get; set; } = string.Empty;
    public int MaxSuggestions { get; set; } = 5;
    public SearchSuggestionType Type { get; set; } = SearchSuggestionType.All;
}

public enum SearchSuggestionType
{
    Skills,
    JobTitles,
    Companies,
    All
}

public class SearchSuggestionsResponse
{
    public List<SearchSuggestion> Suggestions { get; set; } = new();
}

public class SearchSuggestion
{
    public string Text { get; set; } = string.Empty;
    public SearchSuggestionType Type { get; set; }
    public int Frequency { get; set; }
    public float Similarity { get; set; }
}

// Search index management
public class SearchIndexRequest
{
    public bool RefreshMaterializedView { get; set; } = true;
    public bool RebuildIndexes { get; set; } = false;
    public List<Guid>? CandidateIds { get; set; }
}

public class SearchIndexResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public double ProcessingTimeMs { get; set; }
    public int ProcessedCandidates { get; set; }
}