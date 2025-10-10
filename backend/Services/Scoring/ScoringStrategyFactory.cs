namespace RecruiterApi.Services.Scoring
{
    /// <summary>
    /// Factory for creating scoring strategy instances
    /// </summary>
    public class ScoringStrategyFactory
    {
        private readonly Dictionary<string, IScoringStrategy> _strategies;

        public ScoringStrategyFactory()
        {
            // Register all available strategies
            _strategies = new Dictionary<string, IScoringStrategy>(StringComparer.OrdinalIgnoreCase)
            {
                { "option1", new AllOrNothingScoringStrategy() },
                { "option4", new TieredMultiKeywordScoringStrategy() }
            };
        }

        /// <summary>
        /// Get scoring strategy by name
        /// </summary>
        /// <param name="strategyName">Strategy name (option1, option4, etc.)</param>
        /// <returns>Scoring strategy instance</returns>
        public IScoringStrategy GetStrategy(string strategyName)
        {
            if (string.IsNullOrWhiteSpace(strategyName))
            {
                // Default to option1 if not specified
                return _strategies["option1"];
            }

            if (_strategies.TryGetValue(strategyName.ToLower(), out var strategy))
            {
                return strategy;
            }

            // Default to option1 if unknown strategy
            return _strategies["option1"];
        }

        /// <summary>
        /// Get all available strategy names
        /// </summary>
        /// <returns>List of strategy names</returns>
        public IEnumerable<string> GetAvailableStrategies()
        {
            return _strategies.Keys;
        }

        /// <summary>
        /// Check if a strategy exists
        /// </summary>
        /// <param name="strategyName">Strategy name to check</param>
        /// <returns>True if strategy exists</returns>
        public bool HasStrategy(string strategyName)
        {
            return _strategies.ContainsKey(strategyName?.ToLower() ?? string.Empty);
        }
    }
}
