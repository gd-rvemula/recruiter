using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RecruiterApi.Services;

namespace RecruiterApi.Tests.Services;

public class PiiSanitizationServiceTests
{
    private readonly PiiSanitizationService _service;
    private readonly Mock<ILogger<PiiSanitizationService>> _mockLogger;

    public PiiSanitizationServiceTests()
    {
        _mockLogger = new Mock<ILogger<PiiSanitizationService>>();
        _service = new PiiSanitizationService(_mockLogger.Object);
    }

    [Theory]
    [InlineData("Contact me at john.doe@example.com", "Contact me at [EMAIL_REMOVED]")]
    [InlineData("Email: test@test.com and backup@backup.org", "Email: [EMAIL_REMOVED] and [EMAIL_REMOVED]")]
    [InlineData("No emails here", "No emails here")]
    public void RemoveEmailAddresses_ShouldRemoveAllEmailAddresses(string input, string expected)
    {
        // Act
        var result = _service.RemoveEmailAddresses(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Call me at 123-456-7890", "Call me at [PHONE_REMOVED]")]
    [InlineData("Phone: (555) 123-4567", "Phone: [PHONE_REMOVED]")]
    [InlineData("No phone numbers", "No phone numbers")]
    public void RemovePhoneNumbers_ShouldRemoveAllPhoneNumbers(string input, string expected)
    {
        // Act
        var result = _service.RemovePhoneNumbers(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Lives in 12345", "Lives in [ZIP_REMOVED]")]
    [InlineData("Zip: 90210-1234", "Zip: [ZIP_REMOVED]")]
    [InlineData("No zip codes", "No zip codes")]
    public void RemoveZipCodes_ShouldRemoveAllZipCodes(string input, string expected)
    {
        // Act
        var result = _service.RemoveZipCodes(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void RemoveNameOccurrences_ShouldRemoveNameFromText()
    {
        // Arrange
        var text = "John Doe is a great developer. John Doe has 5 years experience.";
        var name = "John Doe";

        // Act
        var result = _service.RemoveNameOccurrences(text, name);

        // Assert
        result.Should().NotContain("John Doe");
        result.Should().Contain("[NAME_REMOVED]");
    }

    [Theory]
    [InlineData("Lives at 123 Main Street", "123 Main Street")]
    [InlineData("Address: 456 Oak Avenue", "456 Oak Avenue")]
    public void RemoveAddressOccurrences_ShouldRemoveAddressFromText(string text, string address)
    {
        // Act
        var result = _service.RemoveAddressOccurrences(text, address);

        // Assert
        result.Should().NotContain(address);
        result.Should().Contain("[ADDRESS_REMOVED]");
    }

    [Fact]
    public void SanitizeResumeText_ShouldRemoveAllPII()
    {
        // Arrange
        var resumeText = @"John Doe
            Email: john.doe@example.com
            Phone: 555-123-4567
            Address: 123 Main Street, City, 12345
            
            Experienced developer with strong skills.";

        // Act
        var result = _service.SanitizeResumeText(resumeText, "John Doe", "john.doe@example.com", "123 Main Street");

        // Assert
        result.Should().NotContain("john.doe@example.com");
        result.Should().NotContain("555-123-4567");
        result.Should().NotContain("12345");
        result.Should().Contain("[EMAIL_REMOVED]");
        result.Should().Contain("[PHONE_REMOVED]");
    }

    [Fact]
    public void SanitizeResumeText_WithNullInput_ShouldReturnSameInput()
    {
        // Act
        var result = _service.SanitizeResumeText(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SanitizeResumeText_WithEmptyString_ShouldReturnEmptyString()
    {
        // Act
        var result = _service.SanitizeResumeText("");

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Dwight Shrute (C123456)", "C123456")]
    [InlineData("John Doe (C20250928abc123)", "C20250928abc123")]
    [InlineData("No code here", null)]
    public void ExtractCandidateCodeFromJobApplication_ShouldExtractCode(string input, string? expected)
    {
        // Act
        var result = _service.ExtractCandidateCodeFromJobApplication(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Dwight Shrute (C123456)", "Dwight Shrute")]
    [InlineData("John Doe (C20250928abc123)", "John Doe")]
    [InlineData("(C123456)", null)]
    public void ExtractCandidateNameFromJobApplication_ShouldExtractName(string input, string? expected)
    {
        // Act
        var result = _service.ExtractCandidateNameFromJobApplication(input);

        // Assert
        result.Should().Be(expected);
    }
}
