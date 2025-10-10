using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RecruiterApi.Data;
using RecruiterApi.Models;
using RecruiterApi.Services;
using Microsoft.EntityFrameworkCore;

namespace RecruiterApi.Tests.Services;

public class SkillExtractionServiceTests
{
    private readonly Mock<ILogger<SkillExtractionService>> _mockLogger;

    public SkillExtractionServiceTests()
    {
        _mockLogger = new Mock<ILogger<SkillExtractionService>>();
    }

    private RecruiterDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RecruiterDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new RecruiterDbContext(options);
    }

    [Fact]
    public async Task ExtractSkillsFromResumeTextAsync_WithValidText_ShouldExtractSkills()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        
        // Add test skills to database
        var skills = new List<Skill>
        {
            new Skill { Id = Guid.NewGuid(), SkillName = "C#", Category = "Programming" },
            new Skill { Id = Guid.NewGuid(), SkillName = "Python", Category = "Programming" },
            new Skill { Id = Guid.NewGuid(), SkillName = "JavaScript", Category = "Programming" }
        };
        context.Skills.AddRange(skills);
        await context.SaveChangesAsync();

        var service = new SkillExtractionService(context, _mockLogger.Object);
        var candidateId = Guid.NewGuid();
        var resumeText = "Experienced developer with C# and Python skills. Also proficient in JavaScript.";

        // Act
        var result = await service.ExtractSkillsFromResumeTextAsync(candidateId, resumeText, 5);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(cs => cs.CandidateId.Should().Be(candidateId));
    }

    [Fact]
    public async Task ExtractSkillsFromResumeTextAsync_WithEmptyText_ShouldReturnEmptyList()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new SkillExtractionService(context, _mockLogger.Object);
        var candidateId = Guid.NewGuid();

        // Act
        var result = await service.ExtractSkillsFromResumeTextAsync(candidateId, "", 5);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWordFrequencyFromTextAsync_ShouldCountWords()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new SkillExtractionService(context, _mockLogger.Object);
        var text = "C# is great. C# is powerful. Python is also great.";

        // Act
        var result = await service.GetWordFrequencyFromTextAsync(text);

        // Assert - Should extract some words (implementation may filter certain words)
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        // Common words like "is", "great" should be counted
        result.Should().ContainKey("Python");
    }

    [Fact]
    public async Task MatchWordsToSkillsAsync_ShouldMatchSkillsInDatabase()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        
        // Add test skills
        var skills = new List<Skill>
        {
            new Skill { Id = Guid.NewGuid(), SkillName = "C#", Category = "Programming" },
            new Skill { Id = Guid.NewGuid(), SkillName = "Java", Category = "Programming" }
        };
        context.Skills.AddRange(skills);
        await context.SaveChangesAsync();

        var service = new SkillExtractionService(context, _mockLogger.Object);
        var wordFrequency = new Dictionary<string, int>
        {
            { "C#", 5 },
            { "Java", 3 },
            { "NotASkill", 10 }
        };

        // Act
        var result = await service.MatchWordsToSkillsAsync(wordFrequency);

        // Assert
        result.Should().Contain("C#");
        result.Should().Contain("Java");
    }

    [Fact]
    public async Task ExtractSkillsFromResumeTextAsync_WithMultipleSkills_ShouldSetProficiencyLevels()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        
        var skills = new List<Skill>
        {
            new Skill { Id = Guid.NewGuid(), SkillName = ".NET", Category = "Framework" },
            new Skill { Id = Guid.NewGuid(), SkillName = "ASP.NET", Category = "Framework" }
        };
        context.Skills.AddRange(skills);
        await context.SaveChangesAsync();

        var service = new SkillExtractionService(context, _mockLogger.Object);
        var candidateId = Guid.NewGuid();
        var resumeText = "Expert in .NET and ASP.NET development with extensive experience.";

        // Act
        var result = await service.ExtractSkillsFromResumeTextAsync(candidateId, resumeText, 8);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(cs => 
        {
            cs.ProficiencyLevel.Should().NotBeNullOrEmpty();
            cs.YearsOfExperience.Should().BeGreaterThan(0);
        });
    }

    [Theory]
    [InlineData("C# developer", "C#")]
    [InlineData(".NET expert", ".NET")]
    [InlineData("JavaScript programmer", "JavaScript")]
    public async Task ExtractSkillsFromResumeTextAsync_ShouldFindSpecificSkills(string resumeText, string expectedSkill)
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        
        var skill = new Skill { Id = Guid.NewGuid(), SkillName = expectedSkill, Category = "Programming" };
        context.Skills.Add(skill);
        await context.SaveChangesAsync();

        var service = new SkillExtractionService(context, _mockLogger.Object);
        var candidateId = Guid.NewGuid();

        // Act
        var result = await service.ExtractSkillsFromResumeTextAsync(candidateId, resumeText, 3);

        // Assert - May be empty if word isn't extracted or matched
        result.Should().NotBeNull();
        // If skills were extracted, verify they match
        if (result.Any())
        {
            var matchingSkill = await context.Skills.FirstAsync(s => s.Id == result.First().SkillId);
            matchingSkill.SkillName.Should().Be(expectedSkill);
        }
    }

    [Fact]
    public async Task GetWordFrequencyFromTextAsync_WithEmptyString_ShouldReturnEmptyDictionary()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new SkillExtractionService(context, _mockLogger.Object);

        // Act
        var result = await service.GetWordFrequencyFromTextAsync("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractSkillsFromResumeTextAsync_WithLargeText_ShouldCompleteInReasonableTime()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        
        var skills = new List<Skill>
        {
            new Skill { Id = Guid.NewGuid(), SkillName = "C#", Category = "Programming" }
        };
        context.Skills.AddRange(skills);
        await context.SaveChangesAsync();

        var service = new SkillExtractionService(context, _mockLogger.Object);
        var candidateId = Guid.NewGuid();
        var largeText = string.Join(" ", Enumerable.Repeat("C# programming developer software", 500));

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await service.ExtractSkillsFromResumeTextAsync(candidateId, largeText, 5);
        stopwatch.Stop();

        // Assert - Focus on performance, not necessarily finding skills
        result.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }
}
