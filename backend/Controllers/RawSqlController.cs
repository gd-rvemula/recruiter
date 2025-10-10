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
}
