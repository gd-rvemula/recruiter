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
    /// Ollama-based embedding service for semantic search (local, free)
    /// Uses nomic-embed-text model (768 dimensions)
    /// </summary>
    public class OllamaEmbeddingService : IEmbeddingService
    {
        private readonly ILogger<OllamaEmbeddingService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _ollamaUrl;
        private readonly string _model;
        private const int EmbeddingDimension = 768;

        public OllamaEmbeddingService(
            ILogger<OllamaEmbeddingService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _ollamaUrl = configuration["Ollama:Url"] ?? "http://localhost:11434";
            _model = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                var request = new
                {
                    model = _model,
                    prompt = text
                };

                var response = await client.PostAsJsonAsync(
                    $"{_ollamaUrl}/api/embeddings", 
                    request
                );

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();

                if (result?.Embedding == null || !result.Embedding.Any())
                {
                    _logger.LogWarning("No embedding returned from Ollama");
                    return Array.Empty<float>();
                }

                return result.Embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding from Ollama for text: {Text}", 
                    text.Length > 100 ? text.Substring(0, 100) + "..." : text);
                throw;
            }
        }

        public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
        {
            // Ollama API processes one at a time, so batch them
            var embeddings = new List<float[]>();
            
            foreach (var text in texts)
            {
                var embedding = await GenerateEmbeddingAsync(text);
                embeddings.Add(embedding);
            }

            return embeddings;
        }

        public int GetEmbeddingDimension() => EmbeddingDimension;

        public string GetModelName() => _model;

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                
                var response = await client.GetAsync($"{_ollamaUrl}/api/tags");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ollama service is not available at {Url}", _ollamaUrl);
                return false;
            }
        }

        // Ollama API response model
        private class OllamaEmbeddingResponse
        {
            public float[] Embedding { get; set; } = Array.Empty<float>();
        }
    }
}