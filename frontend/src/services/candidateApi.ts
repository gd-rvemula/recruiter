import { 
  CandidateSearchRequest, 
  CandidateSearchResponse, 
  CandidateDto,
  CandidateStatusHistory,
  CandidateStatusUpdate,
  CandidateStatusResponse,
  SkillFrequency
} from '../types/candidate';
import {
  AdvancedSearchRequest,
  AdvancedSearchResponse,
  FullTextSearchRequest,
  SearchSuggestionsRequest,
  SearchSuggestionsResponse,
  SearchIndexRequest,
  SearchIndexResponse
} from '../types/advancedSearch';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

class ApiError extends Error {
  constructor(message: string, public status?: number) {
    super(message);
    this.name = 'ApiError';
  }
}

async function fetchAPI<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const url = `${API_BASE_URL}${endpoint}`;
  
  try {
    const response = await fetch(url, {
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
      ...options,
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new ApiError(
        errorData.message || `API request failed: ${response.status} ${response.statusText}`,
        response.status
      );
    }

    return await response.json();
  } catch (error) {
    console.error(`API Error: ${endpoint}`, error);
    if (error instanceof ApiError) {
      throw error;
    }
    throw new ApiError(`Network error: ${error instanceof Error ? error.message : 'Unknown error'}`);
  }
}

// Candidate API functions
export const candidateApi = {
  // Search candidates with pagination and filters
  searchCandidates: async (request: CandidateSearchRequest): Promise<CandidateSearchResponse> => {
    return fetchAPI<CandidateSearchResponse>('/api/candidates/search', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  // Get candidates using query parameters (alternative to search)
  getCandidates: async (params: CandidateSearchRequest): Promise<CandidateSearchResponse> => {
    const queryParams = new URLSearchParams();
    
    if (params.searchTerm) queryParams.append('searchTerm', params.searchTerm);
    if (params.city) queryParams.append('city', params.city);
    if (params.state) queryParams.append('state', params.state);
    if (params.country) queryParams.append('country', params.country);
    if (params.currentTitle) queryParams.append('currentTitle', params.currentTitle);
    if (params.minTotalYearsExperience !== undefined) queryParams.append('minTotalYearsExperience', params.minTotalYearsExperience.toString());
    if (params.maxTotalYearsExperience !== undefined) queryParams.append('maxTotalYearsExperience', params.maxTotalYearsExperience.toString());
    if (params.isAuthorizedToWork !== undefined) queryParams.append('isAuthorizedToWork', params.isAuthorizedToWork.toString());
    if (params.needsSponsorship !== undefined) queryParams.append('needsSponsorship', params.needsSponsorship.toString());
    if (params.minSalaryExpectation !== undefined) queryParams.append('minSalaryExpectation', params.minSalaryExpectation.toString());
    if (params.maxSalaryExpectation !== undefined) queryParams.append('maxSalaryExpectation', params.maxSalaryExpectation.toString());
    if (params.isActive !== undefined) queryParams.append('isActive', params.isActive.toString());
    
    queryParams.append('page', params.page.toString());
    queryParams.append('pageSize', params.pageSize.toString());
    
    if (params.sortBy) queryParams.append('sortBy', params.sortBy);
    if (params.sortDirection) queryParams.append('sortDirection', params.sortDirection);

    // Handle array parameters
    params.skills.forEach(skill => queryParams.append('skills', skill));
    params.companies.forEach(company => queryParams.append('companies', company));
    params.jobTitles.forEach(title => queryParams.append('jobTitles', title));

    return fetchAPI<CandidateSearchResponse>(`/api/candidates?${queryParams.toString()}`);
  },

  // Get single candidate by ID (placeholder - endpoint might not exist yet)
  getCandidateById: async (id: string): Promise<CandidateDto> => {
    return fetchAPI<CandidateDto>(`/api/candidates/${id}`);
  },

  // Advanced search with full-text search capabilities
  advancedSearch: async (request: AdvancedSearchRequest): Promise<AdvancedSearchResponse> => {
    return fetchAPI<AdvancedSearchResponse>('/api/candidates/advanced-search', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  // Full-text search only
  fullTextSearch: async (request: FullTextSearchRequest): Promise<AdvancedSearchResponse> => {
    return fetchAPI<AdvancedSearchResponse>('/api/candidates/fulltext-search', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  // Get search suggestions for autocomplete
  getSearchSuggestions: async (request: SearchSuggestionsRequest): Promise<SearchSuggestionsResponse> => {
    return fetchAPI<SearchSuggestionsResponse>('/api/candidates/search-suggestions', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  // Refresh search index (admin function)
  refreshSearchIndex: async (request: SearchIndexRequest): Promise<SearchIndexResponse> => {
    return fetchAPI<SearchIndexResponse>('/api/candidates/refresh-search-index', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  // Status management
  getCandidateStatus: async (candidateId: string): Promise<CandidateStatusResponse> => {
    return fetchAPI<CandidateStatusResponse>(`/api/candidates/${candidateId}/status`);
  },

  updateCandidateStatus: async (candidateId: string, statusUpdate: CandidateStatusUpdate): Promise<CandidateStatusResponse> => {
    return fetchAPI<CandidateStatusResponse>(`/api/candidates/${candidateId}/status`, {
      method: 'PUT',
      body: JSON.stringify(statusUpdate),
    });
  },

  getCandidateStatusHistory: async (candidateId: string): Promise<CandidateStatusHistory[]> => {
    return fetchAPI<CandidateStatusHistory[]>(`/api/candidates/${candidateId}/status/history`);
  },

  getStatusTotals: async (): Promise<Record<string, number>> => {
    return fetchAPI<Record<string, number>>(`/api/candidates/status/totals`);
  },

  getSkillsFrequency: async (): Promise<SkillFrequency[]> => {
    return fetchAPI<SkillFrequency[]>(`/api/candidates/skills/frequency`);
  },

  getSystemStatistics: async (): Promise<{ totalCandidates: number; withEmbeddings: number; coveragePercent: number }> => {
    return fetchAPI<{ totalCandidates: number; withEmbeddings: number; coveragePercent: number }>(`/api/candidates/statistics`);
  },

  // Upload Excel file with candidates
  uploadExcel: async (file: File): Promise<{
    success: boolean;
    message: string;
    processedRows: number;
    errorCount: number;
    importedCandidates: number;
    errors: string[];
  }> => {
    const formData = new FormData();
    formData.append('file', file);

    const url = `${API_BASE_URL}/api/excelimport/upload`;
    const response = await fetch(url, {
      method: 'POST',
      body: formData,
      // Don't set Content-Type header - browser will set it with boundary for multipart/form-data
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new ApiError(
        errorData.message || `Upload failed: ${response.status} ${response.statusText}`,
        response.status
      );
    }

    return await response.json();
  },

  // Generate embeddings for candidates missing them
  generateMissingEmbeddings: async (): Promise<{
    message: string;
    candidatesQueued: number;
    estimatedTimeMinutes: number;
  }> => {
    return fetchAPI<{
      message: string;
      candidatesQueued: number;
      estimatedTimeMinutes: number;
    }>('/api/embeddings/generate-missing', {
      method: 'POST',
    });
  },

  // Explain why a candidate matched a search query
  explainMatch: async (request: {
    candidateId: string;
    searchQuery: string;
    similarityScore?: number;
  }): Promise<{
    candidateId: string;
    similarityScore: number;
    semanticScore?: number;
    keywordScore?: number;
    matchedSnippets: Array<{
      source: string;
      text: string;
      relevance: number;
      highlightedTerms: string[];
    }>;
    matchedKeywords: string[];
    explanation: string;
  }> => {
    return fetchAPI('/api/candidates/explain-match', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  // Generate AI summary for resume
  generateAISummary: async (resumeText: string, candidateId: string, resumeId: string): Promise<{ summary: string }> => {
    return fetchAPI('/api/ai-summary', {
      method: 'POST',
      body: JSON.stringify({ resumeText, candidateId, resumeId }),
    });
  },
};

// General utility functions
export const api = {
  // Test backend connectivity
  healthCheck: async (): Promise<{ status: string }> => {
    try {
      const response = await fetch(`${API_BASE_URL}/health`);
      if (response.ok) {
        const text = await response.text();
        return { status: text || 'Healthy' };
      }
      return { status: 'Unhealthy' };
    } catch {
      return { status: 'Unreachable' };
    }
  },
};

export { ApiError };