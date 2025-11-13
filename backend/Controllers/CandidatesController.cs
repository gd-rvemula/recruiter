using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruiterApi.Data;
using RecruiterApi.DTOs;
using RecruiterApi.Services;

namespace RecruiterApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CandidatesController : ControllerBase
{
    private readonly RecruiterDbContext _context;
    private readonly ILogger<CandidatesController> _logger;
    private readonly CandidateSearchService _candidateSearchService;
    private readonly ICandidateStatisticsService _statisticsService;
    private readonly ICandidateStatusService _statusService;
    private const string CLIENT_ID_HEADER = "X-Client-ID";
    private const string DEFAULT_CLIENT_ID = "GLOBAL";

    public CandidatesController(
        RecruiterDbContext context, 
        ILogger<CandidatesController> logger,
        CandidateSearchService candidateSearchService,
        ICandidateStatisticsService statisticsService,
        ICandidateStatusService statusService)
    {
        _context = context;
        _logger = logger;
        _candidateSearchService = candidateSearchService;
        _statisticsService = statisticsService;
        _statusService = statusService;
    }

    /// <summary>
    /// Get client ID from request header (defaults to GLOBAL)
    /// </summary>
    private string GetClientId()
    {
        if (Request.Headers.TryGetValue(CLIENT_ID_HEADER, out var clientId) && 
            !string.IsNullOrWhiteSpace(clientId))
        {
            return clientId.ToString();
        }
        return DEFAULT_CLIENT_ID;
    }

    /// <summary>
    /// Search candidates using the refactored search service with strategy pattern
    /// Supports multiple search modes: semantic, nameMatch, and auto-detection
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<CandidateSearchResponse>> SearchCandidates(CandidateSearchRequest request)
    {
        try
        {
            _logger.LogInformation("Search request received: {SearchMode} mode for term: '{SearchTerm}'", 
                request.SearchMode ?? "auto", request.SearchTerm);

            var response = await _candidateSearchService.SearchCandidatesAsync(request);
            
            _logger.LogInformation("Search completed: {TotalCount} results in {TotalPages} pages", 
                response.TotalCount, response.TotalPages);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching candidates with request: {@Request}", request);
            return StatusCode(500, new { message = "Error searching candidates", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all candidates with optional pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CandidateDto>>> GetCandidates(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var candidates = await _context.Candidates
                .Where(c => c.IsActive)
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CandidateDto
                {
                    Id = c.Id,
                    CandidateCode = c.CandidateCode ?? "",
                    FirstName = c.FirstName ?? "",
                    LastName = c.LastName ?? "",
                    FullName = c.FullName ?? "",
                    Email = c.Email ?? "",
                    Phone = c.Phone,
                    CurrentTitle = c.CurrentTitle,
                    TotalYearsExperience = c.TotalYearsExperience,
                    SalaryExpectation = c.SalaryExpectation,
                    IsAuthorizedToWork = c.IsAuthorizedToWork,
                    NeedsSponsorship = c.NeedsSponsorship,
                    IsActive = c.IsActive,
                    CurrentStatus = c.CurrentStatus ?? "New",
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return Ok(candidates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving candidates");
            return StatusCode(500, new { message = "Error retrieving candidates", error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific candidate by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CandidateDto>> GetCandidate(Guid id)
    {
        try
        {
            var candidate = await _context.Candidates
                .Include(c => c.Resumes)
                .Include(c => c.WorkExperiences)
                .Include(c => c.Education)
                .Include(c => c.CandidateSkills)
                    .ThenInclude(cs => cs.Skill)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (candidate == null)
            {
                return NotFound(new { message = "Candidate not found" });
            }

            var candidateDto = new CandidateDto
            {
                Id = candidate.Id,
                CandidateCode = candidate.CandidateCode ?? "",
                FirstName = candidate.FirstName ?? "",
                LastName = candidate.LastName ?? "",
                FullName = candidate.FullName ?? "",
                Email = candidate.Email ?? "",
                Phone = candidate.Phone,
                CurrentTitle = candidate.CurrentTitle,
                TotalYearsExperience = candidate.TotalYearsExperience,
                SalaryExpectation = candidate.SalaryExpectation,
                IsAuthorizedToWork = candidate.IsAuthorizedToWork,
                NeedsSponsorship = candidate.NeedsSponsorship,
                IsActive = candidate.IsActive,
                CurrentStatus = candidate.CurrentStatus ?? "New",
                CreatedAt = candidate.CreatedAt,
                UpdatedAt = candidate.UpdatedAt,
                Resumes = candidate.Resumes.Select(r => new ResumeDto
                {
                    Id = r.Id,
                    FileName = r.FileName,
                    FileType = r.FileType,
                    FileSize = r.FileSize,
                    FilePath = r.FilePath,
                    ResumeText = r.ResumeText,
                    ResumeTextProcessed = r.ResumeTextProcessed,
                    IsProcessed = r.IsProcessed,
                    ProcessingStatus = r.ProcessingStatus,
                    UploadedAt = r.UploadedAt,
                    ProcessedAt = r.ProcessedAt
                }).ToList(),
                WorkExperiences = candidate.WorkExperiences.Select(we => new WorkExperienceDto
                {
                    Id = we.Id,
                    CompanyName = we.CompanyName ?? "",
                    JobTitle = we.JobTitle ?? "",
                    StartDate = we.StartDate,
                    EndDate = we.EndDate,
                    IsCurrent = we.IsCurrent,
                    Location = we.Location,
                    Description = we.Description,
                    ExtractedOrder = we.ExtractedOrder
                }).ToList(),
                Education = candidate.Education.Select(e => new EducationDto
                {
                    Id = e.Id,
                    InstitutionName = e.InstitutionName,
                    DegreeName = e.DegreeName,
                    DegreeType = e.DegreeType,
                    FieldOfStudy = e.FieldOfStudy,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    GPA = e.GPA,
                    Location = e.Location
                }).ToList(),
                Skills = candidate.CandidateSkills.Select(cs => new CandidateSkillDto
                {
                    Id = cs.Id,
                    SkillName = cs.Skill?.SkillName ?? "",
                    Category = cs.Skill?.Category,
                    ProficiencyLevel = cs.ProficiencyLevel,
                    YearsOfExperience = cs.YearsOfExperience,
                    IsExtracted = cs.IsExtracted
                }).ToList()
            };

            return Ok(candidateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving candidate {CandidateId}", id);
            return StatusCode(500, new { message = "Error retrieving candidate", error = ex.Message });
        }
    }

    /// <summary>
    /// Get candidate status
    /// </summary>
    [HttpGet("{id}/status")]
    public async Task<ActionResult<CandidateStatusDto>> GetCandidateStatus(Guid id)
    {
        try
        {
            var status = await _statusService.GetCandidateStatusAsync(id);
            return Ok(status);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving candidate status for {CandidateId}", id);
            return StatusCode(500, new { message = "Error retrieving candidate status", error = ex.Message });
        }
    }

    /// <summary>
    /// Update candidate status using service layer
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<CandidateStatusDto>> UpdateCandidateStatus(Guid id, [FromBody] string status)
    {
        try
        {
            var updatedBy = GetClientId(); // Use client ID as updater
            var result = await _statusService.UpdateCandidateStatusAsync(id, status, updatedBy);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for candidate {CandidateId}", id);
            return StatusCode(500, new { message = "Error updating candidate status", error = ex.Message });
        }
    }

    /// <summary>
    /// Get candidate status history
    /// </summary>
    [HttpGet("{id}/status/history")]
    public async Task<ActionResult<List<CandidateStatusHistoryDto>>> GetCandidateStatusHistory(Guid id)
    {
        try
        {
            var history = await _statusService.GetStatusHistoryAsync(id);
            return Ok(history);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving status history for candidate {CandidateId}", id);
            return StatusCode(500, new { message = "Error retrieving status history", error = ex.Message });
        }
    }

    /// <summary>
    /// Get available search strategies for debugging and monitoring
    /// </summary>
    [HttpGet("search/strategies")]
    public ActionResult GetSearchStrategies()
    {
        try
        {
            var strategies = _candidateSearchService.GetAvailableStrategies();
            return Ok(new { strategies, total = strategies.Count() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving search strategies");
            return StatusCode(500, new { message = "Error retrieving search strategies", error = ex.Message });
        }
    }

    /// <summary>
    /// Get candidate status totals for dashboard
    /// </summary>
    [HttpGet("status/totals")]
    public async Task<ActionResult<Dictionary<string, int>>> GetStatusTotals()
    {
        try
        {
            var totals = await _statisticsService.GetStatusTotalsAsync();
            return Ok(totals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving status totals");
            return StatusCode(500, new { message = "Error retrieving status totals", error = ex.Message });
        }
    }

    /// <summary>
    /// Get skills frequency analytics
    /// </summary>
    [HttpGet("skills/frequency")]
    public async Task<ActionResult<List<SkillFrequencyDto>>> GetSkillsFrequency([FromQuery] int limit = 50)
    {
        try
        {
            var skillsFrequency = await _statisticsService.GetSkillsFrequencyAsync(limit);
            return Ok(skillsFrequency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving skills frequency");
            return StatusCode(500, new { message = "Error retrieving skills frequency", error = ex.Message });
        }
    }

    /// <summary>
    /// Get system statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<SystemStatisticsDto>> GetSystemStatistics()
    {
        try
        {
            var statistics = await _statisticsService.GetSystemStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system statistics");
            return StatusCode(500, new { message = "Error retrieving system statistics", error = ex.Message });
        }
    }
}