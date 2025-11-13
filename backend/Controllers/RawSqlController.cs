using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruiterApi.Data;

namespace RecruiterApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RawSqlController : ControllerBase
{
    private readonly RecruiterDbContext _context;

    public RawSqlController(RecruiterDbContext context)
    {
        _context = context;
    }

    [HttpGet("rawcount")]
    public async Task<ActionResult> GetRawCount()
    {
        try
        {
            var result = await _context.Database.SqlQueryRaw<int>("SELECT COUNT(*) as Value FROM candidates").FirstAsync();
            return Ok(new { count = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error", error = ex.Message });
        }
    }

    [HttpGet("rawdata")]
    public async Task<ActionResult> GetRawData()
    {
        try
        {
            var sql = "SELECT id, candidate_code, first_name, last_name, email FROM candidates LIMIT 5";
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            
            var results = new List<object>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                results.Add(new
                {
                    id = reader["id"],
                    candidate_code = reader["candidate_code"],
                    first_name = reader["first_name"],
                    last_name = reader["last_name"],
                    email = reader["email"]
                });
            }
            
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error", error = ex.Message });
        }
        finally
        {
            await _context.Database.GetDbConnection().CloseAsync();
        }
    }

    [HttpPost("execute")]
    public async Task<ActionResult> ExecuteRawSql([FromBody] RawSqlRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { message = "Query cannot be empty" });
            }

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = request.Query;
            
            var results = new List<Dictionary<string, object>>();
            using var reader = await command.ExecuteReaderAsync();
            
            // Get column names
            var columnNames = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columnNames.Add(reader.GetName(i));
            }
            
            // Read data
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[columnNames[i]] = value ?? DBNull.Value;
                }
                results.Add(row);
            }
            
            return Ok(new 
            { 
                columns = columnNames,
                rows = results,
                count = results.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error executing query", error = ex.Message });
        }
        finally
        {
            await _context.Database.GetDbConnection().CloseAsync();
        }
    }
}

public class RawSqlRequest
{
    public string Query { get; set; } = string.Empty;
}
