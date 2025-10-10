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
    /// Triggered when candidates are imported via Excel
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

            // Wait a bit for the application to fully start
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Dequeue job from Foundatio queue
                    var entry = await _queue.DequeueAsync(TimeSpan.FromSeconds(5));

                    if (entry != null)
                    {
                        _logger.LogInformation(
                            "Dequeued embedding job for candidate {CandidateId} from {Source}",
                            entry.Value.CandidateId,
                            entry.Value.Source
                        );

                        await ProcessEmbeddingJobAsync(entry.Value, stoppingToken);
                        await entry.CompleteAsync();
                        
                        _logger.LogInformation(
                            "Completed embedding job for candidate {CandidateId}",
                            entry.Value.CandidateId
                        );
                    }
                    else
                    {
                        // No jobs in queue, wait before checking again
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
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
                    "Processing embedding generation for candidate {CandidateId} (attempt {Attempt}/{MaxRetries})", 
                    job.CandidateId,
                    job.RetryCount + 1,
                    job.MaxRetries
                );

                // Check if embedding service is available
                var isAvailable = await embeddingService.IsAvailableAsync();
                if (!isAvailable)
                {
                    _logger.LogWarning("Embedding service is not available. Will retry.");
                    throw new Exception("Embedding service unavailable");
                }

                // Generate embedding for profile
                var profileEmbedding = await embeddingService.GenerateEmbeddingAsync(
                    job.ProfileText ?? ""
                );

                if (profileEmbedding == null || profileEmbedding.Length == 0)
                {
                    _logger.LogWarning(
                        "Empty embedding generated for candidate {CandidateId}",
                        job.CandidateId
                    );
                    return;
                }

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

                var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation(
                        "Successfully generated and stored embedding for candidate {CandidateId} using model {Model}. Dimensions: {Dimensions}", 
                        job.CandidateId,
                        embeddingService.GetModelName(),
                        profileEmbedding.Length
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "No rows updated for candidate {CandidateId}. Candidate may not exist.",
                        job.CandidateId
                    );
                }
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
                    _logger.LogInformation(
                        "Requeued embedding job for candidate {CandidateId}. Retry {RetryCount}/{MaxRetries}",
                        job.CandidateId,
                        job.RetryCount,
                        job.MaxRetries
                    );
                }
                else
                {
                    _logger.LogError(
                        "Max retries reached for candidate {CandidateId}. Giving up.",
                        job.CandidateId
                    );
                }
            }
        }

        private string FormatVectorForPostgres(float[] vector)
        {
            return "[" + string.Join(",", vector.Select(v => v.ToString("G"))) + "]";
        }
    }
}
