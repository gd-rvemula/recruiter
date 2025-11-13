using Microsoft.EntityFrameworkCore;
using Npgsql;
using RecruiterApi.Data;
using RecruiterApi.DTOs;

namespace RecruiterApi.Services.CandidateSearchStrategies;

/// <summary>
/// Implements name-based search using PostgreSQL Full-Text Search
/// Optimized for finding specific candidates by name
/// </summary>
public class NameMatchSearchStrategy : ICandidateSearchStrategy
{
    private readonly RecruiterDbContext _context;
    private readonly ILogger<NameMatchSearchStrategy> _logger;

    public string Name => "Name Match";
    public int Priority => 1;

    public NameMatchSearchStrategy(RecruiterDbContext context, ILogger<NameMatchSearchStrategy> logger)
    {
        _context = context;
        _logger = logger;
    }

    public bool CanHandle(string searchMode)
    {
        return searchMode?.ToLowerInvariant() == "namematch";
    }

    public async Task<CandidateSearchResponse> SearchAsync(CandidateSearchRequest request)
    {
        _logger.LogInformation("Using NAME MATCH search for query: '{SearchTerm}'", request.SearchTerm);

        // Use PostgreSQL Full-Text Search for name matching
        var connectionString = _context.Database.GetConnectionString();
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var searchTerm = request.SearchTerm?.Trim() ?? "";
        
        // Create search query for names
        var tsQuery = string.Join(" & ", searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(term => $"{term}:*"));

        var sql = @"
            SELECT c.id, c.candidate_code, c.first_name, c.last_name, c.full_name, 
                   c.email, c.phone, c.current_title, c.requisition_name, 
                   c.total_years_experience, c.salary_expectation, c.is_authorized_to_work, 
                   c.needs_sponsorship, c.is_active, c.current_status,
                   ts_rank_cd(c.search_vector, to_tsquery('english', @tsquery)) as relevance_score
            FROM candidates c
            WHERE c.search_vector @@ to_tsquery('english', @tsquery)
               AND c.is_active = true
            ORDER BY relevance_score DESC, c.last_name, c.first_name
            LIMIT @pagesize OFFSET @offset";

        var candidates = new List<CandidateSearchDto>();
        
        await using var command = new Npgsql.NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tsquery", tsQuery);
        command.Parameters.AddWithValue("@pagesize", request.PageSize);
        command.Parameters.AddWithValue("@offset", (request.Page - 1) * request.PageSize);
        
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var candidate = new CandidateSearchDto
            {
                Id = reader.GetGuid(0),  // id
                CandidateCode = reader.IsDBNull(1) ? null : reader.GetString(1),  // candidate_code
                FirstName = reader.IsDBNull(2) ? null : reader.GetString(2),  // first_name
                LastName = reader.IsDBNull(3) ? null : reader.GetString(3),  // last_name
                FullName = reader.IsDBNull(4) ? null : reader.GetString(4),  // full_name
                Email = reader.IsDBNull(5) ? null : reader.GetString(5),  // email
                Phone = reader.IsDBNull(6) ? null : reader.GetString(6),  // phone
                CurrentTitle = reader.IsDBNull(7) ? null : reader.GetString(7),  // current_title
                RequisitionName = reader.IsDBNull(8) ? null : reader.GetString(8),  // requisition_name
                TotalYearsExperience = reader.IsDBNull(9) ? null : reader.GetInt32(9),  // total_years_experience
                SalaryExpectation = reader.IsDBNull(10) ? null : reader.GetDecimal(10),  // salary_expectation
                IsAuthorizedToWork = reader.IsDBNull(11) ? false : reader.GetBoolean(11),  // is_authorized_to_work
                NeedsSponsorship = reader.IsDBNull(12) ? false : reader.GetBoolean(12),  // needs_sponsorship
                IsActive = reader.GetBoolean(13),  // is_active
                CurrentStatus = reader.IsDBNull(14) ? "New" : reader.GetString(14),  // current_status
                SimilarityScore = (double)reader.GetFloat(15),  // relevance_score
                EmbeddingModel = "PostgreSQL FTS",
                PrimarySkills = new List<string>() // Will be populated separately if needed
            };
            candidates.Add(candidate);
        }

        // Close the reader before executing count query
        await reader.CloseAsync();

        // Apply sponsorship filter
        candidates = ApplyFilters(candidates, request);

        // Get total count for pagination
        var countSql = @"
            SELECT COUNT(*)
            FROM candidates c
            WHERE c.search_vector @@ to_tsquery('english', @tsquery)
               AND c.is_active = true";
        
        await using var countCommand = new Npgsql.NpgsqlCommand(countSql, connection);
        countCommand.Parameters.AddWithValue("@tsquery", tsQuery);
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new CandidateSearchResponse
        {
            Candidates = candidates,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasNextPage = request.Page < totalPages,
            HasPreviousPage = request.Page > 1
        };
    }

    private List<CandidateSearchDto> ApplyFilters(List<CandidateSearchDto> candidates, CandidateSearchRequest request)
    {
        var filtered = candidates.AsEnumerable();

        // Sponsorship filter
        if (!string.IsNullOrEmpty(request.SponsorshipFilter))
        {
            if (request.SponsorshipFilter.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(c => c.NeedsSponsorship == true);
            }
            else if (request.SponsorshipFilter.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(c => c.NeedsSponsorship == false);
            }
        }

        return filtered.ToList();
    }
}