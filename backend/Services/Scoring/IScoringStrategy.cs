using RecruiterApi.DTOs;

namespace RecruiterApi.Services.Scoring
{
    /// <summary>
    /// Interface for different scoring strategies
    /// </summary>
    public interface IScoringStrategy
    {
        /// <summary>
        /// Strategy name for identification (option1, option4, etc.)
        /// </summary>
        string StrategyName { get; }

        /// <summary>
        /// Calculate final score for a candidate given their keyword and semantic scores
        /// </summary>
        /// <param name="keywordScores">Dictionary of keyword -> match score (0-1)</param>
        /// <param name="semanticScore">Overall semantic similarity (0-1)</param>
        /// <param name="totalKeywords">Total number of keywords in search query</param>
        /// <returns>Final similarity score (0-1)</returns>
        double CalculateScore(
            Dictionary<string, double> keywordScores, 
            double semanticScore, 
            int totalKeywords);

        /// <summary>
        /// Generate human-readable explanation for the score
        /// </summary>
        /// <param name="keywordScores">Dictionary of keyword -> match score (0-1)</param>
        /// <param name="semanticScore">Overall semantic similarity (0-1)</param>
        /// <param name="totalKeywords">Total number of keywords in search query</param>
        /// <param name="finalScore">The calculated final score (0-1)</param>
        /// <returns>Explanation text</returns>
        string GenerateExplanation(
            Dictionary<string, double> keywordScores,
            double semanticScore,
            int totalKeywords,
            double finalScore);
    }
}
