using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecruiterApi.Services
{
    /// <summary>
    /// Interface for generating text embeddings for semantic search
    /// Abstraction allows switching between Ollama, Azure OpenAI, etc.
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// Generate embedding vector for a single text
        /// </summary>
        Task<float[]> GenerateEmbeddingAsync(string text);

        /// <summary>
        /// Generate embeddings for multiple texts in batch
        /// </summary>
        Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts);

        /// <summary>
        /// Get the dimension size of embeddings
        /// </summary>
        int GetEmbeddingDimension();

        /// <summary>
        /// Get the model name being used
        /// </summary>
        string GetModelName();

        /// <summary>
        /// Check if the embedding service is available and responsive
        /// </summary>
        Task<bool> IsAvailableAsync();
    }
}