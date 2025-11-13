using Microsoft.EntityFrameworkCore;
using RecruiterApi.Data;
using RecruiterApi.DTOs;
using RecruiterApi.Models;

namespace RecruiterApi.Services;

/// <summary>
/// Service responsible for candidate statistics and dashboard metrics
/// Keeps statistics logic separate from controller and search logic
/// </summary>
public interface ICandidateStatisticsService
{
    Task<Dictionary<string, int>> GetStatusTotalsAsync();
    Task<List<SkillFrequencyDto>> GetSkillsFrequencyAsync(int limit = 50);
    Task<SystemStatisticsDto> GetSystemStatisticsAsync();
}

public class CandidateStatisticsService : ICandidateStatisticsService
{
    private readonly RecruiterDbContext _context;
    private readonly ILogger<CandidateStatisticsService> _logger;

    public CandidateStatisticsService(
        RecruiterDbContext context,
        ILogger<CandidateStatisticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get status totals for dashboard display
    /// Includes all possible statuses even if count is 0
    /// </summary>
    public async Task<Dictionary<string, int>> GetStatusTotalsAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving candidate status totals");

            // Get actual status counts from database
            var statusCounts = await _context.Candidates
                .Where(c => c.IsActive)
                .GroupBy(c => c.CurrentStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status ?? "Unknown", x => x.Count);

            // Ensure all possible statuses are included with 0 count if not present
            var allStatuses = CandidateStatusExtensions.GetAllStatuses();
            var result = new Dictionary<string, int>();

            foreach (var status in allStatuses)
            {
                result[status] = statusCounts.ContainsKey(status) ? statusCounts[status] : 0;
            }

            // Add any custom statuses that exist in database but not in enum
            foreach (var kvp in statusCounts)
            {
                if (!result.ContainsKey(kvp.Key))
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            _logger.LogInformation("Retrieved status totals for {StatusCount} statuses", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving candidate status totals");
            throw;
        }
    }

    /// <summary>
    /// Get skills frequency for analytics and insights
    /// </summary>
    public async Task<List<SkillFrequencyDto>> GetSkillsFrequencyAsync(int limit = 50)
    {
        try
        {
            _logger.LogInformation("Retrieving skills frequency with limit {Limit}", limit);

            var skillsFrequency = await _context.CandidateSkills
                .Include(cs => cs.Skill)
                .Where(cs => cs.Candidate.IsActive && cs.Skill != null)
                .GroupBy(cs => cs.Skill!.SkillName)
                .Select(g => new SkillFrequencyDto
                {
                    Text = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(sf => sf.Value)
                .Take(limit)
                .ToListAsync();

            _logger.LogInformation("Retrieved {SkillCount} skills frequency entries", skillsFrequency.Count);
            return skillsFrequency;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving skills frequency");
            throw;
        }
    }

    /// <summary>
    /// Get system-wide statistics for monitoring and dashboards
    /// </summary>
    public async Task<SystemStatisticsDto> GetSystemStatisticsAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving system statistics");

            var totalCandidates = await _context.Candidates
                .CountAsync(c => c.IsActive);

            // Count candidates with embeddings (assuming embedding exists means they have embeddings)
            var withEmbeddings = await _context.Candidates
                .Where(c => c.IsActive)
                // This is a placeholder - adjust based on your embedding storage strategy
                .CountAsync();

            var coveragePercent = totalCandidates > 0 
                ? Math.Round((double)withEmbeddings * 100.0 / totalCandidates, 2)
                : 0.0;

            var statistics = new SystemStatisticsDto
            {
                TotalCandidates = totalCandidates,
                WithEmbeddings = withEmbeddings,
                CoveragePercent = coveragePercent,
                LastUpdated = DateTime.UtcNow
            };

            _logger.LogInformation("System statistics: {TotalCandidates} total, {WithEmbeddings} with embeddings ({CoveragePercent}%)",
                statistics.TotalCandidates, statistics.WithEmbeddings, statistics.CoveragePercent);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system statistics");
            throw;
        }
    }
}