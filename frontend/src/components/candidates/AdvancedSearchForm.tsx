import React, { useState, useEffect } from 'react';
import { useQuickSearch } from '../../hooks/useAdvancedSearch';

interface QuickSearchProps {
  onSearchResults?: (results: any[]) => void;
  className?: string;
}

const QuickSearch: React.FC<QuickSearchProps> = ({ 
  onSearchResults, 
  className = '' 
}) => {
  const {
    results,
    totalCount,
    isLoading,
    error,
    hasSearched,
    searchTerm,
    quickSearch,
    clearSearch
  } = useQuickSearch();

  const [showSuggestions, setShowSuggestions] = useState(false);

  // Pass results to parent component
  useEffect(() => {
    if (onSearchResults && results.length > 0) {
      onSearchResults(results);
    }
  }, [results, onSearchResults]);

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!searchTerm.trim()) return;

    quickSearch(searchTerm);
    setShowSuggestions(false);
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    if (value.length > 2) {
      quickSearch(value);
    }
  };

  const handleClear = () => {
    clearSearch();
    setShowSuggestions(false);
  };

  return (
    <div className={`bg-white p-6 rounded-lg shadow ${className}`}>
      <div className="space-y-4">
        <h3 className="text-lg font-medium text-gray-900">Search Candidates</h3>
        
        {/* Error Display */}
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
            <span className="block sm:inline">{error}</span>
          </div>
        )}

        {/* Search Form */}
        <form onSubmit={handleSearch} className="space-y-4">
          <div className="relative">
            <input
              type="text"
              value={searchTerm}
              onChange={handleInputChange}
              placeholder="Search resumes and skills... (e.g., 'React developer', 'Python machine learning', 'AWS cloud')"
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-blue-500 focus:border-blue-500"
              disabled={isLoading}
            />
          </div>

          <div className="flex space-x-2">
            <button
              type="submit"
              disabled={isLoading || !searchTerm.trim()}
              className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? 'Searching...' : 'Search'}
            </button>
            <button
              type="button"
              onClick={handleClear}
              className="px-4 py-2 bg-gray-200 text-gray-800 rounded-lg hover:bg-gray-300"
            >
              Clear
            </button>
          </div>
        </form>

        {/* Results Summary */}
        {hasSearched && (
          <div className="mt-4 p-3 bg-gray-50 rounded-lg">
            <p className="text-sm text-gray-600">
              {isLoading ? (
                'Searching...'
              ) : (
                `Found ${totalCount} candidates${results.length > 0 ? ` (showing ${results.length})` : ''}`
              )}
            </p>
          </div>
        )}

        {/* Quick Results Preview */}
        {results.length > 0 && (
          <div className="mt-4">
            <h4 className="text-md font-medium text-gray-900 mb-2">Search Results:</h4>
            <div className="space-y-2 max-h-60 overflow-y-auto">
              {results.slice(0, 5).map((candidate, index) => (
                <div key={candidate.id || index} className="p-3 bg-gray-50 rounded border">
                  <div className="flex justify-between items-start">
                    <div>
                      <h5 className="font-medium text-gray-900">
                        {candidate.firstName} {candidate.lastName}
                      </h5>
                      <p className="text-sm text-gray-600">{candidate.email}</p>
                      {candidate.primarySkills && (
                        <p className="text-sm text-blue-600 mt-1">
                          Skills: {candidate.primarySkills}
                        </p>
                      )}
                    </div>
                    {candidate.searchRank && (
                      <span className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded">
                        Rank: {candidate.searchRank.toFixed(2)}
                      </span>
                    )}
                  </div>
                </div>
              ))}
              {results.length > 5 && (
                <p className="text-sm text-gray-500 text-center">
                  And {results.length - 5} more results...
                </p>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default QuickSearch;