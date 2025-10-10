using System.Threading.Tasks;

namespace RecruiterApi.Services
{
    public interface IAISummaryService
    {
        Task<string> GenerateResumeSummaryAsync(string resumeText, Guid candidateId, Guid resumeId);
    }
}
