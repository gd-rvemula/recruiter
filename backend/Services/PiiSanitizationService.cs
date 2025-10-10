using System.Text.RegularExpressions;

namespace RecruiterApi.Services;

/// <summary>
/// Service for removing Personally Identifiable Information (PII) from candidate data
/// This ensures we don't store sensitive personal information in the database
/// </summary>
public class PiiSanitizationService : IPiiSanitizationService
{
    private readonly ILogger<PiiSanitizationService> _logger;

    // Regex patterns for PII detection
    private static readonly Regex EmailRegex = new Regex(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PhoneRegex = new Regex(
        @"(?:\+?1[-.\s]?)?\(?([0-9]{3})\)?[-.\s]?([0-9]{3})[-.\s]?([0-9]{4})\b|" +
        @"\b\d{3}[-.\s]?\d{3}[-.\s]?\d{4}\b|" +
        @"\b\(\d{3}\)\s*\d{3}[-.\s]?\d{4}\b",
        RegexOptions.Compiled);

    private static readonly Regex ZipCodeRegex = new Regex(
        @"\b\d{5}(?:-\d{4})?\b",
        RegexOptions.Compiled);

    private static readonly Regex CandidateCodeRegex = new Regex(
        @"\(C[0-9a-fA-F]{6,14}\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public PiiSanitizationService(ILogger<PiiSanitizationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Sanitize resume text by removing all PII
    /// </summary>
    public string SanitizeResumeText(string resumeText, string? candidateName = null, string? email = null, string? address = null)
    {
        if (string.IsNullOrWhiteSpace(resumeText))
        {
            return resumeText;
        }

        var sanitized = resumeText;
        int removedCount = 0;

        // 1. Remove emails
        var emailMatches = EmailRegex.Matches(sanitized);
        sanitized = RemoveEmailAddresses(sanitized);
        removedCount += emailMatches.Count;

        // 2. Remove phone numbers
        var phoneMatches = PhoneRegex.Matches(sanitized);
        sanitized = RemovePhoneNumbers(sanitized);
        removedCount += phoneMatches.Count;

        // 3. Remove zip codes
        var zipMatches = ZipCodeRegex.Matches(sanitized);
        sanitized = RemoveZipCodes(sanitized);
        removedCount += zipMatches.Count;

        // 4. Remove candidate name if provided
        if (!string.IsNullOrWhiteSpace(candidateName))
        {
            sanitized = RemoveNameOccurrences(sanitized, candidateName);
        }

        // 5. Remove email address (local part) if provided
        if (!string.IsNullOrWhiteSpace(email) && email.Contains("@"))
        {
            var emailLocalPart = email.Split('@')[0];
            // Remove variations of the email local part (with dots, underscores, etc.)
            sanitized = RemoveTextVariations(sanitized, emailLocalPart);
        }

        // 6. Remove address if provided
        if (!string.IsNullOrWhiteSpace(address))
        {
            sanitized = RemoveAddressOccurrences(sanitized, address);
        }

        // 7. Remove common address patterns (street numbers, apt numbers, etc.)
        sanitized = RemoveStreetAddresses(sanitized);

        _logger.LogInformation("Sanitized resume text: Removed {Count} PII occurrences", removedCount);

        return sanitized;
    }

    /// <summary>
    /// Extract candidate code from job application text
    /// Example: "Dwight Shrute (C20250928edb5a8)" -> "C20250928edb5a8"
    /// </summary>
    public string? ExtractCandidateCodeFromJobApplication(string jobApplicationText)
    {
        if (string.IsNullOrWhiteSpace(jobApplicationText))
        {
            return null;
        }

        var match = CandidateCodeRegex.Match(jobApplicationText);
        if (match.Success)
        {
            // Extract the code without parentheses
            return match.Value.Trim('(', ')');
        }

        return null;
    }

    public string? ExtractCandidateNameFromJobApplication(string jobApplicationText)
    {
        if (string.IsNullOrWhiteSpace(jobApplicationText))
        {
            return null;
        }

        // Extract everything before the opening parenthesis
        // Example: "Alex Carter (C202510051593fe)" -> "Alex Carter"
        var match = Regex.Match(jobApplicationText, @"^(.+?)\s*\(");
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value.Trim();
        }

        return null;
    }

    public string RemoveEmailAddresses(string text)
    {
        return EmailRegex.Replace(text, "[EMAIL_REMOVED]");
    }

    public string RemovePhoneNumbers(string text)
    {
        return PhoneRegex.Replace(text, "[PHONE_REMOVED]");
    }

    public string RemoveZipCodes(string text)
    {
        return ZipCodeRegex.Replace(text, "[ZIP_REMOVED]");
    }

    public string RemoveNameOccurrences(string text, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return text;
        }

        var sanitized = text;
        
        // Split name into individual words
        var nameParts = name.Split(new[] { ' ', '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        
        _logger.LogInformation("Removing {Count} name parts from text", nameParts.Length);
        
        foreach (var part in nameParts)
        {
            if (part.Length >= 2) // Remove words 2+ characters (to handle initials and short names)
            {
                // Use word boundary matching to ensure we only replace complete words
                var pattern = $@"\b{Regex.Escape(part)}\b";
                var beforeCount = Regex.Matches(sanitized, pattern, RegexOptions.IgnoreCase).Count;
                
                if (beforeCount > 0)
                {
                    sanitized = Regex.Replace(sanitized, pattern, "[NAME_REMOVED]", RegexOptions.IgnoreCase);
                    _logger.LogInformation("Removed {Count} occurrences of a name part (length: {Length})", beforeCount, part.Length);
                }
            }
        }

        return sanitized;
    }

    public string RemoveAddressOccurrences(string text, string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return text;
        }

        // Remove full address
        var sanitized = Regex.Replace(text, Regex.Escape(address), "[ADDRESS_REMOVED]", RegexOptions.IgnoreCase);

        // Remove address parts (street, city, state)
        var addressParts = address.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in addressParts)
        {
            var trimmedPart = part.Trim();
            if (trimmedPart.Length >= 4) // Only remove parts 4+ characters
            {
                sanitized = Regex.Replace(sanitized, Regex.Escape(trimmedPart), "[ADDRESS_REMOVED]", RegexOptions.IgnoreCase);
            }
        }

        return sanitized;
    }

    /// <summary>
    /// Remove variations of a text (e.g., "john.doe" -> "john doe", "johndoe")
    /// </summary>
    private string RemoveTextVariations(string text, string textToRemove)
    {
        if (string.IsNullOrWhiteSpace(textToRemove))
        {
            return text;
        }

        // Create variations
        var variations = new List<string>
        {
            textToRemove,
            textToRemove.Replace(".", " "),
            textToRemove.Replace("_", " "),
            textToRemove.Replace("-", " "),
            textToRemove.Replace(".", ""),
            textToRemove.Replace("_", ""),
            textToRemove.Replace("-", "")
        };

        var sanitized = text;
        foreach (var variation in variations.Distinct())
        {
            if (variation.Length >= 4) // Only remove variations 4+ characters
            {
                sanitized = Regex.Replace(sanitized, $@"\b{Regex.Escape(variation)}\b", "[NAME_REMOVED]", RegexOptions.IgnoreCase);
            }
        }

        return sanitized;
    }

    /// <summary>
    /// Remove street addresses (patterns like "123 Main St", "456 Oak Avenue")
    /// </summary>
    private string RemoveStreetAddresses(string text)
    {
        // Pattern: Number followed by street name (e.g., "123 Main St", "456 Oak Avenue Apt 2B")
        var streetAddressRegex = new Regex(
            @"\b\d+\s+[A-Za-z\s]+(?:Street|St|Avenue|Ave|Road|Rd|Drive|Dr|Lane|Ln|Court|Ct|Boulevard|Blvd|Way|Place|Pl|Circle|Cir|Parkway|Pkwy|Terrace|Ter)(?:\s+(?:Apt|Apartment|Unit|Suite|Ste|#)\s*[A-Za-z0-9]+)?\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        return streetAddressRegex.Replace(text, "[ADDRESS_REMOVED]");
    }
}
