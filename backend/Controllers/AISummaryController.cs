using Microsoft.AspNetCore.Mvc;
using RecruiterApi.Services;
using System.Threading.Tasks;

namespace RecruiterApi.Controllers
{
    [ApiController]
    [Route("api/ai-summary")]
    public class AISummaryController : ControllerBase
    {
        private readonly IAISummaryService _aiSummaryService;
        public AISummaryController(IAISummaryService aiSummaryService)
        {
            _aiSummaryService = aiSummaryService;
        }

        [HttpPost]
        public async Task<IActionResult> SummarizeResume([FromBody] ResumeSummaryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ResumeText))
                return BadRequest("Resume text is required.");

            if (request.CandidateId == Guid.Empty)
                return BadRequest("Candidate ID is required.");

            if (request.ResumeId == Guid.Empty)
                return BadRequest("Resume ID is required.");

            var summary = await _aiSummaryService.GenerateResumeSummaryAsync(
                request.ResumeText, 
                request.CandidateId, 
                request.ResumeId
            );
            return Ok(new { summary });
        }
    }

    public class ResumeSummaryRequest
    {
        public required string ResumeText { get; set; }
        public required Guid CandidateId { get; set; }
        public required Guid ResumeId { get; set; }
    }
}
