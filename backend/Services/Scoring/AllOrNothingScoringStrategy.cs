using RecruiterApi.Services.Scoring;

namespace RecruiterApi.Services.Scoring
{
    /// <summary>
    /// Option 1: All-or-Nothing Scoring Strategy
    /// 
    /// Rules:
    /// - If ALL keywords match: Return 100% score
    /// - If ANY keyword is missing: Return semantic score only
    /// 
    /// Use Case: Strict requirements where all skills are mandatory
    /// 
    /// Examples:
    /// - Query: "Kubernetes Yugabyte"
    ///   - Both found → 100%
    ///   - Only Kubernetes → 55% (semantic)
    ///   - Neither found → 35% (semantic)
    /// </summary>
    public class AllOrNothingScoringStrategy : IScoringStrategy
    {
        public string StrategyName => "option1";

        public double CalculateScore(
            Dictionary<string, double> keywordScores,
            double semanticScore,
            int totalKeywords)
        {
            // Check if ALL keywords matched (score > 0 means matched)
            var matchedKeywords = keywordScores.Count(kv => kv.Value > 0);
            var allKeywordsMatched = matchedKeywords == totalKeywords;

            if (allKeywordsMatched)
            {
                // All keywords matched → Return 100%
                return 1.0;
            }
            else
            {
                // Not all matched → Return semantic score only
                return semanticScore;
            }
        }

        public string GenerateExplanation(
            Dictionary<string, double> keywordScores,
            double semanticScore,
            int totalKeywords,
            double finalScore)
        {
            var matchedKeywords = keywordScores.Count(kv => kv.Value > 0);
            var allMatched = matchedKeywords == totalKeywords;

            if (allMatched)
            {
                return $"Perfect match! All {totalKeywords} keywords found in candidate profile. " +
                       $"Score: 100% (All-or-Nothing strategy applied).";
            }
            else
            {
                return $"Partial match: {matchedKeywords} of {totalKeywords} keywords matched. " +
                       $"Score: {Math.Round(semanticScore * 100)}% based on semantic similarity only " +
                       $"(All-or-Nothing strategy requires all keywords for 100% match).";
            }
        }
    }
}
