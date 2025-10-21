namespace RecruiterApi.Services;

/// <summary>
/// Service for removing Personally Identifiable Information (PII) from candidate data
/// </summary>
public interface IPiiSanitizationService
{
    /// <summary>
    /// Sanitize resume text by removing PII such as emails, phone numbers, addresses, and names (async with config check)
    /// </summary>
    Task<string> SanitizeResumeTextAsync(string resumeText, string? candidateName = null, string? email = null, string? address = null);
    
    /// <summary>
    /// Sanitize resume text by removing PII such as emails, phone numbers, addresses, and names (legacy sync method)
    /// </summary>
    string SanitizeResumeText(string resumeText, string? candidateName = null, string? email = null, string? address = null);
    
    /// <summary>
    /// Extract candidate code from job application text (e.g., "Dwight Shrute (C123454)" -> "C123454")
    /// </summary>
    string? ExtractCandidateCodeFromJobApplication(string jobApplicationText);
    
    /// <summary>
    /// Extract candidate name from job application text (e.g., "Dwight Shrute (C123454)" -> "Dwight Shrute")
    /// </summary>
    string? ExtractCandidateNameFromJobApplication(string jobApplicationText);
    
    /// <summary>
    /// Sanitize text by removing specific PII patterns
    /// </summary>
    string RemoveEmailAddresses(string text);
    string RemovePhoneNumbers(string text);
    string RemoveZipCodes(string text);
    string RemoveNameOccurrences(string text, string? name);
    string RemoveAddressOccurrences(string text, string? address);
}
