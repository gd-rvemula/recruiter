import React, { useState } from 'react';
import { CandidateSearchRequest } from '../../types/candidate';

interface SearchFiltersProps {
  onSearch: (filters: CandidateSearchRequest) => void;
  loading: boolean;
  totalCount: number;
}

export const SearchFilters: React.FC<SearchFiltersProps> = ({
  onSearch,
  loading,
  totalCount,
}) => {
  const [filters, setFilters] = useState<CandidateSearchRequest>({
    page: 1,
    pageSize: 20,
    skills: [],
    companies: [],
    jobTitles: [],
    sortDirection: 'asc',
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSearch({ ...filters, page: 1 }); // Reset to first page on new search
  };

  const handleInputChange = (field: keyof CandidateSearchRequest, value: any) => {
    setFilters(prev => ({ ...prev, [field]: value }));
  };

  const clearFilters = () => {
    const defaultFilters: CandidateSearchRequest = {
      page: 1,
      pageSize: 20,
      skills: [],
      companies: [],
      jobTitles: [],
      sortDirection: 'asc',
    };
    setFilters(defaultFilters);
    onSearch(defaultFilters);
  };

  return (
    <div className="bg-white rounded-lg shadow p-6 mb-6">
      <form onSubmit={handleSubmit}>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-4">
          {/* Search Term */}
          <div>
            <label htmlFor="searchTerm" className="block text-sm font-medium text-gray-700 mb-1">
              Search Name/Email
            </label>
            <input
              type="text"
              id="searchTerm"
              value={filters.searchTerm || ''}
              onChange={(e) => handleInputChange('searchTerm', e.target.value)}
              placeholder="Enter name or email..."
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          {/* Current Title */}
          <div>
            <label htmlFor="currentTitle" className="block text-sm font-medium text-gray-700 mb-1">
              Current Title
            </label>
            <input
              type="text"
              id="currentTitle"
              value={filters.currentTitle || ''}
              onChange={(e) => handleInputChange('currentTitle', e.target.value)}
              placeholder="e.g., Software Engineer"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          {/* Location */}
          <div>
            <label htmlFor="city" className="block text-sm font-medium text-gray-700 mb-1">
              City
            </label>
            <input
              type="text"
              id="city"
              value={filters.city || ''}
              onChange={(e) => handleInputChange('city', e.target.value)}
              placeholder="e.g., New York"
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          {/* Min Experience */}
          <div>
            <label htmlFor="minExperience" className="block text-sm font-medium text-gray-700 mb-1">
              Min Experience (years)
            </label>
            <input
              type="number"
              id="minExperience"
              min="0"
              value={filters.minTotalYearsExperience || ''}
              onChange={(e) => handleInputChange('minTotalYearsExperience', e.target.value ? parseInt(e.target.value) : undefined)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          {/* Max Experience */}
          <div>
            <label htmlFor="maxExperience" className="block text-sm font-medium text-gray-700 mb-1">
              Max Experience (years)
            </label>
            <input
              type="number"
              id="maxExperience"
              min="0"
              value={filters.maxTotalYearsExperience || ''}
              onChange={(e) => handleInputChange('maxTotalYearsExperience', e.target.value ? parseInt(e.target.value) : undefined)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          {/* Authorization Status */}
          <div>
            <label htmlFor="isAuthorized" className="block text-sm font-medium text-gray-700 mb-1">
              Work Authorization
            </label>
            <select
              id="isAuthorized"
              value={filters.isAuthorizedToWork === undefined ? '' : filters.isAuthorizedToWork ? 'true' : 'false'}
              onChange={(e) => {
                const value = e.target.value;
                handleInputChange('isAuthorizedToWork', value === '' ? undefined : value === 'true');
              }}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">All</option>
              <option value="true">Authorized</option>
              <option value="false">Not Authorized</option>
            </select>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex justify-between items-center">
          <div className="text-sm text-gray-600">
            {totalCount > 0 && `Found ${totalCount} candidates`}
          </div>
          <div className="flex space-x-3">
            <button
              type="button"
              onClick={clearFilters}
              className="px-4 py-2 text-gray-600 bg-gray-100 hover:bg-gray-200 rounded-md transition-colors"
              disabled={loading}
            >
              Clear Filters
            </button>
            <button
              type="submit"
              disabled={loading}
              className="px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center"
            >
              {loading ? (
                <>
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                  Searching...
                </>
              ) : (
                'Search Candidates'
              )}
            </button>
          </div>
        </div>
      </form>
    </div>
  );
};