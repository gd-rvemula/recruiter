using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruiterApi.Data;
using RecruiterApi.Models;
using Foundatio.Queues;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace RecruiterApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmbeddingsController : ControllerBase
{
    private readonly RecruiterDbContext _context;
    private readonly IQueue<EmbeddingGenerationJob> _embeddingQueue;
    private readonly ILogger<EmbeddingsController> _logger;
    private readonly IConfiguration _configuration;

    public EmbeddingsController(
        RecruiterDbContext context,
        IQueue<EmbeddingGenerationJob> embeddingQueue,
        ILogger<EmbeddingsController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _embeddingQueue = embeddingQueue;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Trigger embedding generation for all candidates missing embeddings
    /// </summary>
    [HttpPost("generate-missing")]
    public async Task<ActionResult> GenerateMissingEmbeddings()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            // Find all candidates without embeddings using raw SQL
            var candidatesWithoutEmbeddings = new List<(Guid Id, string FirstName, string LastName, string? CurrentTitle)>();
            
            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
                    SELECT id, first_name, last_name, current_title 
                    FROM candidates 
                    WHERE is_active = true AND profile_embedding IS NULL
                    LIMIT 1000";
                
                await using var command = new NpgsqlCommand(sql, connection);
                await using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    candidatesWithoutEmbeddings.Add((
                        reader.GetGuid(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.IsDBNull(3) ? null : reader.GetString(3)
                    ));
                }
            }

            if (candidatesWithoutEmbeddings.Count == 0)
            {
                return Ok(new
                {
                    message = "All active candidates already have embeddings",
                    candidatesQueued = 0
                });
            }

            _logger.LogInformation(
                "Queueing embedding generation for {Count} candidates",
                candidatesWithoutEmbeddings.Count
            );

            // Queue embedding generation jobs for each candidate
            var queuedCount = 0;
            foreach (var candidate in candidatesWithoutEmbeddings)
            {
                // Build profile text from candidate data
                var profileText = await BuildProfileTextAsync(candidate.Id);

                await _embeddingQueue.EnqueueAsync(new EmbeddingGenerationJob
                {
                    CandidateId = candidate.Id,
                    ProfileText = profileText,
                    Source = "API-GenerateMissing",
                    QueuedAt = DateTime.UtcNow,
                    RetryCount = 0,
                    MaxRetries = 3
                });

                queuedCount++;

                // Log progress every 50 candidates
                if (queuedCount % 50 == 0)
                {
                    _logger.LogInformation(
                        "Queued {QueuedCount}/{TotalCount} embedding jobs",
                        queuedCount,
                        candidatesWithoutEmbeddings.Count
                    );
                }
            }

            _logger.LogInformation(
                "Successfully queued {Count} embedding generation jobs",
                queuedCount
            );

            return Ok(new
            {
                message = $"Successfully queued embedding generation for {queuedCount} candidates",
                candidatesQueued = queuedCount,
                estimatedTimeMinutes = Math.Ceiling(queuedCount / 10.0) // Assuming ~10 embeddings/minute
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing embedding generation jobs");
            return StatusCode(500, new
            {
                message = "Failed to queue embedding generation jobs",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get embedding generation status
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult> GetEmbeddingStatus()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            int totalActive = 0;
            int withEmbeddings = 0;
            
            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sql = @"
                    SELECT COUNT(*) as total_active,
                           COUNT(profile_embedding) as with_embeddings
                    FROM candidates 
                    WHERE is_active = true";
                
                await using var command = new NpgsqlCommand(sql, connection);
                await using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    totalActive = reader.GetInt32(0);
                    withEmbeddings = reader.GetInt32(1);
                }
            }

            var withoutEmbeddings = totalActive - withEmbeddings;
            var coveragePercent = totalActive > 0 
                ? Math.Round((double)withEmbeddings / totalActive * 100, 2) 
                : 0;

            // Get queue stats
            var queueInfo = await _embeddingQueue.GetQueueStatsAsync();

            return Ok(new
            {
                totalActiveCandidates = totalActive,
                withEmbeddings,
                withoutEmbeddings,
                coveragePercent,
                queueStats = new
                {
                    queued = queueInfo.Queued,
                    working = queueInfo.Working,
                    completed = queueInfo.Completed,
                    deadletter = queueInfo.Deadletter
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting embedding status");
            return StatusCode(500, new
            {
                message = "Failed to get embedding status",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Build profile text from candidate data for embedding generation
    /// </summary>
    private async Task<string> BuildProfileTextAsync(Guid candidateId)
    {
        var candidate = await _context.Candidates
            .Include(c => c.CandidateSkills)
                .ThenInclude(cs => cs.Skill)
            .Include(c => c.WorkExperiences)
            .Include(c => c.Education)
            .Include(c => c.Resumes)
            .FirstOrDefaultAsync(c => c.Id == candidateId);

        if (candidate == null)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        // Basic info
        parts.Add($"{candidate.FirstName} {candidate.LastName}");
        
        if (!string.IsNullOrEmpty(candidate.CurrentTitle))
            parts.Add(candidate.CurrentTitle);

        // Skills
        if (candidate.CandidateSkills?.Any() == true)
        {
            var skills = string.Join(", ", candidate.CandidateSkills
                .Select(cs => cs.Skill?.SkillName)
                .Where(s => !string.IsNullOrEmpty(s)));
            
            if (!string.IsNullOrEmpty(skills))
                parts.Add($"Skills: {skills}");
        }

        // Work experience
        if (candidate.WorkExperiences?.Any() == true)
        {
            var experiences = string.Join(". ", candidate.WorkExperiences
                .OrderByDescending(we => we.StartDate)
                .Take(3)
                .Select(we => $"{we.JobTitle} at {we.CompanyName}"));
            
            if (!string.IsNullOrEmpty(experiences))
                parts.Add(experiences);
        }

        // Education
        if (candidate.Education?.Any() == true)
        {
            var education = string.Join(". ", candidate.Education
                .OrderByDescending(e => e.EndDate)
                .Take(2)
                .Select(e => $"{e.DegreeName} from {e.InstitutionName}"));
            
            if (!string.IsNullOrEmpty(education))
                parts.Add(education);
        }

        // Resume text (if available)
        var latestResume = candidate.Resumes?
            .OrderByDescending(r => r.UploadedAt)
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(latestResume?.ResumeText) && latestResume.ResumeText.Length > 0)
        {
            // Take first 500 characters of resume text
            var resumeText = latestResume.ResumeText.Length > 500 
                ? latestResume.ResumeText.Substring(0, 500) 
                : latestResume.ResumeText;
            parts.Add(resumeText);
        }

        return string.Join(". ", parts);
    }
}
