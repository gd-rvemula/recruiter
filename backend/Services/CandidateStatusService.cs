using Microsoft.EntityFrameworkCore;
using RecruiterApi.Data;
using RecruiterApi.DTOs;
using RecruiterApi.Models;

namespace RecruiterApi.Services;

/// <summary>
/// Service responsible for candidate status management and history tracking
/// Separated from controller to maintain single responsibility
/// </summary>
public interface ICandidateStatusService
{
    Task<CandidateStatusDto> GetCandidateStatusAsync(Guid candidateId);
    Task<CandidateStatusDto> UpdateCandidateStatusAsync(Guid candidateId, string newStatus, string? updatedBy = null);
    Task<List<CandidateStatusHistoryDto>> GetStatusHistoryAsync(Guid candidateId);
}

public class CandidateStatusService : ICandidateStatusService
{
    private readonly RecruiterDbContext _context;
    private readonly ILogger<CandidateStatusService> _logger;

    public CandidateStatusService(
        RecruiterDbContext context,
        ILogger<CandidateStatusService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get current status of a specific candidate
    /// </summary>
    public async Task<CandidateStatusDto> GetCandidateStatusAsync(Guid candidateId)
    {
        try
        {
            _logger.LogInformation("Retrieving status for candidate {CandidateId}", candidateId);

            var candidate = await _context.Candidates
                .Where(c => c.Id == candidateId)
                .Select(c => new CandidateStatusDto
                {
                    CandidateId = c.Id,
                    CandidateName = c.FullName ?? $"{c.FirstName} {c.LastName}",
                    CurrentStatus = c.CurrentStatus ?? "New",
                    StatusUpdatedAt = c.StatusUpdatedAt,
                    StatusUpdatedBy = c.StatusUpdatedBy
                })
                .FirstOrDefaultAsync();

            if (candidate == null)
            {
                throw new ArgumentException($"Candidate with ID {candidateId} not found");
            }

            _logger.LogInformation("Retrieved status '{Status}' for candidate {CandidateId}", 
                candidate.CurrentStatus, candidateId);

            return candidate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving candidate status for {CandidateId}", candidateId);
            throw;
        }
    }

    /// <summary>
    /// Update candidate status with history tracking
    /// </summary>
    public async Task<CandidateStatusDto> UpdateCandidateStatusAsync(Guid candidateId, string newStatus, string? updatedBy = null)
    {
        try
        {
            _logger.LogInformation("Updating status for candidate {CandidateId} to '{NewStatus}'", candidateId, newStatus);

            // Validate status
            if (!CandidateStatusExtensions.IsValidStatus(newStatus))
            {
                throw new ArgumentException($"Invalid status: {newStatus}");
            }

            var candidate = await _context.Candidates.FindAsync(candidateId);
            if (candidate == null)
            {
                throw new ArgumentException($"Candidate with ID {candidateId} not found");
            }

            var oldStatus = candidate.CurrentStatus;

            // Only update if status actually changed
            if (oldStatus != newStatus)
            {
                // Update candidate status
                candidate.CurrentStatus = newStatus;
                candidate.StatusUpdatedAt = DateTime.UtcNow;
                candidate.StatusUpdatedBy = updatedBy ?? "System";
                candidate.UpdatedAt = DateTime.UtcNow;

                // Create status history entry
                var statusHistory = new CandidateStatusHistory
                {
                    Id = Guid.NewGuid(),
                    CandidateId = candidateId,
                    PreviousStatus = oldStatus ?? "Unknown",
                    NewStatus = newStatus,
                    CreatedAt = DateTime.UtcNow,
                    ChangedBy = updatedBy ?? "System",
                    ChangeReason = "Status update via API"
                };

                _context.CandidateStatusHistory.Add(statusHistory);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated candidate {CandidateId} status from '{OldStatus}' to '{NewStatus}'", 
                    candidateId, oldStatus, newStatus);
            }

            return new CandidateStatusDto
            {
                CandidateId = candidate.Id,
                CandidateName = candidate.FullName ?? $"{candidate.FirstName} {candidate.LastName}",
                CurrentStatus = candidate.CurrentStatus ?? "New",
                StatusUpdatedAt = candidate.StatusUpdatedAt,
                StatusUpdatedBy = candidate.StatusUpdatedBy
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating candidate status for {CandidateId}", candidateId);
            throw;
        }
    }

    /// <summary>
    /// Get complete status history for a candidate
    /// </summary>
    public async Task<List<CandidateStatusHistoryDto>> GetStatusHistoryAsync(Guid candidateId)
    {
        try
        {
            _logger.LogInformation("Retrieving status history for candidate {CandidateId}", candidateId);

            // Verify candidate exists
            var candidateExists = await _context.Candidates.AnyAsync(c => c.Id == candidateId);
            if (!candidateExists)
            {
                throw new ArgumentException($"Candidate with ID {candidateId} not found");
            }

            var statusHistory = await _context.CandidateStatusHistory
                .Where(sh => sh.CandidateId == candidateId)
                .OrderByDescending(sh => sh.CreatedAt)
                .Select(sh => new CandidateStatusHistoryDto
                {
                    Id = sh.Id,
                    CandidateId = sh.CandidateId,
                    PreviousStatus = sh.PreviousStatus ?? "Unknown",
                    NewStatus = sh.NewStatus ?? "Unknown",
                    ChangedAt = sh.CreatedAt,
                    ChangedBy = sh.ChangedBy ?? "System",
                    Reason = sh.ChangeReason
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {HistoryCount} status history entries for candidate {CandidateId}", 
                statusHistory.Count, candidateId);

            return statusHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving status history for candidate {CandidateId}", candidateId);
            throw;
        }
    }
}