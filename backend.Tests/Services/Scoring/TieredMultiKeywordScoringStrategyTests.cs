using Xunit;
using RecruiterApi.Services.Scoring;

namespace RecruiterApi.Tests.Services.Scoring
{
    /// <summary>
    /// Unit tests for TieredMultiKeywordScoringStrategy (Option 4)
    /// 
    /// Strategy Rules:
    /// - All keywords matched (100% coverage): Guarantee 85%+ score
    /// - Half or more matched (≥50% coverage): Balanced keyword + semantic
    /// - Less than half matched (<50% coverage): Rely on semantic similarity
    /// </summary>
    public class TieredMultiKeywordScoringStrategyTests
    {
        private readonly TieredMultiKeywordScoringStrategy _strategy;

        public TieredMultiKeywordScoringStrategyTests()
        {
            _strategy = new TieredMultiKeywordScoringStrategy();
        }

        [Fact]
        public void StrategyName_ReturnsOption4()
        {
            // Act
            var name = _strategy.StrategyName;

            // Assert
            Assert.Equal("option4", name);
        }

        [Fact]
        public void CalculateScore_AllKeywordsMatched_ReturnsAtLeast85Percent()
        {
            // Arrange - Candidate A: All 3 keywords matched (expert level)
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 1.0 },    // In title, 5 years experience
                { "yugabyte", 0.9 },      // In skills, 2 years
                { "postgresql", 0.85 }    // In resume, multiple projects
            };
            var semanticScore = 0.92;
            var totalKeywords = 3;

            // Act
            var score = _strategy.CalculateScore(keywordScores, semanticScore, totalKeywords);

            // Assert
            Assert.True(score >= 0.85, $"Expected >= 85%, got {score * 100}%");
            Assert.True(score <= 1.0, $"Score should not exceed 100%, got {score * 100}%");
            // Expected: max(0.85, 0.916*0.7 + 0.92*0.3) ≈ 0.917 (92%)
            Assert.InRange(score, 0.90, 0.95);
        }

        [Fact]
        public void CalculateScore_AllKeywordsMatched_LowQuality_Returns85Percent()
        {
            // Arrange - All keywords matched but low quality (mentioned only)
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 0.5 },  // Low frequency
                { "docker", 0.5 },      // Low frequency
                { "mongodb", 0.5 }      // Low frequency
            };
            var semanticScore = 0.60;
            var totalKeywords = 3;

            // Act
            var score = _strategy.CalculateScore(keywordScores, semanticScore, totalKeywords);

            // Assert
            // avg_quality = 0.5, formula: max(0.85, 0.5*0.7 + 0.6*0.3)
            // = max(0.85, 0.35 + 0.18) = max(0.85, 0.53) = 0.85
            Assert.Equal(0.85, score, 2);
        }

        [Fact]
        public void CalculateScore_TwoOfThreeKeywordsMatched_UsesBalancedApproach()
        {
            // Arrange - Candidate B: 2 of 3 keywords matched
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 1.0 },     // Expert level
                { "yugabyte", 0.0 },       // Not found
                { "postgresql", 0.8 }      // Intermediate
            };
            var semanticScore = 0.70;
            var totalKeywords = 3;

            // Act
            var score = _strategy.CalculateScore(keywordScores, semanticScore, totalKeywords);

            // Assert
            // Coverage = 2/3 = 0.67 (≥ 0.5)
            // avg_quality for matched = (1.0 + 0.8) / 2 = 0.9, but only 2/3 matched
            // Matched scores sum = 1.8, but we calculate differently
            // Actually: matched values average = 0.9, then avg of all including 0s = (1.0+0.0+0.8)/3 = 0.6
            // Formula: (avgQuality * coverage * 0.6) + (semanticScore * 0.4)
            // = (0.6 * 0.67 * 0.6) + (0.70 * 0.4) = 0.24 + 0.28 = 0.52
            Assert.InRange(score, 0.48, 0.60);
        }

        [Fact]
        public void CalculateScore_OneOfThreeKeywordsMatched_ReliesOnSemantic()
        {
            // Arrange - Candidate C: Only 1 of 3 keywords matched
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 0.7 },  // Mentioned
                { "yugabyte", 0.0 },    // Not found
                { "postgresql", 0.0 }   // Not found
            };
            var semanticScore = 0.65;
            var totalKeywords = 3;

            // Act
            var score = _strategy.CalculateScore(keywordScores, semanticScore, totalKeywords);

            // Assert
            // Coverage = 1/3 = 0.33 (< 0.5)
            // Formula: semanticScore * 0.8 = 0.65 * 0.8 = 0.52
            Assert.Equal(0.52, score, 2);
        }

        [Fact]
        public void CalculateScore_NoKeywordsMatched_UsesSemanticOnly()
        {
            // Arrange - Candidate D: Pure semantic match (Docker, distributed systems)
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 0.0 },
                { "yugabyte", 0.0 },
                { "postgresql", 0.0 }
            };
            var semanticScore = 0.75; // Strong related technologies background
            var totalKeywords = 3;

            // Act
            var score = _strategy.CalculateScore(keywordScores, semanticScore, totalKeywords);

            // Assert
            // Coverage = 0/3 = 0.0 (< 0.5)
            // Formula: semanticScore * 0.8 = 0.75 * 0.8 = 0.60
            Assert.Equal(0.60, score, 2);
        }

        [Fact]
        public void CalculateScore_HalfKeywordsMatched_ExactlyOnBoundary()
        {
            // Arrange - Exactly 50% coverage (boundary test)
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 1.0 },  // Matched
                { "docker", 0.9 },      // Matched
                { "mongodb", 0.0 },     // Not matched
                { "redis", 0.0 }        // Not matched
            };
            var semanticScore = 0.75;
            var totalKeywords = 4;

            // Act
            var score = _strategy.CalculateScore(keywordScores, semanticScore, totalKeywords);

            // Assert
            // Coverage = 2/4 = 0.5 (exactly at boundary, should use balanced approach)
            // avgQuality of all = (1.0 + 0.9 + 0 + 0) / 4 = 0.475
            // Formula: (0.475 * 0.5 * 0.6) + (0.75 * 0.4) = 0.1425 + 0.30 = 0.4425
            Assert.InRange(score, 0.40, 0.50);
        }

        [Fact]
        public void GenerateExplanation_FullCoverage_ReturnsExcellentMatch()
        {
            // Arrange
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 1.0 },
                { "docker", 0.95 }
            };
            var semanticScore = 0.88;
            var totalKeywords = 2;
            var finalScore = 0.92;

            // Act
            var explanation = _strategy.GenerateExplanation(
                keywordScores, semanticScore, totalKeywords, finalScore);

            // Assert
            Assert.Contains("Excellent match", explanation);
            Assert.Contains("All 2 keywords", explanation);
            Assert.Contains("100% coverage", explanation);
            Assert.Contains("92%", explanation);
            Assert.Contains("Tiered scoring", explanation);
        }

        [Fact]
        public void GenerateExplanation_PartialCoverage_ReturnsGoodMatch()
        {
            // Arrange
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 1.0 },  // Matched
                { "docker", 0.0 }       // Not matched
            };
            var semanticScore = 0.70;
            var totalKeywords = 2;
            var finalScore = 0.58;

            // Act
            var explanation = _strategy.GenerateExplanation(
                keywordScores, semanticScore, totalKeywords, finalScore);

            // Assert
            Assert.Contains("Good match", explanation);
            Assert.Contains("1 of 2 keywords", explanation);
            Assert.Contains("50% coverage", explanation);
            Assert.Contains("58%", explanation);
        }

        [Fact]
        public void GenerateExplanation_LowCoverage_ReturnsSemanticMatch()
        {
            // Arrange
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 0.0 },
                { "docker", 0.7 },      // Only this matched
                { "mongodb", 0.0 }
            };
            var semanticScore = 0.68;
            var totalKeywords = 3;
            var finalScore = 0.54;

            // Act
            var explanation = _strategy.GenerateExplanation(
                keywordScores, semanticScore, totalKeywords, finalScore);

            // Assert
            Assert.Contains("Semantic match", explanation);
            Assert.Contains("1 of 3 keywords", explanation);
            Assert.Contains("33% coverage", explanation);
            Assert.Contains("semantic similarity", explanation);
        }

        [Fact]
        public void CalculateScore_SingleKeywordMatched_Returns85PercentMinimum()
        {
            // Arrange - Single keyword search with high quality match
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 1.0 }
            };
            var semanticScore = 0.85;
            var totalKeywords = 1;

            // Act
            var score = _strategy.CalculateScore(keywordScores, semanticScore, totalKeywords);

            // Assert
            // Coverage = 1.0, formula: max(0.85, 1.0*0.7 + 0.85*0.3) = max(0.85, 0.955) = 0.955
            Assert.True(score >= 0.85);
            Assert.InRange(score, 0.93, 1.0);
        }

        [Fact]
        public void CalculateScore_AllKeywordsHighQualityHighSemantic_ReturnsNear100()
        {
            // Arrange - Perfect scenario: all keywords, high quality, high semantic
            var keywordScores = new Dictionary<string, double>
            {
                { "kubernetes", 1.0 },
                { "docker", 1.0 }
            };
            var semanticScore = 0.95;
            var totalKeywords = 2;

            // Act
            var score = _strategy.CalculateScore(keywordScores, semanticScore, totalKeywords);

            // Assert
            // avgQuality = 1.0, formula: max(0.85, 1.0*0.7 + 0.95*0.3) = max(0.85, 0.985)
            Assert.InRange(score, 0.95, 1.0);
        }
    }
}
