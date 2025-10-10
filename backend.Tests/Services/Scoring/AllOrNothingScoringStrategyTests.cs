using Xunit;
using RecruiterApi.Services.Scoring;

namespace RecruiterApi.Tests.Services.Scoring
{
    /// <summary>
    /// Unit tests for AllOrNothingScoringStrategy (Option 1)
    /// 
    /// Strategy Rule:
    /// - If ALL keywords match: Return 100%
    /// - If ANY keyword is missing: Return semantic score only
    /// </summary>
    public class AllOrNothingScoringStrategyTests
    {
        private readonly AllOrNothingScoringStrategy _strategy;

        public AllOrNothingScoringStrategyTests()
        {
            _strategy = new AllOrNothingScoringStrategy();
        }

        [Fact]
        public void StrategyName_ReturnsOption1()
        {
            // Act
            var name = _strategy.StrategyName;

            // Assert
            Assert.Equal("option1", name);
        }

        [Fact]
        public void CalculateScore_AllKeywordsMatched_Returns100Percent()
        {
            // Arrange - All 3 keywords matched (Kubernetes, Yugabyte, PostgreSQL)
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 1.0 },    // Found in title
                { "yugabyte", 0.9 },      // Found in skills
                { "postgresql", 0.85 }    // Found in resume (high frequency)
            };
            var semanticScore = 0.92;
            var totalKeywords = 3;

            // Act
            var score = _strategy.CalculateScore(keywordScores, semanticScore, totalKeywords);

            // Assert
            Assert.Equal(1.0, score, 2); // 100% - exact match
        }

        [Fact]
        public void CalculateScore_TwoOfThreeKeywordsMatched_ReturnsSemanticScore()
        {
            // Arrange - Only 2 of 3 keywords matched
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 1.0 },     // Found
                { "yugabyte", 0.0 },       // NOT found
                { "postgresql", 0.8 }      // Found
            };
            var semanticScore = 0.65;
            var totalKeywords = 3;

            // Act
            var score = _strategy.CalculateScore(keywordScores, semanticScore, totalKeywords);

            // Assert
            Assert.Equal(0.65, score, 2); // Returns semantic score only
        }

        [Fact]
        public void CalculateScore_OneOfThreeKeywordsMatched_ReturnsSemanticScore()
        {
            // Arrange - Only 1 of 3 keywords matched
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 0.7 },  // Found (low frequency)
                { "yugabyte", 0.0 },    // NOT found
                { "postgresql", 0.0 }   // NOT found
            };
            var semanticScore = 0.55;
            var totalKeywords = 3;

            // Act
            var score = _strategy.CalculateScore(keywordScores, semanticScore, totalKeywords);

            // Assert
            Assert.Equal(0.55, score, 2); // Returns semantic score only
        }

        [Fact]
        public void CalculateScore_NoKeywordsMatched_ReturnsSemanticScore()
        {
            // Arrange - Pure semantic match (Docker, distributed systems background)
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 0.0 },
                { "yugabyte", 0.0 },
                { "postgresql", 0.0 }
            };
            var semanticScore = 0.75; // High semantic similarity despite no keyword matches
            var totalKeywords = 3;

            // Act
            var score = _strategy.CalculateScore(keywordScores, semanticScore, totalKeywords);

            // Assert
            Assert.Equal(0.75, score, 2); // Returns semantic score
        }

        [Fact]
        public void CalculateScore_AllKeywordsMatched_WithLowSemanticScore_Still100Percent()
        {
            // Arrange - All keywords match but low semantic score
            // This tests that keyword match takes precedence
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 0.5 },  // Mentioned once
                { "docker", 0.5 }       // Mentioned once
            };
            var semanticScore = 0.40; // Low semantic similarity
            var totalKeywords = 2;

            // Act
            var score = _strategy.CalculateScore(keywordScores, semanticScore, totalKeywords);

            // Assert
            Assert.Equal(1.0, score, 2); // Still 100% because all keywords matched
        }

        [Fact]
        public void GenerateExplanation_AllMatched_ReturnsCorrectMessage()
        {
            // Arrange
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 1.0 },
                { "docker", 0.95 }
            };
            var semanticScore = 0.88;
            var totalKeywords = 2;
            var finalScore = 1.0;

            // Act
            var explanation = _strategy.GenerateExplanation(
                keywordScores, semanticScore, totalKeywords, finalScore);

            // Assert
            Assert.Contains("Perfect match", explanation);
            Assert.Contains("All 2 keywords", explanation);
            Assert.Contains("100%", explanation);
            Assert.Contains("All-or-Nothing", explanation);
        }

        [Fact]
        public void GenerateExplanation_PartialMatch_ReturnsCorrectMessage()
        {
            // Arrange
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 1.0 },  // Matched
                { "docker", 0.0 }       // Not matched
            };
            var semanticScore = 0.62;
            var totalKeywords = 2;
            var finalScore = 0.62;

            // Act
            var explanation = _strategy.GenerateExplanation(
                keywordScores, semanticScore, totalKeywords, finalScore);

            // Assert
            Assert.Contains("Partial match", explanation);
            Assert.Contains("1 of 2 keywords", explanation);
            Assert.Contains("62%", explanation);
            Assert.Contains("semantic similarity only", explanation);
            Assert.Contains("All-or-Nothing", explanation);
        }

        [Fact]
        public void CalculateScore_SingleKeywordMatched_Returns100Percent()
        {
            // Arrange - Single keyword search that matches
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 0.95 }
            };
            var semanticScore = 0.70;
            var totalKeywords = 1;

            // Act
            var score = _strategy.CalculateScore(keywordScores, semanticScore, totalKeywords);

            // Assert
            Assert.Equal(1.0, score, 2); // 100% because the only keyword matched
        }

        [Fact]
        public void CalculateScore_SingleKeywordNotMatched_ReturnsSemanticScore()
        {
            // Arrange - Single keyword search that doesn't match
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 0.0 }
            };
            var semanticScore = 0.55;
            var totalKeywords = 1;

            // Act
            var score = _strategy.CalculateScore(keywordScores, semanticScore, totalKeywords);

            // Assert
            Assert.Equal(0.55, score, 2); // Returns semantic score
        }
    }
}
