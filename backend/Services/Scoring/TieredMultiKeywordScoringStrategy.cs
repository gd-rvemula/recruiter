using RecruiterApi.Services.Scoring;

namespace RecruiterApi.Services.Scoring
{
    /// <summary>
    /// Option 4: Tiered Multi-Keyword Scoring Strategy
    /// 
    /// Rules:
    /// - All keywords matched (100% coverage): Guarantee 85%+ score
    /// - Half or more matched (≥50% coverage): Balanced keyword + semantic
    /// - Less than half matched (<50% coverage): Rely on semantic similarity
    /// 
    /// Use Case: Flexible requirements where partial matches are valuable
    /// 
    /// Examples:
    /// Query: "Kubernetes Yugabyte PostgreSQL" (3 keywords)
    /// 
    /// Candidate A - All 3 keywords (expert):
    /// - kubernetes: 1.0, yugabyte: 0.9, postgresql: 0.85
    /// - Coverage: 100%, Avg Quality: 0.916, Semantic: 0.92
    /// - Score: max(0.85, 0.916*0.7 + 0.92*0.3) = 92%
    /// 
    /// Candidate B - 2 of 3 keywords:
    /// - kubernetes: 1.0, yugabyte: 0.0, postgresql: 0.8
    /// - Coverage: 67%, Avg Quality: 0.60, Semantic: 0.70
    /// - Score: (0.60 * 0.67 * 0.6) + (0.70 * 0.4) = 52%
    /// 
    /// Candidate C - 1 of 3 keywords:
    /// - kubernetes: 0.7, yugabyte: 0.0, postgresql: 0.0
    /// - Coverage: 33%, Semantic: 0.65
    /// - Score: 0.65 * 0.8 = 52%
    /// 
    /// Candidate D - 0 keywords (pure semantic):
    /// - Coverage: 0%, Semantic: 0.75 (Docker, distributed systems)
    /// - Score: 0.75 * 0.8 = 60%
    /// </summary>
    public class TieredMultiKeywordScoringStrategy : IScoringStrategy
    {
        public string StrategyName => "option4";

        public double CalculateScore(
            Dictionary<string, double> keywordScores,
            double semanticScore,
            int totalKeywords)
        {
            // Calculate keyword coverage (percentage of keywords matched)
            var matchedCount = keywordScores.Count(kv => kv.Value > 0);
            var coverage = (double)matchedCount / totalKeywords;

            // Calculate average quality of matched keywords
            var matchedScores = keywordScores.Where(kv => kv.Value > 0).Select(kv => kv.Value);
            var avgQuality = matchedScores.Any() ? matchedScores.Average() : 0.0;

            // Tiered scoring based on coverage
            if (coverage == 1.0) // All keywords matched (100% coverage)
            {
                // Guarantee 85%+ for all exact matches
                // Formula: max(0.85, avgQuality*0.7 + semanticScore*0.3)
                return Math.Max(0.85, avgQuality * 0.7 + semanticScore * 0.3);
            }
            else if (coverage >= 0.5) // Half or more matched (≥50% coverage)
            {
                // Balanced approach for partial matches
                // Formula: (avgQuality * coverage * 0.6) + (semanticScore * 0.4)
                return (avgQuality * coverage * 0.6) + (semanticScore * 0.4);
            }
            else // Less than half matched (<50% coverage)
            {
                // Rely heavily on semantic similarity
                // Formula: semanticScore * 0.8
                return semanticScore * 0.8;
            }
        }

        public string GenerateExplanation(
            Dictionary<string, double> keywordScores,
            double semanticScore,
            int totalKeywords,
            double finalScore)
        {
            var matchedCount = keywordScores.Count(kv => kv.Value > 0);
            var coverage = (double)matchedCount / totalKeywords;
            var coveragePercent = Math.Round(coverage * 100);

            if (coverage == 1.0)
            {
                return $"Excellent match! All {totalKeywords} keywords found ({coveragePercent}% coverage). " +
                       $"Final score: {Math.Round(finalScore * 100)}% (Tiered scoring with full keyword coverage).";
            }
            else if (coverage >= 0.5)
            {
                return $"Good match: {matchedCount} of {totalKeywords} keywords matched ({coveragePercent}% coverage). " +
                       $"Final score: {Math.Round(finalScore * 100)}% combines keyword quality and semantic similarity " +
                       $"(Tiered scoring with partial coverage).";
            }
            else
            {
                return $"Semantic match: {matchedCount} of {totalKeywords} keywords matched ({coveragePercent}% coverage). " +
                       $"Final score: {Math.Round(finalScore * 100)}% based primarily on semantic similarity " +
                       $"(Tiered scoring favors semantic analysis for low keyword coverage).";
            }
        }
    }
}
