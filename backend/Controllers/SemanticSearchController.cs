using Microsoft.AspNetCore.Mvc;
using RecruiterApi.DTOs;
using RecruiterApi.Services;

namespace RecruiterApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SemanticSearchController : ControllerBase
    {
        private readonly ILogger<SemanticSearchController> _logger;
        private readonly SemanticSearchService _semanticSearchService;
        private readonly IEmbeddingService _embeddingService;

        public SemanticSearchController(
            ILogger<SemanticSearchController> logger,
            SemanticSearchService semanticSearchService,
            IEmbeddingService embeddingService)
        {
            _logger = logger;
            _semanticSearchService = semanticSearchService;
            _embeddingService = embeddingService;
        }

        /// <summary>
        /// Semantic search using AI embeddings for natural language queries
        /// </summary>
        /// <param name="request">Search parameters including query text</param>
        /// <returns>List of candidates ranked by semantic similarity</returns>
        [HttpPost("search")]
        [ProducesResponseType(typeof(CandidateSearchResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<CandidateSearchResponse>> SemanticSearch(
            [FromBody] SemanticSearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest(new { message = "Query cannot be empty" });
                }

                _logger.LogInformation(
                    "Semantic search request: '{Query}', page: {Page}, pageSize: {PageSize}, threshold: {Threshold}",
                    request.Query, request.Page, request.PageSize, request.SimilarityThreshold
                );

                var results = await _semanticSearchService.SemanticSearchCandidatesAsync(
                    request.Query,
                    request.Page,
                    request.PageSize,
                    request.SimilarityThreshold
                );

                var totalPages = (int)Math.Ceiling(results.Count / (double)request.PageSize);
                
                return Ok(new CandidateSearchResponse
                {
                    Candidates = results,
                    TotalCount = results.Count,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = request.Page < totalPages,
                    HasPreviousPage = request.Page > 1
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing semantic search for query: '{Query}'", request.Query);
                return StatusCode(500, new { message = "Error performing semantic search", error = ex.Message });
            }
        }

        /// <summary>
        /// Hybrid search combining semantic similarity and keyword matching
        /// </summary>
        /// <param name="request">Search parameters with weights for semantic vs keyword</param>
        /// <returns>List of candidates ranked by combined score</returns>
        [HttpPost("hybrid")]
        [ProducesResponseType(typeof(CandidateSearchResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<CandidateSearchResponse>> HybridSearch(
            [FromBody] HybridSearchRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest(new { message = "Query cannot be empty" });
                }

                _logger.LogInformation(
                    "Hybrid search request: '{Query}', semantic weight: {SemanticWeight}, keyword weight: {KeywordWeight}",
                    request.Query, request.SemanticWeight, request.KeywordWeight
                );

                var results = await _semanticSearchService.HybridSearchAsync(
                    request.Query,
                    request.Page,
                    request.PageSize,
                    request.SemanticWeight,
                    request.KeywordWeight
                );

                var totalPages = (int)Math.Ceiling(results.Count / (double)request.PageSize);
                
                return Ok(new CandidateSearchResponse
                {
                    Candidates = results,
                    TotalCount = results.Count,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = request.Page < totalPages,
                    HasPreviousPage = request.Page > 1
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing hybrid search for query: '{Query}'", request.Query);
                return StatusCode(500, new { message = "Error performing hybrid search", error = ex.Message });
            }
        }

        /// <summary>
        /// Check if embedding service is available and healthy
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<ActionResult> CheckHealth()
        {
            try
            {
                var isAvailable = await _embeddingService.IsAvailableAsync();
                var modelName = _embeddingService.GetModelName();
                var dimension = _embeddingService.GetEmbeddingDimension();

                return Ok(new
                {
                    available = isAvailable,
                    model = modelName,
                    dimension = dimension,
                    status = isAvailable ? "healthy" : "unavailable"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking embedding service health");
                return Ok(new
                {
                    available = false,
                    status = "error",
                    error = ex.Message
                });
            }
        }
    }

    // DTOs
    public class SemanticSearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public double SimilarityThreshold { get; set; } = 0.7;
    }

    public class HybridSearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public double SemanticWeight { get; set; } = 0.7;
        public double KeywordWeight { get; set; } = 0.3;
    }
}
