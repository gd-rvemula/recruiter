import { useState, useEffect, useCallback } from 'react';
import { candidateApi } from '../services/candidateApi';
import {
  CandidateSearchDto,
  CandidateSearchRequest,
  CandidateSearchResponse,
  PaginationInfo,
} from '../types/candidate';

interface UseCandidatesResult {
  candidates: CandidateSearchDto[];
  loading: boolean;
  error: string | null;
  pagination: PaginationInfo;
  searchCandidates: (request: CandidateSearchRequest) => Promise<void>;
  refreshCandidates: () => Promise<void>;
  clearError: () => void;
}

const defaultSearchRequest: CandidateSearchRequest = {
  page: 1,
  pageSize: 20,
  skills: [],
  companies: [],
  jobTitles: [],
  sortDirection: 'asc',
};

export const useCandidates = (initialRequest?: Partial<CandidateSearchRequest>): UseCandidatesResult => {
  const [candidates, setCandidates] = useState<CandidateSearchDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pagination, setPagination] = useState<PaginationInfo>({
    page: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0,
    hasNextPage: false,
    hasPreviousPage: false,
  });
  const [currentRequest, setCurrentRequest] = useState<CandidateSearchRequest>({
    ...defaultSearchRequest,
    ...initialRequest,
  });

  const searchCandidates = useCallback(async (request: CandidateSearchRequest) => {
    setLoading(true);
    setError(null);
    
    try {
      // Get search mode from localStorage
      const savedSearchMode = localStorage.getItem('searchMode') as 'semantic' | 'nameMatch' | 'auto' | null;
      const searchMode = savedSearchMode || 'semantic'; // Default to semantic
      
      // Include search mode in the request
      const requestWithSearchMode = {
        ...request,
        searchMode
      };
      
      const response: CandidateSearchResponse = await candidateApi.searchCandidates(requestWithSearchMode);
      
      setCandidates(response.candidates);
      setPagination({
        page: response.page,
        pageSize: response.pageSize,
        totalCount: response.totalCount,
        totalPages: response.totalPages,
        hasNextPage: response.hasNextPage,
        hasPreviousPage: response.hasPreviousPage,
      });
      setCurrentRequest(requestWithSearchMode);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to fetch candidates';
      setError(errorMessage);
      setCandidates([]);
    } finally {
      setLoading(false);
    }
  }, []);

  const refreshCandidates = useCallback(async () => {
    await searchCandidates(currentRequest);
  }, [searchCandidates, currentRequest]);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  // Initial load
  useEffect(() => {
    searchCandidates(currentRequest);
  }, []); // Only run once on mount

  return {
    candidates,
    loading,
    error,
    pagination,
    searchCandidates,
    refreshCandidates,
    clearError,
  };
};

// Hook for managing search filters
interface UseSearchFiltersResult {
  filters: CandidateSearchRequest;
  updateFilter: <K extends keyof CandidateSearchRequest>(key: K, value: CandidateSearchRequest[K]) => void;
  resetFilters: () => void;
  addSkill: (skill: string) => void;
  removeSkill: (skill: string) => void;
  addCompany: (company: string) => void;
  removeCompany: (company: string) => void;
  addJobTitle: (jobTitle: string) => void;
  removeJobTitle: (jobTitle: string) => void;
}

export const useSearchFilters = (initialFilters?: Partial<CandidateSearchRequest>): UseSearchFiltersResult => {
  const [filters, setFilters] = useState<CandidateSearchRequest>({
    ...defaultSearchRequest,
    ...initialFilters,
  });

  const updateFilter = useCallback(<K extends keyof CandidateSearchRequest>(
    key: K,
    value: CandidateSearchRequest[K]
  ) => {
    setFilters(prev => ({ ...prev, [key]: value }));
  }, []);

  const resetFilters = useCallback(() => {
    setFilters({ ...defaultSearchRequest, ...initialFilters });
  }, [initialFilters]);

  const addSkill = useCallback((skill: string) => {
    setFilters(prev => ({
      ...prev,
      skills: [...prev.skills.filter(s => s !== skill), skill],
    }));
  }, []);

  const removeSkill = useCallback((skill: string) => {
    setFilters(prev => ({
      ...prev,
      skills: prev.skills.filter(s => s !== skill),
    }));
  }, []);

  const addCompany = useCallback((company: string) => {
    setFilters(prev => ({
      ...prev,
      companies: [...prev.companies.filter(c => c !== company), company],
    }));
  }, []);

  const removeCompany = useCallback((company: string) => {
    setFilters(prev => ({
      ...prev,
      companies: prev.companies.filter(c => c !== company),
    }));
  }, []);

  const addJobTitle = useCallback((jobTitle: string) => {
    setFilters(prev => ({
      ...prev,
      jobTitles: [...prev.jobTitles.filter(t => t !== jobTitle), jobTitle],
    }));
  }, []);

  const removeJobTitle = useCallback((jobTitle: string) => {
    setFilters(prev => ({
      ...prev,
      jobTitles: prev.jobTitles.filter(t => t !== jobTitle),
    }));
  }, []);

  return {
    filters,
    updateFilter,
    resetFilters,
    addSkill,
    removeSkill,
    addCompany,
    removeCompany,
    addJobTitle,
    removeJobTitle,
  };
};