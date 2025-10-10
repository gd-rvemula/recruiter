using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RecruiterApi.Data;
using RecruiterApi.Models;

namespace RecruiterApi.Services
{
    public class AISummaryService : IAISummaryService
    {
        private readonly HttpClient _httpClient;
        private readonly RecruiterDbContext _context;
        private readonly string _openAiEndpoint;
        private readonly string _openAiApiKey;
        private readonly string _openAiDeployment;
        private readonly string _llmPrompt;

        public AISummaryService(IHttpClientFactory httpClientFactory, RecruiterDbContext context, IConfiguration config)
        {
            _httpClient = httpClientFactory.CreateClient();
            _context = context;
            _openAiEndpoint = config["AZURE_OPENAI_ENDPOINT"] 
                ?? throw new ArgumentNullException("AZURE_OPENAI_ENDPOINT not configured");
            _openAiApiKey = config["AZURE_OPENAI_API_KEY"] 
                ?? throw new ArgumentNullException("AZURE_OPENAI_API_KEY not configured");
            _openAiDeployment = config["AZURE_OPENAI_DEPLOYMENT"] 
                ?? throw new ArgumentNullException("AZURE_OPENAI_DEPLOYMENT not configured");
            _llmPrompt = config["LLM_PROMPT"] 
                ?? throw new ArgumentNullException("LLM_PROMPT not configured");
        }

        public async Task<string> GenerateResumeSummaryAsync(string resumeText, Guid candidateId, Guid resumeId)
        {
            if (string.IsNullOrWhiteSpace(resumeText))
            {
                return "No resume text provided.";
            }

            // Calculate hash of resume text for cache lookup
            var resumeTextHash = ComputeSha256Hash(resumeText);

            // Check if we have a cached summary for this resume
            var cachedSummary = await _context.AISummaries
                .Where(a => a.ResumeId == resumeId && a.ResumeTextHash == resumeTextHash)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (cachedSummary != null)
            {
                Console.WriteLine($"[AISummary] Cache HIT for Resume {resumeId} - Returning cached summary");
                return cachedSummary.SummaryText;
            }

            Console.WriteLine($"[AISummary] Cache MISS for Resume {resumeId} - Calling Azure OpenAI");

            // Clean up the prompt
            var prompt = _llmPrompt.Replace("\n", " ").Replace("\r", " ").Trim();
            
            // Use Azure OpenAI Chat Completions API (not legacy completions)
            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = prompt },
                    new { role = "user", content = $"Please analyze this resume:\n\n{resumeText}" }
                },
                max_tokens = 800,
                temperature = 0.3,
                top_p = 0.95
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_openAiEndpoint}/openai/deployments/{_openAiDeployment}/chat/completions?api-version=2024-02-15-preview")
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("api-key", _openAiApiKey);

            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Azure OpenAI API error: {response.StatusCode} - {responseJson}");
            }
            
            using var doc = JsonDocument.Parse(responseJson);
            var summary = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            var summaryText = summary ?? "No summary generated.";

            // Save the summary to cache
            var aiSummary = new AISummary
            {
                CandidateId = candidateId,
                ResumeId = resumeId,
                SummaryText = summaryText,
                ResumeTextHash = resumeTextHash
            };

            _context.AISummaries.Add(aiSummary);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[AISummary] Cached new summary for Resume {resumeId}");
            
            return summaryText;
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
