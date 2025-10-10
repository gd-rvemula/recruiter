import React, { useState } from 'react';
import Layout from '../components/common/Layout';
import { useCandidates } from '../hooks/useCandidates';
import { CandidateSearchDto } from '../types/candidate';
import { candidateApi } from '../services/candidateApi';

const CandidatesPage: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCandidate, setSelectedCandidate] = useState<CandidateSearchDto | null>(null);
  const [selectedCandidateDetails, setSelectedCandidateDetails] = useState<any>(null);
  const [isLoadingDetails, setIsLoadingDetails] = useState(false);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [allCandidates, setAllCandidates] = useState<CandidateSearchDto[]>([]);
  
  const { candidates, loading, error, pagination, searchCandidates, clearError } = useCandidates();

  // Update allCandidates when candidates change
  React.useEffect(() => {
    if (pagination.page === 1) {
      setAllCandidates(candidates);
    } else {
      setAllCandidates(prev => [...prev, ...candidates]);
    }
  }, [candidates, pagination.page]);

  const handleSearch = async () => {
    const searchCriteria = {
      searchTerm: searchTerm || '',
      skills: [],
      companies: [],
      jobTitles: [],
      page: 1,
      pageSize: 20
    };

    try {
      await searchCandidates(searchCriteria);
      setAllCandidates([]);
    } catch (err) {
      console.error('Search failed:', err);
    }
  };

  const loadMoreResults = async () => {
    if (!pagination.hasNextPage || loading) return;
    
    setIsLoadingMore(true);
    await searchCandidates({
      searchTerm: searchTerm || undefined,
      skills: [],
      companies: [],
      jobTitles: [],
      page: pagination.page + 1,
      pageSize: 20,
      sortDirection: 'asc'
    });
    setIsLoadingMore(false);
  };

  const clearResults = () => {
    setSearchTerm('');
    setAllCandidates([]);
    setSelectedCandidate(null);
    setSelectedCandidateDetails(null);
    clearError();
  };

  const handleCandidateClick = async (candidate: CandidateSearchDto) => {
    setSelectedCandidate(candidate);
    setIsLoadingDetails(true);
    try {
      const details = await candidateApi.getCandidateById(candidate.id);
      setSelectedCandidateDetails(details);
    } catch (error) {
      console.error('Error loading candidate details:', error);
    } finally {
      setIsLoadingDetails(false);
    }
  };

  const handleCloseCandidateDetails = () => {
    setSelectedCandidate(null);
    setSelectedCandidateDetails(null);
  };

  return (
    <Layout>
      <div>
        {/* Header with inline search */}
        <div className="mb-8">
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
            <div>
              <h1 className="text-3xl font-bold text-gray-900">Candidates</h1>
              <p className="mt-2 text-gray-600">Search and manage candidate profiles</p>
            </div>

            {/* Inline search bar */}
            <div className="flex flex-col sm:flex-row gap-3 min-w-0 md:w-auto">
              <div className="flex-1 min-w-64">
                <input
                  type="text"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
                  placeholder="Search candidates by name, skills, title..."
                  className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
              <div className="flex gap-2">
                <button
                  onClick={handleSearch}
                  disabled={loading}
                  className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 whitespace-nowrap"
                >
                  {loading ? 'Searching...' : 'Search'}
                </button>
                <button
                  onClick={clearResults}
                  className="px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700 whitespace-nowrap"
                >
                  Clear
                </button>
              </div>
            </div>
          </div>
        </div>

        {/* Error Display */}
        {error && (
          <div className="mb-4 p-4 bg-red-100 border border-red-400 text-red-700 rounded">
            <p>Error: {error}</p>
            <button 
              onClick={clearError}
              className="mt-2 text-sm underline hover:no-underline"
            >
              Dismiss
            </button>
          </div>
        )}

        {/* Results */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Candidates List */}
          <div className="space-y-4">
            {allCandidates.length > 0 && (
              <div className="mb-4 text-sm text-gray-600">
                Found {pagination.totalCount} candidates
              </div>
            )}
            
            {allCandidates.map((candidate) => (
              <div
                key={candidate.id}
                onClick={() => handleCandidateClick(candidate)}
                className={`p-4 border rounded-lg cursor-pointer transition-colors ${
                  selectedCandidate?.id === candidate.id
                    ? 'border-blue-500 bg-blue-50'
                    : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
                }`}
              >
                <div className="flex justify-between items-start mb-2">
                  <h3 className="font-semibold text-lg text-gray-900">
                    {candidate.fullName}
                  </h3>
                  <span className="text-sm text-gray-500">
                    {candidate.candidateCode}
                  </span>
                </div>
                
                {candidate.currentTitle && (
                  <p className="text-gray-700 mb-2">{candidate.currentTitle}</p>
                )}
                
                <div className="flex flex-wrap gap-4 text-sm text-gray-600">
                  {candidate.totalYearsExperience && (
                    <span>{candidate.totalYearsExperience} years exp.</span>
                  )}
                  {candidate.salaryExpectation && (
                    <span>${candidate.salaryExpectation.toLocaleString()}</span>
                  )}
                  <span className={candidate.isAuthorizedToWork ? 'text-green-600' : 'text-red-600'}>
                    {candidate.isAuthorizedToWork ? 'Authorized' : 'Not Authorized'}
                  </span>
                </div>
                
                {candidate.primarySkills && candidate.primarySkills.length > 0 && (
                  <div className="mt-3">
                    <div className="flex flex-wrap gap-1">
                      {candidate.primarySkills.slice(0, 5).map((skill, index) => (
                        <span
                          key={index}
                          className="px-2 py-1 text-xs bg-blue-100 text-blue-800 rounded"
                        >
                          {skill}
                        </span>
                      ))}
                      {candidate.primarySkills.length > 5 && (
                        <span className="px-2 py-1 text-xs bg-gray-100 text-gray-600 rounded">
                          +{candidate.primarySkills.length - 5} more
                        </span>
                      )}
                    </div>
                  </div>
                )}
              </div>
            ))}

            {/* Load More Button */}
            {pagination.hasNextPage && (
              <div className="text-center">
                <button
                  onClick={loadMoreResults}
                  disabled={isLoadingMore}
                  className="px-6 py-2 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 disabled:opacity-50"
                >
                  {isLoadingMore ? 'Loading...' : 'Load More'}
                </button>
              </div>
            )}

            {allCandidates.length === 0 && !loading && (
              <div className="text-center py-8 text-gray-500">
                No candidates found. Try searching to see results.
              </div>
            )}
          </div>

          {/* Candidate Details */}
          <div className="sticky top-4">
            {selectedCandidate ? (
              <div className="border rounded-lg p-6 bg-white">
                <div className="flex justify-between items-start mb-4">
                  <h2 className="text-xl font-bold text-gray-900">
                    Candidate Details
                  </h2>
                  <button
                    onClick={handleCloseCandidateDetails}
                    className="text-gray-400 hover:text-gray-600"
                  >
                    ✕
                  </button>
                </div>

                {isLoadingDetails ? (
                  <div className="text-center py-8">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
                    <p className="mt-2 text-gray-600">Loading details...</p>
                  </div>
                ) : selectedCandidateDetails ? (
                  <div className="space-y-6">
                    {/* Basic Info */}
                    <div>
                      <h3 className="font-semibold text-gray-900 mb-2">Basic Information</h3>
                      <div className="grid grid-cols-2 gap-4 text-sm">
                        <div>
                          <span className="text-gray-600">Full Name:</span>
                          <p className="font-medium">{selectedCandidateDetails.fullName}</p>
                        </div>
                        <div>
                          <span className="text-gray-600">Code:</span>
                          <p className="font-medium">{selectedCandidateDetails.candidateCode}</p>
                        </div>
                        <div>
                          <span className="text-gray-600">Email:</span>
                          <p className="font-medium">{selectedCandidateDetails.email}</p>
                        </div>
                        <div>
                          <span className="text-gray-600">Phone:</span>
                          <p className="font-medium">{selectedCandidateDetails.phone || 'N/A'}</p>
                        </div>
                      </div>
                    </div>

                    {/* Current Title */}
                    {selectedCandidateDetails.currentTitle && (
                      <div>
                        <h3 className="font-semibold text-gray-900 mb-2">Current Position</h3>
                        <p className="text-gray-700">{selectedCandidateDetails.currentTitle}</p>
                      </div>
                    )}

                    {/* Skills */}
                    {selectedCandidateDetails.skills && selectedCandidateDetails.skills.length > 0 && (
                      <div>
                        <h3 className="font-semibold text-gray-900 mb-2">Skills</h3>
                        <div className="flex flex-wrap gap-2">
                          {selectedCandidateDetails.skills.map((skill: any) => (
                            <span
                              key={skill.id}
                              className="px-3 py-1 text-sm bg-blue-100 text-blue-800 rounded"
                            >
                              {skill.skillName}
                              {skill.yearsOfExperience && (
                                <span className="ml-1 text-blue-600">
                                  ({skill.yearsOfExperience}y)
                                </span>
                              )}
                            </span>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Work Experience */}
                    {selectedCandidateDetails.workExperiences && selectedCandidateDetails.workExperiences.length > 0 && (
                      <div>
                        <h3 className="font-semibold text-gray-900 mb-2">Work Experience</h3>
                        <div className="space-y-3">
                          {selectedCandidateDetails.workExperiences.map((exp: any) => (
                            <div key={exp.id} className="border-l-2 border-gray-200 pl-4">
                              <div className="flex justify-between items-start">
                                <div>
                                  <h4 className="font-medium text-gray-900">{exp.jobTitle}</h4>
                                  <p className="text-gray-700">{exp.companyName}</p>
                                </div>
                                <span className="text-sm text-gray-500">
                                  {exp.startDate ? new Date(exp.startDate).getFullYear() : ''} - 
                                  {exp.isCurrent ? 'Present' : (exp.endDate ? new Date(exp.endDate).getFullYear() : '')}
                                </span>
                              </div>
                              {exp.description && (
                                <p className="mt-1 text-sm text-gray-600">{exp.description}</p>
                              )}
                            </div>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Education */}
                    {selectedCandidateDetails.education && selectedCandidateDetails.education.length > 0 && (
                      <div>
                        <h3 className="font-semibold text-gray-900 mb-2">Education</h3>
                        <div className="space-y-2">
                          {selectedCandidateDetails.education.map((edu: any) => (
                            <div key={edu.id}>
                              <div className="flex justify-between items-start">
                                <div>
                                  <h4 className="font-medium text-gray-900">
                                    {edu.degreeName} {edu.fieldOfStudy && `in ${edu.fieldOfStudy}`}
                                  </h4>
                                  <p className="text-gray-700">{edu.institutionName}</p>
                                </div>
                                <span className="text-sm text-gray-500">
                                  {edu.startDate ? new Date(edu.startDate).getFullYear() : ''} - 
                                  {edu.endDate ? new Date(edu.endDate).getFullYear() : ''}
                                </span>
                              </div>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                ) : (
                  <div className="text-center py-8 text-gray-500">
                    Failed to load candidate details.
                  </div>
                )}
              </div>
            ) : (
              <div className="border rounded-lg p-6 bg-gray-50 text-center text-gray-500">
                Select a candidate to view details
              </div>
            )}
          </div>
        </div>
      </div>
    </Layout>
  );
};

export default CandidatesPage;