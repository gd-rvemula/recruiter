// TypeScript interfaces matching the backend DTOs

export interface CandidateSearchDto {
  id: string;
  candidateCode: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  currentTitle?: string;
  requisitionName?: string;
  totalYearsExperience?: number;
  salaryExpectation?: number;
  isAuthorizedToWork: boolean;
  needsSponsorship: boolean;
  isActive: boolean;
  currentStatus: string;
  primarySkills: string[];
  similarityScore?: number; // Similarity score from semantic/hybrid search (0-1)
}

export interface CandidateDto {
  id: string;
  candidateCode: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phone?: string;
  address?: string;
  city?: string;
  state?: string;
  country?: string;
  currentTitle?: string;
  totalYearsExperience?: number;
  salaryExpectation?: number;
  isAuthorizedToWork: boolean;
  needsSponsorship: boolean;
  createdAt: string;
  updatedAt: string;
  createdBy?: string;
  isActive: boolean;
  workExperiences: WorkExperienceDto[];
  education: EducationDto[];
  skills: CandidateSkillDto[];
  resumes: ResumeDto[];
}

export interface CandidateSearchRequest {
  searchTerm?: string;
  city?: string;
  state?: string;
  country?: string;
  currentTitle?: string;
  minTotalYearsExperience?: number;
  maxTotalYearsExperience?: number;
  isAuthorizedToWork?: boolean;
  needsSponsorship?: boolean;
  sponsorshipFilter?: 'all' | 'yes' | 'no';
  minSalaryExpectation?: number;
  maxSalaryExpectation?: number;
  isActive?: boolean;
  skills: string[];
  companies: string[];
  jobTitles: string[];
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface CandidateSearchResponse {
  candidates: CandidateSearchDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CandidateSkillDto {
  id: string;
  skillName: string;
  category?: string;
  proficiencyLevel?: string;
  yearsOfExperience?: number;
  isExtracted: boolean;
}

export interface WorkExperienceDto {
  id: string;
  companyName: string;
  jobTitle: string;
  startDate?: string;
  endDate?: string;
  isCurrent: boolean;
  location?: string;
  description?: string;
  extractedOrder?: number;
}

export interface EducationDto {
  id: string;
  institutionName?: string;
  degreeName?: string;
  degreeType?: string;
  fieldOfStudy?: string;
  startDate?: string;
  endDate?: string;
  gpa?: number;
  location?: string;
}

export interface ResumeDto {
  id: string;
  fileName?: string;
  fileType?: string;
  fileSize?: number;
  filePath?: string;
  isProcessed: boolean;
  processingStatus: string;
  uploadedAt: string;
  processedAt?: string;
}

// Additional utility types
export interface PaginationInfo {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ApiResponse<T> {
  data?: T;
  success: boolean;
  message?: string;
  errors?: string[];
}

// Status-related interfaces
export interface CandidateStatusHistory {
  id: string;
  candidateId: string;
  previousStatus?: string;
  newStatus: string;
  changedBy?: string;
  changeReason?: string;
  createdAt: string;
}

export interface CandidateStatusUpdate {
  newStatus: string;
  changeReason?: string;
  changedBy?: string;
}

export interface CandidateStatusResponse {
  currentStatus: string;
  statusUpdatedAt: string;
  statusUpdatedBy?: string;
  availableStatuses: string[];
}

export const CandidateStatuses = {
  New: 'New',
  Review: 'Review',
  Screening: 'Screening',
  Interviewing: 'Interviewing',
  Accepted: 'Accepted',
  Rejected: 'Rejected',
  Withdrawn: 'Withdrawn',
  OnHold: 'OnHold'
} as const;

export type CandidateStatusType = typeof CandidateStatuses[keyof typeof CandidateStatuses];

export const getStatusColor = (status: string): string => {
  switch (status.toLowerCase()) {
    case 'new':
      return 'bg-gray-100 text-gray-800';
    case 'review':
      return 'bg-blue-100 text-blue-800';
    case 'screening':
      return 'bg-yellow-100 text-yellow-800';
    case 'interviewing':
      return 'bg-purple-100 text-purple-800';
    case 'accepted':
      return 'bg-green-100 text-green-800';
    case 'rejected':
      return 'bg-red-100 text-red-800';
    case 'withdrawn':
      return 'bg-orange-100 text-orange-800';
    case 'onhold':
      return 'bg-gray-100 text-gray-600';
    default:
      return 'bg-gray-100 text-gray-800';
  }
};

// Skills interfaces
export interface SkillFrequency {
  text: string;
  value: number;
}