// Enhanced search types for full-text search functionality

export interface AdvancedSearchRequest {
  // Full-text search query
  searchQuery?: string;
  
  // Traditional filters (existing)
  searchTerm?: string;
  currentTitle?: string;
  minTotalYearsExperience?: number;
  maxTotalYearsExperience?: number;
  isAuthorizedToWork?: boolean;
  needsSponsorship?: boolean;
  minSalaryExpectation?: number;
  maxSalaryExpectation?: number;
  isActive?: boolean;
  
  // Enhanced search options
  skills: string[];
  companies: string[];
  jobTitles: string[];
  
  // Search type and options
  searchType: SearchType;
  includeResumeContent: boolean;
  includeSkills: boolean;
  useFullTextSearch: boolean;
  useFuzzyMatching: boolean;
  
  // Pagination
  page: number;
  pageSize: number;
  
  // Sorting
  sortBy?: string;
  sortDirection: string;
}

export enum SearchType {
  Resume = 'Resume',
  Skills = 'Skills',
  All = 'All'
}

export interface AdvancedSearchResponse {
  candidates: CandidateSearchResultDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  
  // Search metadata
  searchQuery?: string;
  searchType: SearchType;
  searchDurationMs: number;
  searchSuggestions: string[];
}

export interface CandidateSearchResultDto extends CandidateSearchDto {
  // Full-text search specific fields
  searchRank: number;
  searchHeadline?: string;
  matchedTerms: string[];
  resumeSnippet?: string;
  
  // Additional computed fields
  resumeMatchCount: number;
  skillMatchCount: number;
  highlightedSkills: string[];
}

export interface FullTextSearchRequest {
  query: string;
  page: number;
  pageSize: number;
  includeRanking?: boolean;
}

export interface SearchSuggestionsRequest {
  term: string;
  searchType: SearchType;
  limit?: number;
}

export enum SearchSuggestionType {
  Skills = 'Skills',
  JobTitles = 'JobTitles',
  Companies = 'Companies',
  All = 'All'
}

export interface SearchSuggestionsResponse {
  suggestions: string[];
}

export interface SearchSuggestion {
  text: string;
  type: SearchSuggestionType;
  frequency: number;
  similarity: number;
}

export interface SearchIndexRequest {
  refreshMaterializedView: boolean;
  rebuildIndexes: boolean;
  candidateIds?: string[];
}

export interface SearchIndexResponse {
  success: boolean;
  message: string;
  processingTimeMs: number;
  processedCandidates: number;
}