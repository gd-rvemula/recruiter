using RecruiterApi.DTOs;
using System.Text.RegularExpressions;

namespace RecruiterApi.Services.CandidateSearchStrategies;

/// <summary>
/// Implements auto-detection logic to intelligently choose between search strategies
/// Based on query patterns, length, and content analysis
/// </summary>
public class AutoDetectionStrategy : ICandidateSearchStrategy
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoDetectionStrategy> _logger;

    public string Name => "Auto Detection";
    public int Priority => 3;

    public AutoDetectionStrategy(IServiceProvider serviceProvider, ILogger<AutoDetectionStrategy> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public bool CanHandle(string searchMode)
    {
        return searchMode?.ToLowerInvariant() == "auto";
    }

    public async Task<CandidateSearchResponse> SearchAsync(CandidateSearchRequest request)
    {
        var detectedMode = DetermineSearchMode(request.SearchTerm ?? "");
        _logger.LogInformation("Auto-detection determined mode '{DetectedMode}' for query: '{SearchTerm}'", 
            detectedMode, request.SearchTerm);

        // Get the appropriate strategy based on detected mode
        var strategies = _serviceProvider.GetServices<ICandidateSearchStrategy>();
        var strategy = strategies.FirstOrDefault(s => s.CanHandle(detectedMode));

        if (strategy == null)
        {
            _logger.LogWarning("No strategy found for detected mode '{DetectedMode}', falling back to semantic", detectedMode);
            strategy = strategies.FirstOrDefault(s => s.CanHandle("semantic"));
        }

        if (strategy == null)
        {
            throw new InvalidOperationException("No fallback search strategy available");
        }

        // Create new request with detected mode
        var modifiedRequest = new CandidateSearchRequest
        {
            SearchTerm = request.SearchTerm,
            Page = request.Page,
            PageSize = request.PageSize,
            SponsorshipFilter = request.SponsorshipFilter,
            SearchMode = detectedMode
        };

        return await strategy.SearchAsync(modifiedRequest);
    }

    private string DetermineSearchMode(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return "semantic";
        }

        var term = searchTerm.Trim();

        // Check for exact name patterns
        if (IsNamePattern(term))
        {
            return "nameMatch";
        }

        // Check for skill-based or complex queries
        if (IsSkillOrComplexQuery(term))
        {
            return "semantic";
        }

        // Default to semantic for other cases
        return "semantic";
    }

    private bool IsNamePattern(string term)
    {
        // Pattern 1: "FirstName LastName" (2 words, each starting with capital)
        var namePattern = @"^[A-Z][a-z]+\s+[A-Z][a-z]+$";
        if (Regex.IsMatch(term, namePattern))
        {
            return true;
        }

        // Pattern 2: "First Middle Last" (3 words, proper case)
        var fullNamePattern = @"^[A-Z][a-z]+\s+[A-Z][a-z]+\s+[A-Z][a-z]+$";
        if (Regex.IsMatch(term, fullNamePattern))
        {
            return true;
        }

        // Pattern 3: Single name that looks like a proper name
        var singleNamePattern = @"^[A-Z][a-z]{2,}$";
        if (Regex.IsMatch(term, singleNamePattern) && term.Length >= 3)
        {
            return true;
        }

        // Pattern 4: Names with common patterns (O', Mc, De, etc.)
        var specialNamePattern = @"^[A-Z][a-z]*['']?[A-Z]?[a-z]*\s+[A-Z][a-z]*$";
        if (Regex.IsMatch(term, specialNamePattern))
        {
            return true;
        }

        return false;
    }

    private bool IsSkillOrComplexQuery(string term)
    {
        // Common technical keywords that indicate skill searches
        var skillKeywords = new[]
        {
            "developer", "engineer", "analyst", "manager", "architect", "consultant",
            "java", "python", "javascript", "react", "angular", "vue", "node",
            ".net", "c#", "sql", "database", "aws", "azure", "cloud",
            "docker", "kubernetes", "devops", "agile", "scrum",
            "frontend", "backend", "fullstack", "full-stack",
            "senior", "junior", "lead", "principal", "staff"
        };

        var lowerTerm = term.ToLowerInvariant();
        
        // Check if query contains skill keywords
        if (skillKeywords.Any(keyword => lowerTerm.Contains(keyword)))
        {
            return true;
        }

        // Check for multiple words (likely descriptive search)
        var words = term.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 3)
        {
            return true;
        }

        // Check for phrases with "and", "or", "with" (complex queries)
        if (lowerTerm.Contains(" and ") || lowerTerm.Contains(" or ") || lowerTerm.Contains(" with "))
        {
            return true;
        }

        return false;
    }
}