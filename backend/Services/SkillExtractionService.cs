using RecruiterApi.Data;
using RecruiterApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace RecruiterApi.Services;

public interface ISkillExtractionService
{
    Task<List<CandidateSkill>> ExtractSkillsFromResumeTextAsync(Guid candidateId, string resumeText, int totalYearsExperience);
    Task<Dictionary<string, int>> GetWordFrequencyFromTextAsync(string text);
    Task<List<string>> MatchWordsToSkillsAsync(Dictionary<string, int> wordFrequency);
}

public class SkillExtractionService : ISkillExtractionService
{
    private readonly RecruiterDbContext _context;
    private readonly ILogger<SkillExtractionService> _logger;

    public SkillExtractionService(RecruiterDbContext context, ILogger<SkillExtractionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<CandidateSkill>> ExtractSkillsFromResumeTextAsync(Guid candidateId, string resumeText, int totalYearsExperience)
    {
        if (string.IsNullOrWhiteSpace(resumeText))
        {
            return new List<CandidateSkill>();
        }

        try
        {
            // Get word frequency from resume text
            var wordFrequency = await GetWordFrequencyFromTextAsync(resumeText);
            
            // Match words to skills
            var matchedSkillNames = await MatchWordsToSkillsAsync(wordFrequency);
            
            // Get skill entities from database
            var skills = await _context.Skills
                .Where(s => matchedSkillNames.Contains(s.SkillName))
                .ToListAsync();

            var candidateSkills = new List<CandidateSkill>();

            foreach (var skill in skills)
            {
                var proficiencyLevel = DetermineProficiencyLevel(totalYearsExperience, skill.SkillName);
                var yearsOfExperience = CalculateYearsOfExperience(totalYearsExperience, skill.SkillName);

                candidateSkills.Add(new CandidateSkill
                {
                    Id = Guid.NewGuid(),
                    CandidateId = candidateId,
                    SkillId = skill.Id,
                    ProficiencyLevel = proficiencyLevel,
                    YearsOfExperience = yearsOfExperience,
                    IsExtracted = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            _logger.LogInformation("Extracted {SkillCount} skills for candidate {CandidateId}", candidateSkills.Count, candidateId);
            return candidateSkills;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting skills for candidate {CandidateId}", candidateId);
            return new List<CandidateSkill>();
        }
    }

    public async Task<Dictionary<string, int>> GetWordFrequencyFromTextAsync(string text)
    {
        var wordFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        if (string.IsNullOrWhiteSpace(text))
        {
            return wordFrequency;
        }

        // Clean and normalize text
        var cleanText = CleanText(text);
        
        // Extract words (including compound words and phrases)
        var words = ExtractWords(cleanText);
        var phrases = ExtractPhrases(cleanText);
        
        // Count word frequencies
        foreach (var word in words)
        {
            if (IsValidSkillWord(word))
            {
                wordFrequency[word] = wordFrequency.GetValueOrDefault(word, 0) + 1;
            }
        }
        
        // Count phrase frequencies
        foreach (var phrase in phrases)
        {
            if (IsValidSkillPhrase(phrase))
            {
                wordFrequency[phrase] = wordFrequency.GetValueOrDefault(phrase, 0) + 1;
            }
        }

        return wordFrequency;
    }

    public async Task<List<string>> MatchWordsToSkillsAsync(Dictionary<string, int> wordFrequency)
    {
        // Get all skills from database
        var allSkills = await _context.Skills.Select(s => s.SkillName).ToListAsync();
        var matchedSkills = new List<string>();

        // Create skill variations map for better matching
        var skillVariations = CreateSkillVariationsMap(allSkills);

        foreach (var word in wordFrequency.Keys)
        {
            // Direct skill name match
            if (allSkills.Any(skill => string.Equals(skill, word, StringComparison.OrdinalIgnoreCase)))
            {
                var matchedSkill = allSkills.First(skill => string.Equals(skill, word, StringComparison.OrdinalIgnoreCase));
                if (!matchedSkills.Contains(matchedSkill))
                {
                    matchedSkills.Add(matchedSkill);
                }
            }
            
            // Skill variation match
            if (skillVariations.ContainsKey(word.ToLower()))
            {
                var matchedSkill = skillVariations[word.ToLower()];
                if (!matchedSkills.Contains(matchedSkill))
                {
                    matchedSkills.Add(matchedSkill);
                }
            }
        }

        return matchedSkills;
    }

    private string CleanText(string text)
    {
        // Remove special characters, normalize whitespace
        text = Regex.Replace(text, @"[^\w\s\-\.#\+/]", " ");
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    private List<string> ExtractWords(string text)
    {
        // Extract individual words
        var words = Regex.Matches(text, @"\b[\w\-#\+\.]+\b")
            .Cast<Match>()
            .Select(m => m.Value)
            .Where(w => w.Length >= 2) // Minimum word length
            .ToList();

        return words;
    }

    private List<string> ExtractPhrases(string text)
    {
        // Extract common technology phrases (2-4 words)
        var phrases = new List<string>();
        
        // Extract 2-word phrases
        var twoWordMatches = Regex.Matches(text, @"\b[\w\-#\+\.]+\s+[\w\-#\+\.]+\b");
        phrases.AddRange(twoWordMatches.Cast<Match>().Select(m => m.Value));
        
        // Extract 3-word phrases
        var threeWordMatches = Regex.Matches(text, @"\b[\w\-#\+\.]+\s+[\w\-#\+\.]+\s+[\w\-#\+\.]+\b");
        phrases.AddRange(threeWordMatches.Cast<Match>().Select(m => m.Value));

        return phrases.Distinct().ToList();
    }

    private bool IsValidSkillWord(string word)
    {
        // Filter out common words that are not skills
        var commonWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
            "from", "up", "about", "into", "through", "during", "before", "after", "above",
            "below", "over", "under", "again", "further", "then", "once", "here", "there",
            "when", "where", "why", "how", "all", "any", "both", "each", "few", "more",
            "most", "other", "some", "such", "no", "nor", "not", "only", "own", "same",
            "so", "than", "too", "very", "can", "will", "just", "should", "now", "work",
            "experience", "years", "team", "project", "company", "role", "position"
        };

        return !commonWords.Contains(word) && word.Length >= 2;
    }

    private bool IsValidSkillPhrase(string phrase)
    {
        // Check if phrase might be a skill (contains technical terms)
        var technicalIndicators = new[] 
        {
            "development", "engineer", "framework", "language", "database", "cloud",
            "platform", "server", "client", "web", "mobile", "api", "testing", "management"
        };

        return technicalIndicators.Any(indicator => 
            phrase.Contains(indicator, StringComparison.OrdinalIgnoreCase));
    }

    private Dictionary<string, string> CreateSkillVariationsMap(List<string> allSkills)
    {
        var variations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var skill in allSkills)
        {
            // Add the skill itself
            variations[skill.ToLower()] = skill;
            
            // Add common variations
            switch (skill)
            {
                case "C#":
                    variations["c sharp"] = skill;
                    variations["csharp"] = skill;
                    break;
                case ".NET":
                    variations["dotnet"] = skill;
                    variations["dot net"] = skill;
                    break;
                case "JavaScript":
                    variations["js"] = skill;
                    break;
                case "TypeScript":
                    variations["ts"] = skill;
                    break;
                case "React":
                    variations["reactjs"] = skill;
                    variations["react.js"] = skill;
                    break;
                case "Node.js":
                    variations["nodejs"] = skill;
                    variations["node js"] = skill;
                    break;
                case "Vue.js":
                    variations["vuejs"] = skill;
                    variations["vue"] = skill;
                    break;
                case "Angular":
                    variations["angularjs"] = skill;
                    break;
                case "PostgreSQL":
                    variations["postgres"] = skill;
                    variations["psql"] = skill;
                    break;
                case "MongoDB":
                    variations["mongo"] = skill;
                    break;
                case "Kubernetes":
                    variations["k8s"] = skill;
                    break;
                case "Machine Learning":
                    variations["ml"] = skill;
                    break;
                case "AI/ML":
                    variations["artificial intelligence"] = skill;
                    variations["ai"] = skill;
                    break;
                case "Spring Boot":
                    variations["spring framework"] = skill;
                    variations["spring"] = skill;
                    break;
                case "Express.js":
                    variations["express"] = skill;
                    break;
                case "Microsoft SQL Server":
                    variations["sql server"] = skill;
                    variations["mssql"] = skill;
                    break;
                case "HTML5":
                    variations["html"] = skill;
                    break;
                case "CSS3":
                    variations["css"] = skill;
                    break;
                case "SASS/SCSS":
                    variations["sass"] = skill;
                    variations["scss"] = skill;
                    break;
                case "iOS Development":
                    variations["ios"] = skill;
                    break;
                case "Android Development":
                    variations["android"] = skill;
                    break;
                case "AWS":
                    variations["amazon web services"] = skill;
                    break;
                case "Microsoft Azure":
                    variations["azure"] = skill;
                    break;
                case "Google Cloud Platform":
                    variations["google cloud"] = skill;
                    variations["gcp"] = skill;
                    break;
                case "Agile/Scrum":
                    variations["agile"] = skill;
                    variations["scrum"] = skill;
                    break;
                case "TDD":
                    variations["test driven development"] = skill;
                    break;
                case "BDD":
                    variations["behavior driven development"] = skill;
                    break;
                case "Copilot":
                    variations["github copilot"] = skill;
                    break;
                case "Claude":
                    variations["claude ai"] = skill;
                    break;
            }
        }

        return variations;
    }

    private string DetermineProficiencyLevel(int totalYearsExperience, string skillName)
    {
        // Leadership skills typically require more experience
        var leadershipSkills = new[] { "Leadership", "Team Management", "Project Management", "Strategic Planning", "Stakeholder Management" };
        
        if (leadershipSkills.Contains(skillName))
        {
            return totalYearsExperience switch
            {
                >= 12 => "Expert",
                >= 8 => "Advanced",
                >= 5 => "Intermediate",
                _ => "Beginner"
            };
        }

        // Technical skills
        return totalYearsExperience switch
        {
            >= 10 => "Expert",
            >= 6 => "Advanced",
            >= 3 => "Intermediate",
            _ => "Beginner"
        };
    }

    private int CalculateYearsOfExperience(int totalYearsExperience, string skillName)
    {
        var leadershipSkills = new[] { "Leadership", "Team Management", "Project Management", "Strategic Planning", "Stakeholder Management" };
        
        if (leadershipSkills.Contains(skillName))
        {
            // Leadership skills are typically acquired later in career
            return Math.Max(1, Math.Min(totalYearsExperience - 2, totalYearsExperience / 2));
        }

        // For technical skills, assume 80% of total experience
        return Math.Max(1, (int)(totalYearsExperience * 0.8));
    }
}