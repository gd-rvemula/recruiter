using Microsoft.EntityFrameworkCore;
using RecruiterApi.Data;
using RecruiterApi.Models;
using RecruiterApi.DTOs;

namespace RecruiterApi.Services
{
    public interface IClientConfigService
    {
        Task<ClientConfigDto?> GetConfigAsync(string key, string clientId = "GLOBAL");
        Task<List<ClientConfigDto>> GetAllConfigsAsync(string clientId = "GLOBAL");
        Task<ClientConfigDto> UpsertConfigAsync(string key, string value, string clientId = "GLOBAL");
        Task<SearchScoringConfigDto> GetSearchScoringConfigAsync(string clientId = "GLOBAL");
        Task<SearchScoringConfigDto> UpdateSearchScoringConfigAsync(string clientId, SearchScoringConfigDto config);
        Task<PrivacyConfigDto> GetPrivacyConfigAsync(string clientId = "GLOBAL");
        Task<PrivacyConfigDto> UpdatePrivacyConfigAsync(string clientId, PrivacyConfigDto config);
    }

    /// <summary>
    /// Service for managing multi-tenant client configurations
    /// </summary>
    public class ClientConfigService : IClientConfigService
    {
        private readonly RecruiterDbContext _context;
        private readonly ILogger<ClientConfigService> _logger;

        public ClientConfigService(RecruiterDbContext context, ILogger<ClientConfigService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get specific configuration by key for a client
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="clientId">Client/tenant identifier (defaults to GLOBAL)</param>
        /// <returns>Configuration DTO or null if not found</returns>
        public async Task<ClientConfigDto?> GetConfigAsync(string key, string clientId = "GLOBAL")
        {
            var config = await _context.ClientConfigs
                .FirstOrDefaultAsync(c => c.ClientId == clientId && c.ConfigKey == key);

            if (config == null)
            {
                _logger.LogWarning("Configuration not found: {Key} for client: {ClientId}", key, clientId);
                return null;
            }

            return MapToDto(config);
        }

        /// <summary>
        /// Get all configurations for a client
        /// </summary>
        /// <param name="clientId">Client/tenant identifier (defaults to GLOBAL)</param>
        /// <returns>List of configuration DTOs</returns>
        public async Task<List<ClientConfigDto>> GetAllConfigsAsync(string clientId = "GLOBAL")
        {
            var configs = await _context.ClientConfigs
                .Where(c => c.ClientId == clientId)
                .OrderBy(c => c.ConfigKey)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} configurations for client: {ClientId}", 
                configs.Count, clientId);

            return configs.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Update or create a configuration for a client
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">Configuration value</param>
        /// <param name="clientId">Client/tenant identifier (defaults to GLOBAL)</param>
        /// <returns>Updated or created configuration DTO</returns>
        public async Task<ClientConfigDto> UpsertConfigAsync(string key, string value, string clientId = "GLOBAL")
        {
            var existing = await _context.ClientConfigs
                .FirstOrDefaultAsync(c => c.ClientId == clientId && c.ConfigKey == key);

            if (existing != null)
            {
                // Update existing configuration
                existing.ConfigValue = value;
                existing.UpdatedAt = DateTime.UtcNow;
                
                _logger.LogInformation("Updated config {Key} for client {ClientId}: {Value}", 
                    key, clientId, value);
            }
            else
            {
                // Create new configuration
                existing = new ClientConfig
                {
                    ClientId = clientId,
                    ConfigKey = key,
                    ConfigValue = value,
                    ConfigType = "string"
                };
                _context.ClientConfigs.Add(existing);
                
                _logger.LogInformation("Created config {Key} for client {ClientId}: {Value}", 
                    key, clientId, value);
            }

            await _context.SaveChangesAsync();

            return MapToDto(existing);
        }

        /// <summary>
        /// Get search scoring configuration for a client
        /// </summary>
        /// <param name="clientId">Client/tenant identifier (defaults to GLOBAL)</param>
        /// <returns>Search scoring configuration DTO</returns>
        public async Task<SearchScoringConfigDto> GetSearchScoringConfigAsync(string clientId = "GLOBAL")
        {
            var configs = await _context.ClientConfigs
                .Where(c => c.ClientId == clientId && c.ConfigKey.StartsWith("search."))
                .ToDictionaryAsync(c => c.ConfigKey, c => c.ConfigValue);

            _logger.LogInformation("Retrieved search scoring config for client: {ClientId}", clientId);

            return new SearchScoringConfigDto
            {
                ClientId = clientId,
                ScoringStrategy = configs.GetValueOrDefault("search.scoring_strategy", "option1"),
                SemanticWeight = double.Parse(configs.GetValueOrDefault("search.semantic_weight", "0.6")),
                KeywordWeight = double.Parse(configs.GetValueOrDefault("search.keyword_weight", "0.4")),
                SimilarityThreshold = double.Parse(configs.GetValueOrDefault("search.similarity_threshold", "0.3"))
            };
        }

        /// <summary>
        /// Map entity to DTO
        /// </summary>
        private static ClientConfigDto MapToDto(ClientConfig config)
        {
            return new ClientConfigDto
            {
                Id = config.Id,
                ClientId = config.ClientId,
                ConfigKey = config.ConfigKey,
                ConfigValue = config.ConfigValue,
                ConfigType = config.ConfigType,
                Description = config.Description,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };
        }

        public async Task<SearchScoringConfigDto> UpdateSearchScoringConfigAsync(string clientId, SearchScoringConfigDto config)
        {
            await UpsertConfigAsync("SCORING_STRATEGY", config.ScoringStrategy, clientId);
            await UpsertConfigAsync("SEMANTIC_WEIGHT", config.SemanticWeight.ToString(), clientId);
            await UpsertConfigAsync("KEYWORD_WEIGHT", config.KeywordWeight.ToString(), clientId);
            await UpsertConfigAsync("SIMILARITY_THRESHOLD", config.SimilarityThreshold.ToString(), clientId);

            return await GetSearchScoringConfigAsync(clientId);
        }

        public async Task<PrivacyConfigDto> GetPrivacyConfigAsync(string clientId = "GLOBAL")
        {
            var configs = await GetAllConfigsAsync(clientId);
            
            var piiEnabledConfig = configs.FirstOrDefault(c => c.ConfigKey == "PII_SANITIZATION_ENABLED");
            var piiLevelConfig = configs.FirstOrDefault(c => c.ConfigKey == "PII_SANITIZATION_LEVEL");
            var logRemovalsConfig = configs.FirstOrDefault(c => c.ConfigKey == "PII_LOG_REMOVALS");

            return new PrivacyConfigDto
            {
                ClientId = clientId,
                PiiSanitizationEnabled = piiEnabledConfig?.ConfigValue?.ToLowerInvariant() == "true",
                PiiSanitizationLevel = piiLevelConfig?.ConfigValue ?? "full",
                LogPiiRemovals = logRemovalsConfig?.ConfigValue?.ToLowerInvariant() != "false"
            };
        }

        public async Task<PrivacyConfigDto> UpdatePrivacyConfigAsync(string clientId, PrivacyConfigDto config)
        {
            await UpsertConfigAsync("PII_SANITIZATION_ENABLED", config.PiiSanitizationEnabled.ToString().ToLowerInvariant(), clientId);
            await UpsertConfigAsync("PII_SANITIZATION_LEVEL", config.PiiSanitizationLevel, clientId);
            await UpsertConfigAsync("PII_LOG_REMOVALS", config.LogPiiRemovals.ToString().ToLowerInvariant(), clientId);

            return await GetPrivacyConfigAsync(clientId);
        }
    }
}
