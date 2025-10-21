import React, { useState, useEffect } from 'react';
import Layout from '../components/common/Layout';
import StatusUpdateModal from '../components/common/StatusUpdateModal';
import { useCandidates } from '../hooks/useCandidates';
import { CandidateSearchDto, CandidateStatusUpdate, getStatusColor, CandidateStatuses } from '../types/candidate';
import { candidateApi } from '../services/candidateApi';
import { clientConfigApi, PrivacyConfigDto } from '../services/clientConfigApi';

const CandidatesPage: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [sponsorshipFilter, setSponsorshipFilter] = useState<'all' | 'yes' | 'no'>('all');
  const [selectedCandidate, setSelectedCandidate] = useState<CandidateSearchDto | null>(null);
  const [privacyConfig, setPrivacyConfig] = useState<PrivacyConfigDto | null>(null);
  const [selectedCandidateDetails, setSelectedCandidateDetails] = useState<any>(null);
  const [isLoadingDetails, setIsLoadingDetails] = useState(false);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [allCandidates, setAllCandidates] = useState<CandidateSearchDto[]>([]);
  const [isFullScreenModal, setIsFullScreenModal] = useState(false);
  const [isStatusModalOpen, setIsStatusModalOpen] = useState(false);
  const [selectedCandidateForStatus, setSelectedCandidateForStatus] = useState<CandidateSearchDto | null>(null);
  const [showMatchExplanation, setShowMatchExplanation] = useState(false);
  const [matchExplanation, setMatchExplanation] = useState<any>(null);
  const [loadingExplanation, setLoadingExplanation] = useState(false);
  const [showAISummary, setShowAISummary] = useState(false);
  const [aiSummary, setAiSummary] = useState<string>('');
  const [loadingAISummary, setLoadingAISummary] = useState(false);
  
  const { candidates, loading, error, pagination, searchCandidates, clearError } = useCandidates();

  useEffect(() => {
    const fetchPrivacyConfig = async () => {
      try {
        const config = await clientConfigApi.getPrivacyConfig();
        setPrivacyConfig(config);
      } catch (error) {
        console.error('Failed to fetch privacy configuration:', error);
      }
    };

    fetchPrivacyConfig();
  }, []);

  // Function to get candidate display text based on privacy settings (for list view)
  const getCandidateDisplayText = (candidate: CandidateSearchDto): string => {
    // If PII sanitization is enabled or privacy config is not loaded yet, show candidate code (last 6 digits)
    if (!privacyConfig || privacyConfig.piiSanitizationEnabled) {
      return candidate.candidateCode?.slice(-6) || 'Unknown';
    }
    // If PII sanitization is disabled, show cleaned full name
    return cleanCandidateName(candidate.fullName || `${candidate.firstName} ${candidate.lastName}`.trim());
  };

  // Function to get candidate detail display text (for detail view)
  const getCandidateDetailDisplayText = (candidate: CandidateSearchDto): string => {
    // If PII sanitization is enabled or privacy config is not loaded yet, show full candidate code
    if (!privacyConfig || privacyConfig.piiSanitizationEnabled) {
      return candidate.candidateCode || 'Unknown';
    }
    // If PII sanitization is disabled, show cleaned full name
    return cleanCandidateName(candidate.fullName || `${candidate.firstName} ${candidate.lastName}`.trim());
  };

  // Utility function to clean up candidate names
  const cleanCandidateName = (fullName: string): string => {
    if (!fullName) return 'Unnamed Candidate';
    
    // Remove 'Unknown' from the name
    let cleanName = fullName.replace(/\s*Unknown\s*/gi, '').trim();
    
    // Handle empty result after removing 'Unknown'
    if (!cleanName) return 'Unnamed Candidate';
    
    // Split concatenated names (e.g., "Briansanders" -> "Brian Sanders")
    // Look for lowercase followed by uppercase (camelCase pattern)
    cleanName = cleanName.replace(/([a-z])([A-Z])/g, '$1 $2');
    
    // Capitalize first letter of each word
    cleanName = cleanName.split(' ')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
      .join(' ');
    
    return cleanName;
  };

  // Utility function to highlight search terms in text
  const highlightSearchTerms = (text: string, searchTerms: string): React.ReactNode => {
    if (!text || !searchTerms.trim()) return text;
    
    // Split search terms by spaces and filter out empty strings
    const terms = searchTerms.trim().split(/\s+/).filter(term => term.length > 1);
    if (terms.length === 0) return text;
    
    // Create a regex pattern that matches any of the search terms (case-insensitive)
    const pattern = new RegExp(`(${terms.map(term => term.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('|')})`, 'gi');
    
    // Split text by the pattern and create highlighted spans
    const parts = text.split(pattern);
    
    return parts.map((part, index) => {
      // Check if this part matches any search term
      const isMatch = terms.some(term => part.toLowerCase() === term.toLowerCase());
      return isMatch ? (
        <span key={index} className="bg-yellow-200 text-yellow-900 px-1 rounded">
          {part}
        </span>
      ) : (
        part
      );
    });
  };

  // Special component for highlighting resume text while preserving formatting
  const HighlightedResumeText: React.FC<{ text: string, searchTerms: string }> = ({ text, searchTerms }) => {
    if (!searchTerms.trim()) {
      return <pre className="whitespace-pre-wrap text-sm text-gray-800 leading-tight">{text}</pre>;
    }

    const highlightedText = highlightSearchTerms(text, searchTerms);
    return (
      <pre className="whitespace-pre-wrap text-sm text-gray-800 leading-tight">
        {highlightedText}
      </pre>
    );
  };

    const handleSearch = async () => {
    const searchCriteria = {
      searchTerm: searchTerm || '',
      sponsorshipFilter: sponsorshipFilter !== 'all' ? sponsorshipFilter : undefined,
      skills: [],
      companies: [],
      jobTitles: [],
      page: 1,
      pageSize: 20
    };

    try {
      await searchCandidates(searchCriteria);
      // allCandidates will be updated by the useEffect that watches candidates and pagination.page
      // No need to manually clear here
    } catch (err) {
      console.error('Search failed:', err);
    }
  };

  // Update allCandidates when candidates change
  React.useEffect(() => {
    if (pagination.page === 1) {
      setAllCandidates(candidates);
    } else {
      setAllCandidates(prev => [...prev, ...candidates]);
    }
  }, [candidates, pagination.page]);

  // Auto-select first candidate when results load
  React.useEffect(() => {
    if (allCandidates.length > 0 && !selectedCandidate) {
      handleCandidateClick(allCandidates[0]);
    }
  }, [allCandidates]);

  // Debug: Log pagination changes
  React.useEffect(() => {
    console.log('Pagination updated:', {
      page: pagination.page,
      totalPages: pagination.totalPages,
      pageSize: pagination.pageSize,
      totalCount: pagination.totalCount,
      hasNextPage: pagination.hasNextPage,
      hasPreviousPage: pagination.hasPreviousPage,
      candidatesLoaded: allCandidates.length
    });
  }, [pagination, allCandidates.length]);

  // Debug: Log AI summary state changes
  React.useEffect(() => {
    console.log('AI Summary state changed:', {
      showAISummary,
      loadingAISummary,
      aiSummaryLength: aiSummary?.length || 0,
      aiSummaryPreview: aiSummary?.substring(0, 50) || 'empty'
    });
  }, [showAISummary, loadingAISummary, aiSummary]);

  const loadMoreResults = React.useCallback(async () => {
    if (!pagination.hasNextPage || loading || isLoadingMore) {
      console.log('Skipping load more:', { hasNextPage: pagination.hasNextPage, loading, isLoadingMore });
      return;
    }
    
    console.log('Loading more results, current page:', pagination.page);
    setIsLoadingMore(true);
    try {
      await searchCandidates({
        searchTerm: searchTerm || undefined,
        sponsorshipFilter: sponsorshipFilter !== 'all' ? sponsorshipFilter : undefined,
        skills: [],
        companies: [],
        jobTitles: [],
        page: pagination.page + 1,
        pageSize: 20,
        sortDirection: 'asc'
      });
    } finally {
      setIsLoadingMore(false);
    }
  }, [pagination.hasNextPage, pagination.page, loading, isLoadingMore, searchCandidates, searchTerm, sponsorshipFilter]);

  // Infinite scroll handler
  const handleScroll = React.useCallback((e: React.UIEvent<HTMLDivElement>) => {
    const { scrollTop, scrollHeight, clientHeight } = e.currentTarget;
    const isNearBottom = scrollHeight - scrollTop <= clientHeight + 100; // 100px threshold
    
    console.log('Scroll event:', { scrollTop, scrollHeight, clientHeight, isNearBottom });
    
    if (isNearBottom && pagination.hasNextPage && !loading && !isLoadingMore) {
      console.log('Triggering load more');
      loadMoreResults();
    }
  }, [pagination.hasNextPage, loading, isLoadingMore, loadMoreResults]);

  const clearResults = () => {
    setSearchTerm('');
    setSponsorshipFilter('all');
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
      console.error('Error fetching candidate details:', error);
      setSelectedCandidateDetails(null);
    } finally {
      setIsLoadingDetails(false);
    }
  };

  // Handler for opening status update modal from details panel
  const handleStatusUpdate = () => {
    if (selectedCandidate) {
      setSelectedCandidateForStatus(selectedCandidate);
      setIsStatusModalOpen(true);
    }
  };

  // Handler for AI Summary
  const handleAISummarize = async () => {
    if (!selectedCandidateDetails?.resumes || selectedCandidateDetails.resumes.length === 0) {
      alert('No resume available for this candidate.');
      return;
    }

    console.log('Starting AI summarization...');
    setLoadingAISummary(true);
    setShowAISummary(true);
    setAiSummary('');

    try {
      const resume = selectedCandidateDetails.resumes[0];
      const resumeText = resume.resumeText || '';
      const resumeId = resume.id;
      const candidateId = selectedCandidateDetails.id;
      
      console.log('Starting AI summarization...', { candidateId, resumeId, resumeTextLength: resumeText.length });
      
      const result = await candidateApi.generateAISummary(resumeText, candidateId, resumeId);
      console.log('API Response:', result);
      console.log('Summary:', result.summary);
      
      setAiSummary(result.summary);
      console.log('Summary set in state');
    } catch (error) {
      console.error('Error generating AI summary:', error);
      setAiSummary('Failed to generate summary. Please try again.');
    } finally {
      setLoadingAISummary(false);
      console.log('Loading complete');
    }
  };

  // Handler for showing match explanation
  const handleShowMatchExplanation = async () => {
    if (!selectedCandidate || !selectedCandidate.similarityScore) return;

    setLoadingExplanation(true);
    setShowMatchExplanation(true);

    try {
      const explanation = await candidateApi.explainMatch({
        candidateId: selectedCandidate.id,
        searchQuery: searchTerm,
        similarityScore: selectedCandidate.similarityScore,
      });
      setMatchExplanation(explanation);
    } catch (error) {
      console.error('Error fetching match explanation:', error);
      setMatchExplanation({
        explanation: 'Failed to load match explanation. Please try again.',
        matchedKeywords: [],
        matchedSnippets: [],
      });
    } finally {
      setLoadingExplanation(false);
    }
  };

  return (
    <Layout>
      <div>
        {/* Header with Search on Same Line */}
        <div className="mb-6 flex items-center justify-between gap-6">
          {/* Left: Title and Subtitle */}
          <div className="flex-shrink-0">
            <h1 className="text-2xl font-bold text-gray-900">Candidates</h1>
            <p className="text-sm text-gray-500 mt-1">Search and manage candidate profiles</p>
          </div>

          {/* Right: Search Bar and Controls */}
          <div className="flex flex-nowrap items-center gap-2">
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
              className="w-80 px-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="Search candidates, skills, resumes..."
            />
            
            <button
              onClick={handleSearch}
              disabled={loading}
              className="px-5 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 font-medium"
            >
              {loading ? 'Searching...' : 'Search'}
            </button>
            
            <button
              onClick={clearResults}
              className="px-5 py-2 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 font-medium"
            >
              Clear
            </button>
            
            <span className="text-sm text-gray-700 ml-2">Sponsorship:</span>
            
            <div className="flex items-center bg-gray-200 rounded-full p-0.5">
              <button
                onClick={() => setSponsorshipFilter('no')}
                className={`px-3 py-1 text-sm font-medium rounded-full transition-all ${
                  sponsorshipFilter === 'no'
                    ? 'bg-blue-600 text-white'
                    : 'text-gray-600 hover:text-gray-900'
                }`}
              >
                No
              </button>
              <button
                onClick={() => setSponsorshipFilter('all')}
                className={`px-3 py-1 text-sm font-medium rounded-full transition-all ${
                  sponsorshipFilter === 'all'
                    ? 'bg-blue-600 text-white'
                    : 'text-gray-600 hover:text-gray-900'
                }`}
              >
                All
              </button>
              <button
                onClick={() => setSponsorshipFilter('yes')}
                className={`px-3 py-1 text-sm font-medium rounded-full transition-all ${
                  sponsorshipFilter === 'yes'
                    ? 'bg-blue-600 text-white'
                    : 'text-gray-600 hover:text-gray-900'
                }`}
              >
                Yes
              </button>
            </div>
          </div>
        </div>

        {/* Error Display */}
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
            <div className="text-red-800">Error: {error}</div>
          </div>
        )}

        {/* Split Layout: Left Sidebar + Right Panel - Full width with equal margins */}
        {allCandidates.length > 0 && (
          <div className="flex gap-4" style={{ height: 'calc(100vh - 280px)', minHeight: '500px' }}>
            {/* Left Sidebar - Fixed Width Candidate Menu */}
            <div className="w-80 flex-shrink-0 bg-white rounded-lg shadow flex flex-col" style={{ height: '100%' }}>
              {/* Header */}
              <div className="px-4 py-3 border-b border-gray-200 bg-gray-50 flex-shrink-0">
                <h2 className="text-lg font-semibold text-gray-900">
                  Candidates ({pagination.totalCount})
                </h2>
                <p className="text-sm text-gray-500">
                  Showing {allCandidates.length} of {pagination.totalCount} | Page {pagination.page}/{pagination.totalPages}
                  {pagination.hasNextPage && ' | More available'}
                </p>
              </div>

              {/* Scrollable Candidate List with Infinite Scroll */}
              <div className="flex-1 overflow-y-scroll" style={{ maxHeight: 'calc(100% - 60px)' }} onScroll={handleScroll}>
                <div className="divide-y divide-gray-100">
                  {allCandidates.map((candidate) => (
                    <div
                      key={candidate.id}
                      className={`relative p-3 hover:bg-gray-50 cursor-pointer transition-colors duration-150 ${
                        selectedCandidate?.id === candidate.id 
                          ? 'bg-blue-50' 
                          : ''
                      }`}
                      onClick={() => handleCandidateClick(candidate)}
                    >
                      {selectedCandidate?.id === candidate.id && (
                        <div className="absolute right-0 top-0 bottom-0 w-1 bg-blue-600"></div>
                      )}
                      {/* Line 1: Candidate Name + Status + Match */}
                      <div className="mb-1 flex items-center justify-between gap-1">
                        <h3 className="text-sm font-semibold text-gray-900 truncate flex-1">
                          {cleanCandidateName(candidate.fullName)}
                        </h3>
                        <div className="flex items-center gap-1 flex-shrink-0">
                          {candidate.similarityScore !== undefined && candidate.similarityScore > 0 && (
                            <span className={`px-2 py-0.5 text-xs rounded-full font-semibold ${
                              candidate.similarityScore >= 0.8 ? 'bg-green-100 text-green-700' :
                              candidate.similarityScore >= 0.6 ? 'bg-blue-100 text-blue-700' :
                              candidate.similarityScore >= 0.4 ? 'bg-yellow-100 text-yellow-700' :
                              'bg-gray-100 text-gray-700'
                            }`}>
                              {Math.round(candidate.similarityScore * 100)}%
                            </span>
                          )}
                          <span className={`px-2 py-0.5 text-xs rounded-full ${getStatusColor(candidate.currentStatus)}`}>
                            {candidate.currentStatus}
                          </span>
                        </div>
                      </div>
                      
                      {/* Line 2: Experience, Requisition, Skills, and Code */}
                      <div className="flex items-center justify-between text-xs">
                        <div className="flex items-center gap-2 flex-1 min-w-0">
                          <span className="text-gray-600 flex-shrink-0">
                            {candidate.totalYearsExperience ? `${candidate.totalYearsExperience}yr` : 'New'}
                          </span>
                          {candidate.requisitionName && (
                            <span className="text-blue-600 font-medium truncate max-w-[120px]" title={candidate.requisitionName}>
                              {candidate.requisitionName}
                            </span>
                          )}
                          {candidate.primarySkills.length > 0 && (
                            <div className="flex gap-1 flex-1 min-w-0 overflow-hidden">
                              {candidate.primarySkills.slice(0, 2).map((skill, index) => (
                                <span
                                  key={index}
                                  className="px-1.5 py-0.5 bg-blue-100 text-blue-700 text-xs rounded flex-shrink-0"
                                >
                                  {skill}
                                </span>
                              ))}
                              {candidate.primarySkills.length > 2 && (
                                <span className="px-1.5 py-0.5 bg-gray-100 text-gray-600 text-xs rounded flex-shrink-0">
                                  +{candidate.primarySkills.length - 2}
                                </span>
                              )}
                            </div>
                          )}
                        </div>
                        <span className="text-gray-400 flex-shrink-0 ml-2">
                          {getCandidateDisplayText(candidate)}
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Loading Indicator for Infinite Scroll */}
              {isLoadingMore && (
                <div className="px-4 py-3 border-t border-gray-200 bg-gray-50 flex-shrink-0">
                  <div className="flex items-center justify-center">
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
                    <span className="ml-2 text-sm text-gray-600">Loading more candidates...</span>
                  </div>
                </div>
              )}
            </div>

            {/* Right Panel - Candidate Details */}
            <div className="flex-1 bg-white rounded-lg shadow flex flex-col" style={{ height: '100%' }}>
              {selectedCandidate ? (
                <>
                  {/* Compact Header */}
                  <div className="px-4 py-2 border-b border-gray-200 bg-gray-50 flex-shrink-0">
                    <div className="flex justify-between items-center">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-3 flex-wrap">
                          <h3 className="text-lg font-semibold text-gray-900">
                            {cleanCandidateName(selectedCandidate.fullName)}
                          </h3>
                          <span className="text-sm text-gray-600">
                            {selectedCandidate.email}
                          </span>
                          <span className="text-xs text-gray-400">
                            {getCandidateDetailDisplayText(selectedCandidate)}
                          </span>
                          {selectedCandidate.requisitionName && (
                            <span className="text-sm text-blue-600 font-medium">
                              • {selectedCandidate.requisitionName}
                            </span>
                          )}
                          <button
                            onClick={handleAISummarize}
                            className="ml-2 px-3 py-1 text-xs font-medium bg-gradient-to-r from-purple-600 to-blue-600 text-white rounded-full hover:from-purple-700 hover:to-blue-700 transition-all shadow-sm hover:shadow-md flex items-center gap-1"
                            title="Generate AI Summary"
                          >
                            <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                            </svg>
                            AI Summarize
                          </button>
                        </div>
                      </div>
                      <button
                        onClick={() => setIsFullScreenModal(true)}
                        className="text-gray-400 hover:text-blue-600 transition-colors ml-4"
                        title="Expand to full screen"
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 8V4m0 0h4m-4 0l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4" />
                        </svg>
                      </button>
                    </div>
                  </div>

                  {/* Content */}
                  <div className="flex-1 overflow-y-auto p-4">
                    {isLoadingDetails ? (
                      <div className="flex items-center justify-center py-8">
                        <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600"></div>
                        <span className="ml-2 text-gray-600">Loading details...</span>
                      </div>
                    ) : selectedCandidateDetails ? (
                      <div className="space-y-4">
                        {/* Status Bar with Update Button */}
                        {/* Compact Single-Line Info Bar with Clickable Status */}
                        <div className="flex flex-wrap items-center gap-4 text-sm bg-gray-50 p-3 rounded-lg">
                          <div className="flex items-center gap-2">
                            <span className="font-medium text-gray-700">Status:</span>
                            <button
                              onClick={() => handleStatusUpdate()}
                              className={`px-3 py-1 text-sm rounded-full cursor-pointer transition-all hover:ring-2 hover:ring-blue-500 hover:ring-offset-2 hover:shadow-md hover:underline ${getStatusColor(selectedCandidateDetails.currentStatus || 'New')}`}
                              title="Click to update status"
                            >
                              {selectedCandidateDetails.currentStatus || 'New'} ▼
                            </button>
                          </div>
                          <div className="flex items-center gap-2">
                            <span className="font-medium text-gray-700">Experience:</span>
                            <span className="text-gray-900">{selectedCandidateDetails.totalYearsExperience || 0} years</span>
                          </div>
                          <div className="flex items-center gap-2">
                            <span className="font-medium text-gray-700">Salary:</span>
                            <span className="text-gray-900">
                              {selectedCandidateDetails.salaryExpectation 
                                ? `$${Math.round(selectedCandidateDetails.salaryExpectation / 1000)}K` 
                                : '$0K'}
                            </span>
                          </div>
                          <div className="flex items-center gap-2">
                            <span className="font-medium text-gray-700">Work Auth:</span>
                            <span className="text-gray-900">{selectedCandidateDetails.isAuthorizedToWork ? 'Yes' : 'No'}</span>
                          </div>
                          <div className="flex items-center gap-2">
                            <span className="font-medium text-gray-700">Sponsorship:</span>
                            <span className="text-gray-900">{selectedCandidateDetails.needsSponsorship ? 'Needed' : 'Not Needed'}</span>
                          </div>
                          {selectedCandidate?.similarityScore !== undefined && selectedCandidate.similarityScore > 0 && (
                            <div className="flex items-center gap-2">
                              <span className="font-medium text-gray-700">Match:</span>
                              <span className={`font-semibold ${
                                selectedCandidate.similarityScore >= 0.8 ? 'text-green-600' :
                                selectedCandidate.similarityScore >= 0.6 ? 'text-blue-600' :
                                selectedCandidate.similarityScore >= 0.4 ? 'text-yellow-600' :
                                'text-gray-600'
                              }`}>
                                {Math.round(selectedCandidate.similarityScore * 100)}%
                              </span>
                              <button
                                onClick={handleShowMatchExplanation}
                                className="text-blue-600 hover:text-blue-800 transition-colors"
                                title="Why did this match?"
                              >
                                <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                                  <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                                </svg>
                              </button>
                            </div>
                          )}
                        </div>

                        {/* Skills - Inline */}
                        {selectedCandidateDetails.skills && selectedCandidateDetails.skills.length > 0 && (
                          <div className="flex items-start gap-3">
                            <span className="font-medium text-gray-700 text-sm flex-shrink-0 mt-1">Skills:</span>
                            <div className="flex flex-wrap gap-1">
                              {selectedCandidateDetails.skills.map((skill: string, index: number) => (
                                <span
                                  key={index}
                                  className="px-2 py-1 bg-blue-100 text-blue-800 text-xs rounded"
                                >
                                  {skill}
                                </span>
                              ))}
                            </div>
                          </div>
                        )}

                        {/* Resume Content - No Title */}
                        {selectedCandidateDetails.resumes && selectedCandidateDetails.resumes.length > 0 ? (
                          <div>
                            {selectedCandidateDetails.resumes.map((resume: any, index: number) => (
                              <div key={resume.id || index}>
                                {resume.resumeText ? (
                                  <div className="bg-gray-50 border rounded-lg p-4">
                                    <HighlightedResumeText text={resume.resumeText} searchTerms={searchTerm} />
                                  </div>
                                ) : (
                                  <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
                                    <p className="text-yellow-800 text-sm">
                                      ⚠️ Resume text is not available. The file may need to be processed.
                                    </p>
                                  </div>
                                )}
                              </div>
                            ))}
                          </div>
                        ) : (
                          <div className="bg-gray-50 border border-gray-200 rounded-lg p-8 text-center">
                            <div className="text-gray-400 mb-3">
                              <svg className="w-12 h-12 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1} 
                                      d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                              </svg>
                            </div>
                            <p className="text-gray-600">No resume available for this candidate</p>
                          </div>
                        )}
                      </div>
                    ) : (
                      <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                        <p className="text-red-800">
                          ❌ Error loading candidate details. Please try again.
                        </p>
                      </div>
                    )}
                  </div>
                </>
              ) : (
                <div className="flex-1 flex items-center justify-center text-gray-500">
                  <div className="text-center">
                    <svg className="w-16 h-16 mx-auto mb-4 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1} 
                            d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                    </svg>
                    <p className="text-lg font-medium text-gray-600">Select a candidate</p>
                    <p className="text-sm text-gray-500 mt-1">Choose a candidate from the list to view their details</p>
                  </div>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Full Screen Resume Modal */}
        {isFullScreenModal && selectedCandidateDetails && (
          <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-lg w-full h-full max-w-6xl max-h-[90vh] flex flex-col">
              {/* Modal Header */}
              <div className="px-6 py-4 border-b border-gray-200 bg-gray-50 flex justify-between items-center flex-shrink-0">
                <div>
                  <h2 className="text-xl font-semibold text-gray-900">
                    {cleanCandidateName(selectedCandidate?.fullName || '')}
                  </h2>
                  <p className="text-sm text-gray-600 mt-1">
                    {selectedCandidate?.email} • {selectedCandidate && getCandidateDetailDisplayText(selectedCandidate)}
                    {selectedCandidate?.requisitionName && (
                      <span className="text-blue-600 font-medium"> • {selectedCandidate.requisitionName}</span>
                    )}
                  </p>
                </div>
                <button
                  onClick={() => setIsFullScreenModal(false)}
                  className="text-gray-400 hover:text-gray-600 transition-colors"
                  title="Close full screen"
                >
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
              
              {/* Modal Content - Resume */}
              <div className="flex-1 overflow-y-auto p-6">
                {selectedCandidateDetails.resumes && selectedCandidateDetails.resumes.length > 0 ? (
                  <div className="space-y-4">
                    {selectedCandidateDetails.resumes.map((resume: any, index: number) => (
                      <div key={resume.id || index}>
                        {resume.resumeText ? (
                          <div className="bg-gray-50 border rounded-lg p-6">
                            <HighlightedResumeText text={resume.resumeText} searchTerms={searchTerm} />
                          </div>
                        ) : (
                          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-6 text-center">
                            <p className="text-yellow-800">
                              ⚠️ Resume text is not available. The file may need to be processed.
                            </p>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="bg-gray-50 border border-gray-200 rounded-lg p-12 text-center">
                    <div className="text-gray-400 mb-4">
                      <svg className="w-16 h-16 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1} 
                              d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                      </svg>
                    </div>
                    <p className="text-gray-600 text-lg">No resume available for this candidate</p>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        {/* Status Update Modal */}
        {isStatusModalOpen && selectedCandidateForStatus && (
          <StatusUpdateModal
            isOpen={isStatusModalOpen}
            candidateId={selectedCandidateForStatus.id}
            candidateName={cleanCandidateName(selectedCandidateForStatus.fullName)}
            currentStatus={selectedCandidateForStatus.currentStatus || 'New'}
            availableStatuses={Object.values(CandidateStatuses)}
            onClose={() => {
              setIsStatusModalOpen(false);
              setSelectedCandidateForStatus(null);
            }}
            onStatusUpdate={async (candidateId: string, statusUpdate: CandidateStatusUpdate) => {
              try {
                await candidateApi.updateCandidateStatus(candidateId, statusUpdate);
                
                // Update the selected candidate details if it's the same candidate
                if (selectedCandidateDetails && selectedCandidateDetails.id === candidateId) {
                  setSelectedCandidateDetails({
                    ...selectedCandidateDetails,
                    currentStatus: statusUpdate.newStatus
                  });
                }
                
                // Update the candidate in the main list
                setAllCandidates(prev => 
                  prev.map(candidate => 
                    candidate.id === candidateId 
                      ? { ...candidate, currentStatus: statusUpdate.newStatus }
                      : candidate
                  )
                );
                
                setIsStatusModalOpen(false);
                setSelectedCandidateForStatus(null);
              } catch (error) {
                console.error('Error updating candidate status:', error);
                // Modal will handle error display
              }
            }}
          />
        )}

        {/* Match Explanation Modal */}
        {showMatchExplanation && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50" onClick={() => setShowMatchExplanation(false)}>
            <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full mx-4 max-h-[80vh] overflow-hidden" onClick={(e) => e.stopPropagation()}>
              {/* Modal Header */}
              <div className="flex items-center justify-between p-4 border-b border-gray-200 bg-blue-50">
                <div className="flex items-center gap-2">
                  <svg className="w-6 h-6 text-blue-600" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                  </svg>
                  <h2 className="text-xl font-semibold text-gray-900">Why This Match?</h2>
                </div>
                <button
                  onClick={() => setShowMatchExplanation(false)}
                  className="text-gray-400 hover:text-gray-600 transition-colors"
                >
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              {/* Modal Body */}
              <div className="p-6 overflow-y-auto max-h-[calc(80vh-80px)]">
                {loadingExplanation ? (
                  <div className="flex items-center justify-center py-12">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                    <span className="ml-3 text-gray-600">Analyzing match...</span>
                  </div>
                ) : matchExplanation ? (
                  <div className="space-y-6">
                    {/* Overall Match Score */}
                    <div className="bg-gray-50 rounded-lg p-4">
                      <div className="flex items-center justify-between mb-2">
                        <span className="text-sm font-medium text-gray-700">Overall Match Score</span>
                        <span className={`text-2xl font-bold ${
                          matchExplanation.similarityScore >= 0.8 ? 'text-green-600' :
                          matchExplanation.similarityScore >= 0.6 ? 'text-blue-600' :
                          matchExplanation.similarityScore >= 0.4 ? 'text-yellow-600' :
                          'text-gray-600'
                        }`}>
                          {Math.round(matchExplanation.similarityScore * 100)}%
                        </span>
                      </div>
                      {matchExplanation.semanticScore !== undefined && matchExplanation.semanticScore > 0 && (
                        <div className="text-xs text-gray-600 mt-1">
                          Semantic Score: {Math.round(matchExplanation.semanticScore * 100)}%
                        </div>
                      )}
                    </div>

                    {/* Explanation */}
                    <div>
                      <h3 className="text-sm font-semibold text-gray-900 mb-2">Explanation</h3>
                      <p className="text-sm text-gray-700 leading-relaxed">
                        {matchExplanation.explanation}
                      </p>
                    </div>

                    {/* Matched Keywords */}
                    {matchExplanation.matchedKeywords && matchExplanation.matchedKeywords.length > 0 && (
                      <div>
                        <h3 className="text-sm font-semibold text-gray-900 mb-2">Matched Keywords</h3>
                        <div className="flex flex-wrap gap-2">
                          {matchExplanation.matchedKeywords.map((keyword: string, index: number) => (
                            <span
                              key={index}
                              className="px-3 py-1 bg-blue-100 text-blue-800 text-sm rounded-full font-medium"
                            >
                              {keyword}
                            </span>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Matched Snippets */}
                    {matchExplanation.matchedSnippets && matchExplanation.matchedSnippets.length > 0 && (
                      <div>
                        <h3 className="text-sm font-semibold text-gray-900 mb-3">Matched Content</h3>
                        <div className="space-y-3">
                          {matchExplanation.matchedSnippets.map((snippet: any, index: number) => (
                            <div key={index} className="bg-gray-50 rounded-lg p-3 border border-gray-200">
                              <div className="flex items-center justify-between mb-2">
                                <span className="text-xs font-semibold text-blue-600 uppercase">
                                  {snippet.source}
                                </span>
                                <span className="text-xs text-gray-500">
                                  Relevance: {Math.round(snippet.relevance * 100)}%
                                </span>
                              </div>
                              <p className="text-sm text-gray-700 leading-relaxed">
                                {snippet.text}
                              </p>
                              {snippet.highlightedTerms && snippet.highlightedTerms.length > 0 && (
                                <div className="flex flex-wrap gap-1 mt-2">
                                  {snippet.highlightedTerms.map((term: string, termIndex: number) => (
                                    <span
                                      key={termIndex}
                                      className="px-2 py-0.5 bg-yellow-100 text-yellow-800 text-xs rounded"
                                    >
                                      {term}
                                    </span>
                                  ))}
                                </div>
                              )}
                            </div>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Semantic Search Info */}
                    {matchExplanation.matchedKeywords.length === 0 && matchExplanation.semanticScore && (
                      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                        <div className="flex items-start">
                          <svg className="w-5 h-5 text-blue-600 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                            <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                          </svg>
                          <div className="ml-3">
                            <h4 className="text-sm font-medium text-blue-900">Semantic Match</h4>
                            <p className="text-sm text-blue-700 mt-1">
                              This candidate matched based on semantic similarity using AI embeddings. 
                              Their experience with related concepts, technologies, or domains is relevant 
                              to your search, even without exact keyword matches.
                            </p>
                          </div>
                        </div>
                      </div>
                    )}
                  </div>
                ) : (
                  <div className="text-center py-8 text-gray-500">
                    No explanation available
                  </div>
                )}
              </div>

              {/* Modal Footer */}
              <div className="flex justify-end gap-2 p-4 border-t border-gray-200 bg-gray-50">
                <button
                  onClick={() => setShowMatchExplanation(false)}
                  className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
                >
                  Close
                </button>
              </div>
            </div>
          </div>
        )}

        {/* AI Summary Modal */}
        {showAISummary && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50" onClick={() => setShowAISummary(false)}>
            <div className="bg-white rounded-lg shadow-xl max-w-3xl w-full mx-4 max-h-[85vh] overflow-hidden" onClick={(e) => e.stopPropagation()}>
              {/* Modal Header */}
              <div className="flex items-center justify-between p-4 border-b border-gray-200 bg-gradient-to-r from-purple-50 to-blue-50">
                <div className="flex items-center gap-2">
                  <svg className="w-6 h-6 text-purple-600" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M13 6a3 3 0 11-6 0 3 3 0 016 0zM18 8a2 2 0 11-4 0 2 2 0 014 0zM14 15a4 4 0 00-8 0v3h8v-3zM6 8a2 2 0 11-4 0 2 2 0 014 0zM16 18v-3a5.972 5.972 0 00-.75-2.906A3.005 3.005 0 0119 15v3h-3zM4.75 12.094A5.973 5.973 0 004 15v3H1v-3a3 3 0 013.75-2.906z" />
                  </svg>
                  <h2 className="text-xl font-semibold text-gray-900">AI Resume Summary</h2>
                  {selectedCandidate && (
                    <span className="ml-2 text-sm text-gray-500">
                      {cleanCandidateName(selectedCandidate.fullName)}
                    </span>
                  )}
                </div>
                <button
                  onClick={() => setShowAISummary(false)}
                  className="text-gray-400 hover:text-gray-600 transition-colors"
                >
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              {/* Modal Body */}
              <div className="p-6 overflow-y-auto max-h-[calc(85vh-140px)]">
                {loadingAISummary ? (
                  <div className="flex flex-col items-center justify-center py-12">
                    <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-purple-600 mb-4"></div>
                    <span className="text-gray-600 font-medium">Generating AI summary...</span>
                    <span className="text-sm text-gray-500 mt-2">Analyzing resume with advanced AI</span>
                  </div>
                ) : aiSummary ? (
                  <div className="space-y-4">
                    {/* AI Summary Content */}
                    <div className="bg-gradient-to-br from-purple-50 to-blue-50 rounded-lg p-6 border border-purple-200">
                      <div className="flex items-center gap-2 mb-4">
                        <svg className="w-5 h-5 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                        </svg>
                        <h3 className="text-lg font-semibold text-gray-900">Summary</h3>
                      </div>
                      <div className="prose prose-sm max-w-none">
                        <p className="text-gray-800 leading-relaxed whitespace-pre-line">
                          {aiSummary}
                        </p>
                      </div>
                    </div>

                    {/* How This Works */}
                    <details className="bg-gray-50 rounded-lg border border-gray-200">
                      <summary className="px-4 py-3 cursor-pointer hover:bg-gray-100 transition-colors font-medium text-gray-700 text-sm">
                        How AI Summarization Works
                      </summary>
                      <div className="px-4 py-3 border-t border-gray-200">
                        <p className="text-sm text-gray-700 leading-relaxed">
                          The AI summary is generated by Azure OpenAI using a specialized prompt designed for technical recruiting. 
                          The system analyzes the candidate's resume focusing on:
                        </p>
                        <ul className="mt-2 text-sm text-gray-700 space-y-1 ml-4 list-disc">
                          <li>Years of experience and relevant technical roles</li>
                          <li>Technical skills, programming languages, and frameworks</li>
                          <li>Industry and domain expertise</li>
                          <li>Key achievements and quantifiable results</li>
                          <li>Role scope and level of responsibility</li>
                          <li>Overall seniority assessment</li>
                        </ul>
                      </div>
                    </details>

                    {/* Info Banner */}
                    <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                      <div className="flex items-start gap-3">
                        <svg className="w-5 h-5 text-blue-600 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                          <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                        </svg>
                        <div>
                          <h4 className="text-sm font-medium text-blue-900">AI-Generated Summary</h4>
                          <p className="text-sm text-blue-700 mt-1">
                            This summary was generated using Azure OpenAI to provide a quick overview of the candidate's 
                            experience, skills, and qualifications. Review the full resume for complete details.
                          </p>
                        </div>
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="text-center py-8 text-gray-500">
                    No summary available
                  </div>
                )}
              </div>

              {/* Modal Footer */}
              <div className="flex justify-end gap-2 p-4 border-t border-gray-200 bg-gray-50">
                <button
                  onClick={() => setShowAISummary(false)}
                  className="px-4 py-2 bg-gradient-to-r from-purple-600 to-blue-600 text-white rounded-md hover:from-purple-700 hover:to-blue-700 transition-all shadow-sm"
                >
                  Close
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </Layout>
  );
};

export default CandidatesPage;