import { useState, useCallback, useMemo } from 'react';
import { candidateApi } from '../services/candidateApi';
import {
  AdvancedSearchRequest,
  AdvancedSearchResponse,
  FullTextSearchRequest,
  SearchSuggestionsRequest,
  SearchSuggestionsResponse,
  SearchType,
  CandidateSearchResultDto
} from '../types/advancedSearch';

interface UseAdvancedSearchState {
  results: CandidateSearchResultDto[];
  totalCount: number;
  isLoading: boolean;
  error: string | null;
  hasSearched: boolean;
  suggestions: string[];
  isSuggestionsLoading: boolean;
}

interface UseAdvancedSearchReturn extends UseAdvancedSearchState {
  performAdvancedSearch: (request: AdvancedSearchRequest) => Promise<void>;
  performFullTextSearch: (query: string, page?: number, pageSize?: number) => Promise<void>;
  getSuggestions: (term: string, searchType?: SearchType) => Promise<void>;
  clearResults: () => void;
  resetError: () => void;
}

export const useAdvancedSearch = (): UseAdvancedSearchReturn => {
  const [state, setState] = useState<UseAdvancedSearchState>({
    results: [],
    totalCount: 0,
    isLoading: false,
    error: null,
    hasSearched: false,
    suggestions: [],
    isSuggestionsLoading: false,
  });

  const performAdvancedSearch = useCallback(async (request: AdvancedSearchRequest) => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));
    
    try {
      const response: AdvancedSearchResponse = await candidateApi.advancedSearch(request);
      setState(prev => ({
        ...prev,
        results: response.candidates,
        totalCount: response.totalCount,
        isLoading: false,
        hasSearched: true,
      }));
    } catch (error) {
      setState(prev => ({
        ...prev,
        isLoading: false,
        error: error instanceof Error ? error.message : 'An error occurred during search',
        hasSearched: true,
      }));
    }
  }, []);

  const performFullTextSearch = useCallback(async (
    query: string, 
    page: number = 1, 
    pageSize: number = 10
  ) => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));
    
    try {
      const request: FullTextSearchRequest = {
        query,
        page,
        pageSize,
        includeRanking: true,
      };

      const response: AdvancedSearchResponse = await candidateApi.fullTextSearch(request);
      setState(prev => ({
        ...prev,
        results: response.candidates,
        totalCount: response.totalCount,
        isLoading: false,
        hasSearched: true,
      }));
    } catch (error) {
      setState(prev => ({
        ...prev,
        isLoading: false,
        error: error instanceof Error ? error.message : 'An error occurred during search',
        hasSearched: true,
      }));
    }
  }, []);

  const getSuggestions = useCallback(async (
    term: string, 
    searchType: SearchType = SearchType.Skills
  ) => {
    if (!term.trim()) {
      setState(prev => ({ ...prev, suggestions: [] }));
      return;
    }

    setState(prev => ({ ...prev, isSuggestionsLoading: true }));
    
    try {
      const request: SearchSuggestionsRequest = {
        term,
        searchType,
        limit: 10,
      };

      const response: SearchSuggestionsResponse = await candidateApi.getSearchSuggestions(request);
      setState(prev => ({
        ...prev,
        suggestions: response.suggestions,
        isSuggestionsLoading: false,
      }));
    } catch (error) {
      setState(prev => ({
        ...prev,
        suggestions: [],
        isSuggestionsLoading: false,
      }));
      console.error('Error fetching suggestions:', error);
    }
  }, []);

  const clearResults = useCallback(() => {
    setState(prev => ({
      ...prev,
      results: [],
      totalCount: 0,
      hasSearched: false,
      error: null,
      suggestions: [],
    }));
  }, []);

  const resetError = useCallback(() => {
    setState(prev => ({ ...prev, error: null }));
  }, []);

  // Memoized return object to prevent unnecessary re-renders
  return useMemo(() => ({
    ...state,
    performAdvancedSearch,
    performFullTextSearch,
    getSuggestions,
    clearResults,
    resetError,
  }), [state, performAdvancedSearch, performFullTextSearch, getSuggestions, clearResults, resetError]);
};

// Helper hook for quick full-text search with debouncing
export const useQuickSearch = () => {
  const { performFullTextSearch, ...searchState } = useAdvancedSearch();
  const [searchTerm, setSearchTerm] = useState('');

  // Debounced search effect would go here if needed
  // For now, keeping it simple without debouncing dependencies

  const quickSearch = useCallback((query: string) => {
    setSearchTerm(query);
    if (query.trim()) {
      performFullTextSearch(query, 1, 20);
    }
  }, [performFullTextSearch]);

  return {
    ...searchState,
    searchTerm,
    quickSearch,
    clearSearch: () => {
      setSearchTerm('');
      searchState.clearResults();
    },
  };
};