using Microsoft.AspNetCore.Mvc;
using RecruiterApi.DTOs;
using RecruiterApi.Services;

namespace RecruiterApi.Controllers
{
    /// <summary>
    /// API controller for managing multi-tenant client configurations
    /// 
    /// Multi-Tenancy Support:
    /// - Reads X-Client-ID header from requests (defaults to "GLOBAL")
    /// - No validation currently implemented
    /// - Future: Add authentication and tenant validation
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ClientConfigController : ControllerBase
    {
        private readonly IClientConfigService _configService;
        private readonly ILogger<ClientConfigController> _logger;
        private const string CLIENT_ID_HEADER = "X-Client-ID";
        private const string DEFAULT_CLIENT_ID = "GLOBAL";

        public ClientConfigController(
            IClientConfigService configService,
            ILogger<ClientConfigController> logger)
        {
            _configService = configService;
            _logger = logger;
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
        /// Get all configuration settings for the client
        /// 
        /// Headers:
        ///   X-Client-ID: Client/tenant identifier (optional, defaults to GLOBAL)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ClientConfigDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<ClientConfigDto>>> GetAllConfigs()
        {
            try
            {
                var clientId = GetClientId();
                var configs = await _configService.GetAllConfigsAsync(clientId);
                
                _logger.LogInformation("Retrieved {Count} configs for client: {ClientId}", 
                    configs.Count, clientId);
                
                return Ok(configs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all configs");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get specific configuration by key
        /// 
        /// Headers:
        ///   X-Client-ID: Client/tenant identifier (optional, defaults to GLOBAL)
        /// </summary>
        [HttpGet("{key}")]
        [ProducesResponseType(typeof(ClientConfigDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ClientConfigDto>> GetConfig(string key)
        {
            try
            {
                var clientId = GetClientId();
                var config = await _configService.GetConfigAsync(key, clientId);
                
                if (config == null)
                {
                    return NotFound($"Configuration '{key}' not found for client '{clientId}'");
                }

                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving config: {Key}", key);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update or create configuration
        /// 
        /// Headers:
        ///   X-Client-ID: Client/tenant identifier (optional, defaults to GLOBAL)
        /// </summary>
        [HttpPost]
        [HttpPatch]
        [ProducesResponseType(typeof(ClientConfigDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ClientConfigDto>> UpsertConfig([FromBody] UpdateConfigRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ConfigKey))
                {
                    return BadRequest("ConfigKey is required");
                }

                var clientId = GetClientId();
                var config = await _configService.UpsertConfigAsync(
                    request.ConfigKey, 
                    request.ConfigValue, 
                    clientId);
                
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting config: {Key}", request.ConfigKey);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get search scoring configuration
        /// 
        /// Headers:
        ///   X-Client-ID: Client/tenant identifier (optional, defaults to GLOBAL)
        /// </summary>
        [HttpGet("search/scoring")]
        [ProducesResponseType(typeof(SearchScoringConfigDto), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SearchScoringConfigDto>> GetSearchScoringConfig()
        {
            try
            {
                var clientId = GetClientId();
                var config = await _configService.GetSearchScoringConfigAsync(clientId);
                
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving search scoring config");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get available scoring strategies
        /// 
        /// This endpoint returns strategy descriptions and does not require client ID
        /// </summary>
        [HttpGet("search/strategies")]
        [ProducesResponseType(typeof(Dictionary<string, string>), 200)]
        public ActionResult<Dictionary<string, string>> GetScoringStrategies()
        {
            var strategies = new Dictionary<string, string>
            {
                { 
                    "option1", 
                    "All-or-Nothing: 100% if all keywords match, otherwise semantic score only. " +
                    "Best for strict requirements where all skills are mandatory." 
                },
                { 
                    "option4", 
                    "Tiered Multi-Keyword: Balanced scoring based on keyword coverage and quality. " +
                    "Rewards partial matches. Best for flexible requirements." 
                }
            };

            return Ok(strategies);
        }

        /// <summary>
        /// Get privacy configuration settings
        /// </summary>
        [HttpGet("privacy")]
        [ProducesResponseType(typeof(PrivacyConfigDto), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<PrivacyConfigDto>> GetPrivacyConfig()
        {
            try
            {
                var clientId = GetClientId();
                var config = await _configService.GetPrivacyConfigAsync(clientId);
                
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving privacy config");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update privacy configuration settings
        /// </summary>
        [HttpPost("privacy")]
        [ProducesResponseType(typeof(PrivacyConfigDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<PrivacyConfigDto>> UpdatePrivacyConfig([FromBody] PrivacyConfigDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Privacy configuration is required");
                }

                var clientId = GetClientId();
                var config = await _configService.UpdatePrivacyConfigAsync(clientId, request);
                
                _logger.LogInformation("Updated privacy configuration for client {ClientId}", clientId);
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating privacy config");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
