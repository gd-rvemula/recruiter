using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruiterApi.Data;

namespace RecruiterApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly RecruiterDbContext _context;

    public TestController(RecruiterDbContext context)
    {
        _context = context;
    }

    [HttpGet("count")]
    public async Task<ActionResult> GetCandidateCount()
    {
        try
        {
            var count = await _context.Candidates.CountAsync();
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error", error = ex.Message });
        }
    }

    [HttpGet("simple")]
    public async Task<ActionResult> GetSimple()
    {
        try
        {
            var candidates = await _context.Candidates
                .Select(c => new { c.Id, c.FirstName, c.LastName })
                .Take(5)
                .ToListAsync();
            return Ok(candidates);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error", error = ex.Message });
        }
    }
}
