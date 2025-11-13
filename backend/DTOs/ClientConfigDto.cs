namespace RecruiterApi.DTOs
{
    /// <summary>
    /// Client configuration data transfer object
    /// </summary>
    public class ClientConfigDto
    {
        public Guid Id { get; set; }
        public string ClientId { get; set; } = "GLOBAL";
        public string ConfigKey { get; set; } = string.Empty;
        public string ConfigValue { get; set; } = string.Empty;
        public string ConfigType { get; set; } = "string";
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Request to update or create a configuration
    /// </summary>
    public class UpdateConfigRequest
    {
        public string ConfigKey { get; set; } = string.Empty;
        public string ConfigValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// Search scoring configuration settings
    /// </summary>
    public class SearchScoringConfigDto
    {
        public string ClientId { get; set; } = "GLOBAL";
        public string ScoringStrategy { get; set; } = "option1"; // option1 or option4
        public double SemanticWeight { get; set; } = 0.6;
        public double KeywordWeight { get; set; } = 0.4;
        public double SimilarityThreshold { get; set; } = 0.3;
    }

    /// <summary>
    /// Privacy and PII protection configuration settings
    /// </summary>
    public class PrivacyConfigDto
    {
        public string ClientId { get; set; } = "GLOBAL";
        public bool PiiSanitizationEnabled { get; set; } = true;
        public string PiiSanitizationLevel { get; set; } = "full"; // minimal, standard, full
        public bool LogPiiRemovals { get; set; } = true;
    }

    /// <summary>
    /// Full-Text Search rebuild operation result
    /// </summary>
    public class FtsRebuildResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcessedItems { get; set; }
        public double DurationMs { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public List<string> Errors { get; set; } = new List<string>();
    }
}
