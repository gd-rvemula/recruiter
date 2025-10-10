using Microsoft.AspNetCore.Mvc;
using RecruiterApi.DTOs;
using RecruiterApi.Services;

namespace RecruiterApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExcelImportController : ControllerBase
{
    private readonly IExcelImportService _excelImportService;
    private readonly ILogger<ExcelImportController> _logger;

    public ExcelImportController(IExcelImportService excelImportService, ILogger<ExcelImportController> logger)
    {
        _excelImportService = excelImportService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<ExcelImportResultDto>> UploadExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _excelImportService.ProcessExcelFileAsync(stream, file.FileName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Excel file");
            return StatusCode(500, new { message = "Error processing Excel file", error = ex.Message });
        }
    }
}
