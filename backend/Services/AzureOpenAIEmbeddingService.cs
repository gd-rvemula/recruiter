using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RecruiterApi.Services
{
    /// <summary>
    /// Azure OpenAI-based embedding service for semantic search
    /// Alternative to Ollama for production use
    /// Uses text-embedding-3-small model (1536 dimensions)
    /// </summary>
    public class AzureOpenAIEmbeddingService : IEmbeddingService
    {
        private readonly ILogger<AzureOpenAIEmbeddingService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _endpoint;
        private readonly string _deployment;
        private readonly string _apiKey;
        private readonly int _embeddingDimension;
        private const string ApiVersion = "2023-05-15";

        public AzureOpenAIEmbeddingService(
            ILogger<AzureOpenAIEmbeddingService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            
            _endpoint = configuration["Embedding:AzureOpenAI:Endpoint"] 
                ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                ?? throw new ArgumentNullException("Azure OpenAI Endpoint not configured");
                
            _deployment = configuration["Embedding:AzureOpenAI:Deployment"] 
                ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT")
                ?? throw new ArgumentNullException("Azure OpenAI Embedding Deployment not configured");
                
            _apiKey = configuration["Embedding:AzureOpenAI:ApiKey"] 
                ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
                ?? throw new ArgumentNullException("Azure OpenAI API Key not configured");

            _embeddingDimension = int.Parse(
                configuration["Embedding:AzureOpenAI:Dimension"] 
                ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_DIMENSION") 
                ?? "1536"
            );

            _logger.LogInformation(
                "Azure OpenAI Embedding Service initialized. Deployment: {Deployment}, Dimensions: {Dimensions}",
                _deployment, _embeddingDimension
            );
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var embeddings = await GenerateEmbeddingsAsync(new List<string> { text });
            return embeddings.FirstOrDefault() ?? new float[_embeddingDimension];
        }

        public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
        {
            if (texts == null || !texts.Any())
            {
                _logger.LogWarning("No texts provided for embedding generation");
                return new List<float[]>();
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("api-key", _apiKey);
                client.Timeout = TimeSpan.FromMinutes(2);

                var url = $"{_endpoint}/openai/deployments/{_deployment}/embeddings?api-version={ApiVersion}";
                
                var request = new
                {
                    input = texts
                };

                _logger.LogInformation(
                    "Generating embeddings for {Count} texts using deployment: {Deployment}", 
                    texts.Count, 
                    _deployment
                );

                var response = await client.PostAsJsonAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "Azure OpenAI API error: {StatusCode} - {Error}", 
                        response.StatusCode, 
                        error
                    );
                    throw new HttpRequestException($"Azure OpenAI API error: {response.StatusCode}");
                }

                var result = await response.Content.ReadFromJsonAsync<AzureOpenAIEmbeddingResponse>();

                if (result?.Data == null || !result.Data.Any())
                {
                    _logger.LogWarning("No embeddings returned from Azure OpenAI");
                    return new List<float[]>();
                }

                _logger.LogInformation(
                    "Successfully generated {Count} embeddings. Total tokens: {Tokens}", 
                    result.Data.Count,
                    result.Usage?.TotalTokens ?? 0
                );

                return result.Data
                    .OrderBy(d => d.Index)
                    .Select(d => d.Embedding)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embeddings from Azure OpenAI");
                throw;
            }
        }

        public int GetEmbeddingDimension() => _embeddingDimension;

        public string GetModelName() => $"azure/{_deployment}";

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("api-key", _apiKey);
                client.Timeout = TimeSpan.FromSeconds(5);
                
                var response = await client.GetAsync(_endpoint);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Azure OpenAI service is not available");
                return false;
            }
        }

        // Azure OpenAI API response models
        private class AzureOpenAIEmbeddingResponse
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
