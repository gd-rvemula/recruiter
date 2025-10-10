namespace RecruiterApi.DTOs;

/// <summary>
/// Explains why a candidate matched a search query
/// </summary>
public class SearchExplainDto
{
    /// <summary>
    /// Candidate ID
    /// </summary>
    public Guid CandidateId { get; set; }

    /// <summary>
    /// Overall similarity score (0-1)
    /// </summary>
    public double SimilarityScore { get; set; }

    /// <summary>
    /// Semantic similarity score (0-1)
    /// </summary>
    public double? SemanticScore { get; set; }

    /// <summary>
    /// Keyword matching score
    /// </summary>
    public double? KeywordScore { get; set; }

    /// <summary>
    /// Text snippets from the candidate profile that matched
    /// Includes context around matches
    /// </summary>
    public List<MatchedSnippet> MatchedSnippets { get; set; } = new();

    /// <summary>
    /// Keywords from the query that were found in the profile
    /// </summary>
    public List<string> MatchedKeywords { get; set; } = new();

    /// <summary>
    /// Explanation of why this candidate matched
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
}

/// <summary>
/// A snippet of text that matched the search
/// </summary>
public class MatchedSnippet
{
    /// <summary>
    /// Source of the snippet (e.g., "Current Title", "Skills", "Work Experience", "Resume")
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The matched text with surrounding context
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Relevance score for this snippet (0-1)
    /// </summary>
    public double Relevance { get; set; }

    /// <summary>
    /// Specific words/phrases that matched (for highlighting)
    /// </summary>
    public List<string> HighlightedTerms { get; set; } = new();
}

/// <summary>
/// Request to explain why a candidate matched a search
/// </summary>
public class ExplainMatchRequest
{
    /// <summary>
    /// Candidate ID to explain
    /// </summary>
    public Guid CandidateId { get; set; }

    /// <summary>
    /// The search query that was used
    /// </summary>
    public string SearchQuery { get; set; } = string.Empty;

    /// <summary>
    /// The similarity score for this candidate (optional)
    /// </summary>
    public double? SimilarityScore { get; set; }
}
