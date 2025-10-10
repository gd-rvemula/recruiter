using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RecruiterApi.Services
{
    /// <summary>
    /// OpenAI-based embedding service for semantic search
    /// Uses text-embedding-3-small model (1536 dimensions)
    /// </summary>
    public class OpenAIEmbeddingService : IEmbeddingService
    {
        private readonly ILogger<OpenAIEmbeddingService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;
        private readonly string _model;
        private const int EmbeddingDimension = 1536;

        public OpenAIEmbeddingService(
            ILogger<OpenAIEmbeddingService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI:ApiKey not configured");
            _model = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var embeddings = await GenerateEmbeddingsAsync(new List<string> { text });
            return embeddings.FirstOrDefault() ?? Array.Empty<float>();
        }

        public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var request = new
                {
                    model = _model,
                    input = texts
                };

                var response = await client.PostAsJsonAsync(
                    "https://api.openai.com/v1/embeddings", 
                    request
                );

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>();

                if (result?.Data == null || !result.Data.Any())
                {
                    _logger.LogWarning("No embeddings returned from OpenAI");
                    return new List<float[]>();
                }

                return result.Data
                    .OrderBy(d => d.Index)
                    .Select(d => d.Embedding)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embeddings from OpenAI");
                throw;
            }
        }

        public int GetEmbeddingDimension() => EmbeddingDimension;

        public string GetModelName() => _model;

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                // Try a simple embedding to test availability
                var testEmbedding = await GenerateEmbeddingAsync("test");
                return testEmbedding != null && testEmbedding.Length > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI service is not available");
                return false;
            }
        }

        // OpenAI API response models
        private class OpenAIEmbeddingResponse
        {
            public string Object { get; set; } = string.Empty;
            public List<EmbeddingData> Data { get; set; } = new();
            public string Model { get; set; } = string.Empty;
            public Usage Usage { get; set; } = new();
        }

        private class EmbeddingData
        {
            public string Object { get; set; } = string.Empty;
            public int Index { get; set; }
            public float[] Embedding { get; set; } = Array.Empty<float>();
        }

        private class Usage
        {
            public int PromptTokens { get; set; }
            public int TotalTokens { get; set; }
        }
    }
}