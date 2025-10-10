# Phase 1: Semantic Search Implementation
## Complete Step-by-Step Implementation Guide

**Date**: October 4, 2025  
**Status**: Ready to Implement  
**Framework**: Foundatio + Azure OpenAI + pgvector

---

## ⚠️ PREREQUISITE CHECK

### Azure OpenAI Configuration Status

**✅ Connection Verified**: Azure OpenAI endpoint is accessible  
**✅ API Key Valid**: Authentication successful  
**⚠️ Embedding Model Required**: Current deployment "scm_test" uses gpt-4.1-mini (chat model)

### Available Embedding Models in Your Azure OpenAI:
- `text-embedding-3-small` (1536 dimensions) - **RECOMMENDED**
- `text-embedding-3-large` (3072 dimensions) - Higher accuracy, more cost
- `text-embedding-ada-002` (1536 dimensions) - Legacy, still supported

### 🔴 ACTION REQUIRED BEFORE STARTING:

**You need to provide the deployment name for your embedding model.**

Please create a deployment in Azure OpenAI Portal:
1. Go to Azure OpenAI Studio → Deployments
2. Create new deployment with model: `text-embedding-3-small`
3. Give it a name (e.g., "text-embedding-small")
4. Update `.env` file with the deployment name

**Or** tell me the existing deployment name if you already have one for embeddings.

---

## Phase 1 Overview

### Goals
1. ✅ Install pgvector extension
2. ✅ Add embedding columns to database  
3. ✅ Implement Azure OpenAI embedding service
4. ✅ Generate embeddings for 651 existing candidates (using Foundatio queue)
5. ✅ Add semantic search endpoint
6. ✅ Create comprehensive unit tests (backend + frontend)

### Timeline Estimate
- **Database Setup**: 30 minutes
- **Backend Service Implementation**: 2 hours
- **Foundatio Queue Integration**: 1.5 hours
- **API Endpoints**: 1 hour
- **Unit Tests (Backend)**: 2 hours
- **Frontend Integration**: 1.5 hours
- **Frontend Unit Tests**: 1 hour
- **Integration Testing**: 1 hour
- **TOTAL**: ~10.5 hours

### Architecture Pattern (from agents.md)
```
User Query → API Controller → Semantic Search Service → 
  ↓
  ├─→ Azure OpenAI Embedding Service (generate query vector)
  ↓
  └─→ PostgreSQL + pgvector (vector similarity search)
      ↓
      Results (ranked by similarity)
```

### Foundatio Usage (as per agents.md guidelines)
- **Background Job Queue**: For async embedding generation
- **Caching**: For frequent search queries
- **Message Bus**: For embedding generation events

---

## Step 1: Database Setup (30 minutes)

### 1.1 Install pgvector Extension

**File**: `/backend/Migrations/01_InstallPgVector.sql`

```sql
-- Phase 1: Install pgvector Extension for Semantic Search
-- Date: October 4, 2025
-- Description: Enable vector similarity search for AI embeddings

-- Install the pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Verify installation
SELECT extname, extversion FROM pg_extension WHERE extname = 'vector';

-- Show vector capabilities
SELECT 'pgvector installed successfully! Ready for semantic search.' as status;
```

**Execution**:
```bash
# Copy to container
docker cp /Users/rvemula/projects/Recruiter/backend/Migrations/01_InstallPgVector.sql \
  $(docker ps -q --filter "ancestor=postgres:15"):/tmp/01_InstallPgVector.sql

# Execute
docker exec -it $(docker ps -q --filter "ancestor=postgres:15") \
  bash -c "PAGER=cat psql -U postgres -d recruitingdb -f /tmp/01_InstallPgVector.sql"
```

**Expected Output**:
```
CREATE EXTENSION
 extname | extversion 
---------+------------
 vector  | 0.5.1
```

### 1.2 Add Embedding Columns

**File**: `/backend/Migrations/02_AddEmbeddingColumns.sql`

```sql
-- Phase 1: Add Embedding Columns for Semantic Search
-- Date: October 4, 2025
-- Description: Add vector columns to store AI embeddings
-- Using 1536 dimensions for text-embedding-3-small

-- Add embedding column to candidates table (profile summary)
ALTER TABLE candidates 
ADD COLUMN IF NOT EXISTS profile_embedding vector(1536),
ADD COLUMN IF NOT EXISTS embedding_generated_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS embedding_model VARCHAR(100),
ADD COLUMN IF NOT EXISTS embedding_tokens INTEGER;

-- Add embedding column to resumes table (full resume text)
ALTER TABLE resumes 
ADD COLUMN IF NOT EXISTS resume_embedding vector(1536),
ADD COLUMN IF NOT EXISTS embedding_generated_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS embedding_model VARCHAR(100),
ADD COLUMN IF NOT EXISTS embedding_tokens INTEGER;

-- Create HNSW indexes for fast vector similarity search
-- Using cosine distance (most common for embeddings)
CREATE INDEX IF NOT EXISTS idx_candidates_profile_embedding 
ON candidates USING hnsw (profile_embedding vector_cosine_ops)
WITH (m = 16, ef_construction = 64);

CREATE INDEX IF NOT EXISTS idx_resumes_resume_embedding 
ON resumes USING hnsw (resume_embedding vector_cosine_ops)
WITH (m = 16, ef_construction = 64);

-- Create covering indexes for embedding queries
CREATE INDEX IF NOT EXISTS idx_candidates_embedding_status 
ON candidates (is_active, embedding_generated_at) 
WHERE profile_embedding IS NOT NULL;

-- Add comments for documentation
COMMENT ON COLUMN candidates.profile_embedding IS 'Vector embedding of candidate profile (1536 dims from text-embedding-3-small)';
COMMENT ON COLUMN candidates.embedding_generated_at IS 'Timestamp when embedding was last generated';
COMMENT ON COLUMN candidates.embedding_model IS 'Model used for embedding generation';

-- Show updated schema
SELECT 'Embedding columns and indexes created successfully!' as status;
\d candidates;
\d resumes;
```

**Execution**: Same pattern as Step 1.1

**Expected Output**: Shows updated table schema with vector columns

---

## Step 2: Backend Service Implementation (2 hours)

### 2.1 Update Azure OpenAI Embedding Service

**File**: `/backend/Services/AzureOpenAIEmbeddingService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RecruiterApi.Services
{
    /// <summary>
    /// Azure OpenAI-based embedding service for semantic search
    /// Uses text-embedding-3-small model (1536 dimensions)
    /// </summary>
    public class AzureOpenAIEmbeddingService : IEmbeddingService
    {
        private readonly ILogger<AzureOpenAIEmbeddingService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _endpoint;
        private readonly string _deployment;
        private readonly string _apiKey;
        private const int EmbeddingDimension = 1536;
        private const string ApiVersion = "2023-05-15";

        public AzureOpenAIEmbeddingService(
            ILogger<AzureOpenAIEmbeddingService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            
            // Load from .env or appsettings
            _endpoint = configuration["AzureOpenAI:Endpoint"] 
                ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                ?? throw new ArgumentNullException("Azure OpenAI Endpoint not configured");
                
            _deployment = configuration["AzureOpenAI:EmbeddingDeployment"] 
                ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT")
                ?? throw new ArgumentNullException("Azure OpenAI Embedding Deployment not configured");
                
            _apiKey = configuration["AzureOpenAI:ApiKey"] 
                ?? Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
                ?? throw new ArgumentNullException("Azure OpenAI API Key not configured");
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var embeddings = await GenerateEmbeddingsAsync(new List<string> { text });
            return embeddings.FirstOrDefault() ?? Array.Empty<float>();
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

        public int GetEmbeddingDimension() => EmbeddingDimension;

        public string GetModelName() => $"azure/{_deployment}";

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
```

**Unit Test**: `/backend/Tests/Services/AzureOpenAIEmbeddingServiceTests.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using RecruiterApi.Services;

namespace RecruiterApi.Tests.Services
{
    public class AzureOpenAIEmbeddingServiceTests
    {
        private readonly Mock<ILogger<AzureOpenAIEmbeddingService>> _loggerMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IConfiguration> _configurationMock;

        public AzureOpenAIEmbeddingServiceTests()
        {
            _loggerMock = new Mock<ILogger<AzureOpenAIEmbeddingService>>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _configurationMock = new Mock<IConfiguration>();

            // Setup configuration
            _configurationMock.Setup(c => c["AzureOpenAI:Endpoint"])
                .Returns("https://test.openai.azure.com");
            _configurationMock.Setup(c => c["AzureOpenAI:EmbeddingDeployment"])
                .Returns("text-embedding-small");
            _configurationMock.Setup(c => c["AzureOpenAI:ApiKey"])
                .Returns("test-key");
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_SuccessfulResponse_ReturnsEmbedding()
        {
            // Arrange
            var mockResponse = new
            {
                data = new[]
                {
                    new
                    {
                        index = 0,
                        embedding = Enumerable.Range(0, 1536).Select(i => (float)i / 1536).ToArray()
                    }
                },
                usage = new { prompt_tokens = 10, total_tokens = 10 }
            };

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(mockResponse))
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            var service = new AzureOpenAIEmbeddingService(
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _configurationMock.Object
            );

            // Act
            var result = await service.GenerateEmbeddingAsync("Test text");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1536, result.Length);
            Assert.Equal(0f, result[0]);
        }

        [Fact]
        public async Task GenerateEmbeddingsAsync_MultipleTexts_ReturnsBatch()
        {
            // Arrange
            var texts = new List<string> { "Text 1", "Text 2", "Text 3" };
            var mockResponse = new
            {
                data = texts.Select((t, i) => new
                {
                    index = i,
                    embedding = Enumerable.Range(0, 1536).Select(j => (float)j / 1536).ToArray()
                }).ToArray(),
                usage = new { prompt_tokens = 30, total_tokens = 30 }
            };

            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(mockResponse))
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            var service = new AzureOpenAIEmbeddingService(
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _configurationMock.Object
            );

            // Act
            var result = await service.GenerateEmbeddingsAsync(texts);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.All(result, embedding => Assert.Equal(1536, embedding.Length));
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_ApiError_ThrowsException()
        {
            // Arrange
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("{\"error\": {\"message\": \"Bad request\"}}")
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            var service = new AzureOpenAIEmbeddingService(
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _configurationMock.Object
            );

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => 
                service.GenerateEmbeddingAsync("Test text")
            );
        }

        [Fact]
        public void GetEmbeddingDimension_Returns1536()
        {
            // Arrange
            var service = new AzureOpenAIEmbeddingService(
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _configurationMock.Object
            );

            // Act
            var dimension = service.GetEmbeddingDimension();

            // Assert
            Assert.Equal(1536, dimension);
        }

        [Fact]
        public void GetModelName_ReturnsCorrectFormat()
        {
            // Arrange
            var service = new AzureOpenAIEmbeddingService(
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _configurationMock.Object
            );

            // Act
            var modelName = service.GetModelName();

            // Assert
            Assert.Equal("azure/text-embedding-small", modelName);
        }

        [Fact]
        public async Task GenerateEmbeddingsAsync_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            var service = new AzureOpenAIEmbeddingService(
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _configurationMock.Object
            );

            // Act
            var result = await service.GenerateEmbeddingsAsync(new List<string>());

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
```

### 2.2 Create Embedding Generation Models

**File**: `/backend/Models/EmbeddingGenerationJob.cs`

```csharp
using System;

namespace RecruiterApi.Models
{
    /// <summary>
    /// Foundatio job for async embedding generation
    /// Queued when candidates are created/updated
    /// </summary>
    public class EmbeddingGenerationJob
    {
        public Guid CandidateId { get; set; }
        public string? ProfileText { get; set; }
        public string? ResumeText { get; set; }
        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
        public int RetryCount { get; set; } = 0;
        public int MaxRetries { get; set; } = 3;
    }
}
```

---

## Step 3: Foundatio Queue Integration (1.5 hours)

### 3.1 Setup Foundatio Services in Program.cs

**File**: `/backend/Program.cs` (Add after existing service registrations)

```csharp
// Add Foundatio services for embedding generation
using Foundatio.Queues;
using Foundatio.Caching;
using Foundatio.Messaging;

// Embedding queue
builder.Services.AddSingleton<IQueue<EmbeddingGenerationJob>>(provider => 
    new InMemoryQueue<EmbeddingGenerationJob>(new InMemoryQueueOptions 
    { 
        Retries = 3,
        RetryDelay = TimeSpan.FromSeconds(30)
    })
);

// Cache for search results
builder.Services.AddSingleton<ICacheClient>(provider => 
    new InMemoryCacheClient(new InMemoryCacheClientOptions())
);

// Message bus for events
builder.Services.AddSingleton<IMessageBus>(provider => 
    new InMemoryMessageBus(new InMemoryMessageBusOptions())
);

// Register embedding service
builder.Services.AddScoped<IEmbeddingService, AzureOpenAIEmbeddingService>();

// Register semantic search service
builder.Services.AddScoped<SemanticSearchService>();

// Add HTTP client factory for Azure OpenAI
builder.Services.AddHttpClient();
```

### 3.2 Create Embedding Generation Background Service

**File**: `/backend/Services/EmbeddingGenerationBackgroundService.cs`

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Foundatio.Queues;
using Npgsql;
using RecruiterApi.Models;

namespace RecruiterApi.Services
{
    /// <summary>
    /// Background service that processes embedding generation jobs from Foundatio queue
    /// Follows agents.md guidelines for using Foundatio for async operations
    /// </summary>
    public class EmbeddingGenerationBackgroundService : BackgroundService
    {
        private readonly ILogger<EmbeddingGenerationBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IQueue<EmbeddingGenerationJob> _queue;

        public EmbeddingGenerationBackgroundService(
            ILogger<EmbeddingGenerationBackgroundService> logger,
            IServiceProvider serviceProvider,
            IQueue<EmbeddingGenerationJob> queue)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _queue = queue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Embedding Generation Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Dequeue job from Foundatio queue
                    var entry = await _queue.DequeueAsync(stoppingToken);

                    if (entry != null)
                    {
                        await ProcessEmbeddingJobAsync(entry.Value, stoppingToken);
                        await entry.CompleteAsync();
                    }
                    else
                    {
                        // No jobs in queue, wait before checking again
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing embedding generation queue");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }

            _logger.LogInformation("Embedding Generation Background Service stopped");
        }

        private async Task ProcessEmbeddingJobAsync(
            EmbeddingGenerationJob job, 
            CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            try
            {
                _logger.LogInformation(
                    "Processing embedding generation for candidate {CandidateId}", 
                    job.CandidateId
                );

                // Generate embedding for profile
                var profileEmbedding = await embeddingService.GenerateEmbeddingAsync(
                    job.ProfileText ?? ""
                );

                // Store embedding in database
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);

                var vectorString = FormatVectorForPostgres(profileEmbedding);
                var sql = @"
                    UPDATE candidates 
                    SET profile_embedding = @vector::vector,
                        embedding_generated_at = NOW(),
                        embedding_model = @model
                    WHERE id = @id";

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@vector", vectorString);
                command.Parameters.AddWithValue("@model", embeddingService.GetModelName());
                command.Parameters.AddWithValue("@id", job.CandidateId);

                await command.ExecuteNonQueryAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully generated embedding for candidate {CandidateId}", 
                    job.CandidateId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, 
                    "Failed to generate embedding for candidate {CandidateId}. Retry: {RetryCount}/{MaxRetries}",
                    job.CandidateId,
                    job.RetryCount,
                    job.MaxRetries
                );

                // Requeue if retries available
                if (job.RetryCount < job.MaxRetries)
                {
                    job.RetryCount++;
                    await _queue.EnqueueAsync(job);
                }
            }
        }

        private string FormatVectorForPostgres(float[] vector)
        {
            return "[" + string.Join(",", vector.Select(v => v.ToString("G"))) + "]";
        }
    }
}
```

**Register in Program.cs**:
```csharp
builder.Services.AddHostedService<EmbeddingGenerationBackgroundService>();
```

---

## Step 4: API Endpoints (1 hour)

### 4.1 Add Semantic Search Endpoint

**File**: `/backend/Controllers/SearchController.cs` (Add new methods)

```csharp
[HttpPost("semantic")]
public async Task<ActionResult<CandidateSearchResponse>> SemanticSearch(
    [FromBody] SemanticSearchRequest request)
{
    try
    {
        var results = await _semanticSearchService.SemanticSearchCandidatesAsync(
            request.Query,
            request.Page,
            request.PageSize,
            request.SimilarityThreshold
        );

        return Ok(new CandidateSearchResponse
        {
            Results = results,
            TotalCount = results.Count,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error performing semantic search");
        return StatusCode(500, new { message = "Error performing semantic search" });
    }
}

[HttpPost("hybrid")]
public async Task<ActionResult<CandidateSearchResponse>> HybridSearch(
    [FromBody] HybridSearchRequest request)
{
    try
    {
        var results = await _semanticSearchService.HybridSearchAsync(
            request.Query,
            request.Page,
            request.PageSize,
            request.SemanticWeight,
            request.KeywordWeight
        );

        return Ok(new CandidateSearchResponse
        {
            Results = results,
            TotalCount = results.Count,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error performing hybrid search");
        return StatusCode(500, new { message = "Error performing hybrid search" });
    }
}

[HttpPost("generate-embeddings")]
public async Task<ActionResult> GenerateAllEmbeddings()
{
    try
    {
        var count = await _semanticSearchService.GenerateAllCandidateEmbeddingsAsync();
        return Ok(new { message = $"Generated embeddings for {count} candidates" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error generating embeddings");
        return StatusCode(500, new { message = "Error generating embeddings" });
    }
}
```

**DTOs**: `/backend/DTOs/SemanticSearchDto.cs`

```csharp
namespace RecruiterApi.DTOs
{
    public class SemanticSearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set} = 20;
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
```

---

## Step 5: Generate Initial Embeddings (Automated)

### 5.1 Create Bulk Embedding Generation Script

**File**: `/backend/Scripts/GenerateInitialEmbeddings.cs`

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Foundatio.Queues;
using Npgsql;
using RecruiterApi.Models;

namespace RecruiterApi.Scripts
{
    /// <summary>
    /// One-time script to generate embeddings for all 651 existing candidates
    /// Uses Foundatio queue for async processing
    /// </summary>
    public class GenerateInitialEmbeddings
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Bulk Embedding Generation for 651 Candidates ===\n");

            var serviceProvider = BuildServiceProvider();
            var queue = serviceProvider.GetRequiredService<IQueue<EmbeddingGenerationJob>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            var candidatesQueued = 0;

            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                // Get all active candidates without embeddings
                var sql = @"
                    SELECT c.id, c.full_name, c.current_title, r.resume_text
                    FROM candidates c
                    LEFT JOIN resumes r ON c.id = r.candidate_id
                    WHERE c.is_active = true 
                        AND c.profile_embedding IS NULL
                    ORDER BY c.created_at DESC";

                await using var command = new NpgsqlCommand(sql, connection);
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var candidateId = reader.GetGuid(0);
                    var fullName = reader.GetString(1);
                    var title = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    var resumeText = reader.IsDBNull(3) ? "" : reader.GetString(3);

                    // Combine profile information
                    var profileText = $"{fullName} {title} {resumeText}";

                    // Queue for embedding generation
                    await queue.EnqueueAsync(new EmbeddingGenerationJob
                    {
                        CandidateId = candidateId,
                        ProfileText = profileText,
                        ResumeText = resumeText
                    });

                    candidatesQueued++;

                    if (candidatesQueued % 50 == 0)
                    {
                        Console.WriteLine($"Queued {candidatesQueued} candidates...");
                    }
                }

                Console.WriteLine($"\n✅ Successfully queued {candidatesQueued} candidates for embedding generation");
                Console.WriteLine("Background service will process them asynchronously.");
                Console.WriteLine($"Estimated time: {candidatesQueued * 0.2 / 60:F1} minutes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
            }
        }

        private static IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();
            // Add configuration, logging, Foundatio services, etc.
            // (Implementation details)
            return services.BuildServiceProvider();
        }
    }
}
```

---

## Step 6: Frontend Integration (1.5 hours)

### 6.1 Update API Service

**File**: `/frontend/src/services/api.ts` (Add methods)

```typescript
// Semantic search types
export interface SemanticSearchRequest {
  query: string;
  page?: number;
  pageSize?: number;
  similarityThreshold?: number;
}

export interface HybridSearchRequest extends SemanticSearchRequest {
  semanticWeight?: number;
  keywordWeight?: number;
}

// Semantic search API
export const semanticSearch = async (
  request: SemanticSearchRequest
): Promise<CandidateSearchResponse> => {
  const response = await api.post('/search/semantic', request);
  return response.data;
};

export const hybridSearch = async (
  request: HybridSearchRequest
): Promise<CandidateSearchResponse> => {
  const response = await api.post('/search/hybrid', request);
  return response.data;
};

export const generateEmbeddings = async (): Promise<{ message: string }> => {
  const response = await api.post('/search/generate-embeddings');
  return response.data;
};
```

### 6.2 Create Semantic Search Hook

**File**: `/frontend/src/hooks/useSemanticSearch.ts`

```typescript
import { useState } from 'react';
import { useQuery, useMutation } from '@tanstack/react-query';
import { semanticSearch, hybridSearch, SemanticSearchRequest } from '../services/api';

export const useSemanticSearch = () => {
  const [request, setRequest] = useState<SemanticSearchRequest>({
    query: '',
    page: 1,
    pageSize: 20,
    similarityThreshold: 0.7
  });

  const { data, isLoading, error } = useQuery({
    queryKey: ['semantic-search', request],
    queryFn: () => semanticSearch(request),
    enabled: request.query.length > 0
  });

  return {
    results: data?.results || [],
    totalCount: data?.totalCount || 0,
    isLoading,
    error,
    setQuery: (query: string) => setRequest(prev => ({ ...prev, query })),
    setPage: (page: number) => setRequest(prev => ({ ...prev, page })),
    setSimilarityThreshold: (threshold: number) => 
      setRequest(prev => ({ ...prev, similarityThreshold: threshold }))
  };
};
```

---

## Step 7: Unit Tests (3 hours total)

### 7.1 Backend Unit Tests

Already included inline with each service above.

**Additional Test File**: `/backend/Tests/Services/SemanticSearchServiceTests.cs`

```csharp
[Fact]
public async Task SemanticSearchCandidatesAsync_ValidQuery_ReturnsResults()
{
    // Arrange
    var mockEmbeddingService = new Mock<IEmbeddingService>();
    mockEmbeddingService
        .Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>()))
        .ReturnsAsync(new float[1536]);

    var service = new SemanticSearchService(
        Mock.Of<ILogger<SemanticSearchService>>(),
        mockEmbeddingService.Object,
        _configurationMock.Object
    );

    // Act
    var results = await service.SemanticSearchCandidatesAsync("test query");

    // Assert
    Assert.NotNull(results);
}
```

### 7.2 Frontend Unit Tests

**File**: `/frontend/src/hooks/__tests__/useSemanticSearch.test.ts`

```typescript
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useSemanticSearch } from '../useSemanticSearch';
import * as api from '../../services/api';

jest.mock('../../services/api');

describe('useSemanticSearch', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  const wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );

  it('should fetch results when query is provided', async () => {
    const mockResults = {
      results: [{ id: '1', fullName: 'John Doe' }],
      totalCount: 1
    };

    (api.semanticSearch as jest.Mock).mockResolvedValue(mockResults);

    const { result } = renderHook(() => useSemanticSearch(), { wrapper });

    result.current.setQuery('React developer');

    await waitFor(() => {
      expect(result.current.results).toEqual(mockResults.results);
      expect(result.current.totalCount).toBe(1);
    });
  });

  it('should not fetch when query is empty', () => {
    const { result } = renderHook(() => useSemanticSearch(), { wrapper });

    expect(api.semanticSearch).not.toHaveBeenCalled();
    expect(result.current.results).toEqual([]);
  });
});
```

---

## Step 8: Integration & Testing (1 hour)

### 8.1 End-to-End Test Script

**File**: `/backend/Tests/Integration/SemanticSearchE2ETests.cs`

```csharp
[Fact]
public async Task CompleteSemanticSearchFlow_Success()
{
    // 1. Generate embedding for candidate
    var candidateId = Guid.NewGuid();
    await _queue.EnqueueAsync(new EmbeddingGenerationJob
    {
        CandidateId = candidateId,
        ProfileText = "Senior React Developer with 10 years experience"
    });

    // 2. Wait for background processing
    await Task.Delay(TimeSpan.FromSeconds(5));

    // 3. Perform semantic search
    var results = await _searchService.SemanticSearchCandidatesAsync(
        "experienced React engineer"
    );

    // 4. Verify results
    Assert.NotEmpty(results);
    Assert.Contains(results, r => r.Id == candidateId);
}
```

---

## Deployment Checklist

### Prerequisites
- [ ] Azure OpenAI embedding deployment created
- [ ] `.env` file updated with correct deployment name
- [ ] Docker containers running
- [ ] PostgreSQL accessible

### Phase 1 Steps
- [ ] Step 1.1: Install pgvector extension
- [ ] Step 1.2: Add embedding columns
- [ ] Step 2: Implement Azure OpenAI service
- [ ] Step 3: Setup Foundatio queues
- [ ] Step 4: Add API endpoints
- [ ] Step 5: Generate initial embeddings (651 candidates)
- [ ] Step 6: Frontend integration
- [ ] Step 7: Run all unit tests
- [ ] Step 8: Integration testing

### Validation
- [ ] Unit tests pass (backend: >90% coverage)
- [ ] Unit tests pass (frontend: >80% coverage)
- [ ] Semantic search returns relevant results
- [ ] Hybrid search combines keyword + semantic
- [ ] Background embedding generation works
- [ ] Performance: Search < 300ms
- [ ] All 651 candidates have embeddings

---

## Success Metrics

| Metric | Target | How to Measure |
|--------|--------|----------------|
| **Embedding Generation** | 651/651 candidates | `SELECT COUNT(*) FROM candidates WHERE profile_embedding IS NOT NULL` |
| **Search Latency** | < 300ms | API response time monitoring |
| **Search Relevance** | > 80% relevant | Manual testing with known queries |
| **Background Processing** | < 30 minutes total | Monitor queue processing |
| **Unit Test Coverage** | > 85% | dotnet test --collect:"XPlat Code Coverage" |

---

## Next Steps After Phase 1

1. **Phase 2**: Performance optimization & caching
2. **Phase 3**: Advanced features (filters, facets, personalization)
3. **Phase 4**: Analytics & search insights
4. **Phase 5**: Production deployment

---

**READY TO START?** 

Please provide your Azure OpenAI embedding deployment name, and we'll begin implementation! 🚀
