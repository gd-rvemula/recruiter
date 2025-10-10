using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RecruiterApi.Data;
using RecruiterApi.DTOs;
using System.Data;

namespace RecruiterApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly RecruiterDbContext _context;
    private readonly ILogger<SearchController> _logger;
    private readonly IConfiguration _configuration;

    public SearchController(
        RecruiterDbContext context, 
        ILogger<SearchController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("fts")]
    public async Task<ActionResult<CandidateSearchResponse>> FullTextSearch([FromBody] SimpleSearchRequest request)
    {
        try
        {
            var candidates = new List<CandidateSearchDto>();
            var totalCount = 0;

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            // Get total count first
            using (var countCommand = new NpgsqlCommand(@"
                SELECT COUNT(*) 
                FROM candidate_search_view 
                WHERE combined_search_vector @@ plainto_tsquery('english', @query)", connection))
            {
                countCommand.Parameters.AddWithValue("@query", request.Query ?? "");
                totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
            }

            // Get the results
            using (var command = new NpgsqlCommand(@"
                SELECT 
                    csv.candidate_id,
                    c.candidate_code,
                    csv.first_name,
                    csv.last_name,
                    c.full_name,
                    csv.email,
                    csv.current_title,
                    csv.years_of_experience,
                    csv.skills_text,
                    ts_rank(csv.combined_search_vector, plainto_tsquery('english', @query)) as search_rank
                FROM candidate_search_view csv
                JOIN candidates c ON csv.candidate_id = c.id
                WHERE csv.combined_search_vector @@ plainto_tsquery('english', @query)
                  AND c.is_active = true
                ORDER BY search_rank DESC, csv.last_name, csv.first_name
                LIMIT @pageSize OFFSET @offset", connection))
            {
                command.Parameters.AddWithValue("@query", request.Query ?? "");
                command.Parameters.AddWithValue("@pageSize", request.PageSize);
                command.Parameters.AddWithValue("@offset", (request.Page - 1) * request.PageSize);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    candidates.Add(new CandidateSearchDto
                    {
                        Id = reader.GetGuid("candidate_id"),
                        CandidateCode = reader.GetString("candidate_code"),
                        FirstName = reader.GetString("first_name"),
                        LastName = reader.GetString("last_name"),
                        FullName = reader.GetString("full_name"),
                        Email = reader.GetString("email"),
                        CurrentTitle = reader.IsDBNull("current_title") ? null : reader.GetString("current_title"),
                        TotalYearsExperience = reader.IsDBNull("years_of_experience") ? 0 : reader.GetInt32("years_of_experience"),
                        PrimarySkills = reader.IsDBNull("skills_text") ? new List<string>() : 
                            reader.GetString("skills_text").Split(',').Select(s => s.Trim()).ToList(),
                        // Default values for required fields
                        SalaryExpectation = 0,
                        IsAuthorizedToWork = true,
                        NeedsSponsorship = false,
                        IsActive = true
                    });
                }
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            return Ok(new CandidateSearchResponse
            {
                Candidates = candidates,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing full-text search with query: {Query}", request.Query);
            return StatusCode(500, new { message = "Error performing search", error = ex.Message });
        }
    }
}

public class SimpleSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}