import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';

interface Candidate {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  location?: string;
  currentCompany?: string;
  currentTitle?: string;
  yearsOfExperience?: number;
  visaStatus?: string;
  salaryExpectation?: number;
  status: string;
  primarySkills: string[];
}

interface CandidateSearchRequest {
  searchTerm?: string;
  location?: string;
  currentCompany?: string;
  currentTitle?: string;
  minYearsOfExperience?: number;
  maxYearsOfExperience?: number;
  visaStatus?: string;
  minSalaryExpectation?: number;
  maxSalaryExpectation?: number;
  relocationWillingness?: boolean;
  preferredWorkType?: string;
  status?: string;
  skills: string[];
  companies: string[];
  jobTitles: string[];
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: string;
}

interface CandidateSearchResponse {
  candidates: Candidate[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

const searchCandidates = async (searchRequest: CandidateSearchRequest): Promise<CandidateSearchResponse> => {
  const response = await fetch('http://localhost:5000/api/candidates/search', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(searchRequest),
  });

  if (!response.ok) {
    throw new Error('Failed to search candidates');
  }

  return response.json();
};

const Candidates: React.FC = () => {
  const [searchParams, setSearchParams] = useState<CandidateSearchRequest>({
    searchTerm: '',
    location: '',
    currentCompany: '',
    currentTitle: '',
    visaStatus: '',
    status: '',
    skills: [],
    companies: [],
    jobTitles: [],
    page: 1,
    pageSize: 20,
    sortBy: 'lastName',
    sortDirection: 'asc'
  });

  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [uploadStatus, setUploadStatus] = useState<string>('');

  const {
    data: searchResults,
    isLoading,
    error,
    refetch
  } = useQuery({
    queryKey: ['candidates', searchParams],
    queryFn: () => searchCandidates(searchParams),
    enabled: false // Only search when user clicks search button
  });

  const handleSearch = () => {
    refetch();
  };

  const handleInputChange = (field: keyof CandidateSearchRequest, value: any) => {
    setSearchParams(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleFileUpload = async () => {
    if (!selectedFile) {
      setUploadStatus('Please select a file first');
      return;
    }

    const formData = new FormData();
    formData.append('file', selectedFile);
    formData.append('overwriteExisting', 'false');

    try {
      setUploadStatus('Uploading and processing...');
      const response = await fetch('http://localhost:5000/api/excelimport/upload', {
        method: 'POST',
        body: formData,
      });

      const result = await response.json();
      
      if (response.ok) {
        setUploadStatus(`Success! Processed: ${result.processedRecords}, Skipped: ${result.skippedRecords}, Errors: ${result.errorRecords}`);
        // Refresh the search results
        refetch();
      } else {
        setUploadStatus(`Error: ${result.message}`);
      }
    } catch (error) {
      setUploadStatus(`Error: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold text-gray-900 mb-8">Candidate Management</h1>
      
      {/* Excel Upload Section */}
      <div className="bg-white shadow rounded-lg p-6 mb-8">
        <h2 className="text-xl font-semibold text-gray-900 mb-4">Upload Excel File</h2>
        <div className="flex items-center space-x-4">
          <input
            type="file"
            accept=".xlsx,.xls"
            onChange={(e) => setSelectedFile(e.target.files?.[0] || null)}
            className="block w-full text-sm text-gray-500
              file:mr-4 file:py-2 file:px-4
              file:rounded-full file:border-0
              file:text-sm file:font-semibold
              file:bg-blue-50 file:text-blue-700
              hover:file:bg-blue-100"
          />
          <button
            onClick={handleFileUpload}
            disabled={!selectedFile}
            className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:bg-gray-400"
          >
            Upload
          </button>
        </div>
        {uploadStatus && (
          <div className={`mt-2 text-sm ${uploadStatus.includes('Error') ? 'text-red-600' : 'text-green-600'}`}>
            {uploadStatus}
          </div>
        )}
      </div>

      {/* Search Section */}
      <div className="bg-white shadow rounded-lg p-6 mb-8">
        <h2 className="text-xl font-semibold text-gray-900 mb-4">Search Candidates</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
          <input
            type="text"
            placeholder="Search by name, email, company..."
            value={searchParams.searchTerm}
            onChange={(e) => handleInputChange('searchTerm', e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <input
            type="text"
            placeholder="Location"
            value={searchParams.location}
            onChange={(e) => handleInputChange('location', e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <input
            type="text"
            placeholder="Current Company"
            value={searchParams.currentCompany}
            onChange={(e) => handleInputChange('currentCompany', e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <input
            type="text"
            placeholder="Current Title"
            value={searchParams.currentTitle}
            onChange={(e) => handleInputChange('currentTitle', e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <select
            value={searchParams.visaStatus}
            onChange={(e) => handleInputChange('visaStatus', e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">All Visa Status</option>
            <option value="US Citizen">US Citizen</option>
            <option value="Green Card">Green Card</option>
            <option value="H1B">H1B</option>
            <option value="F1/OPT">F1/OPT</option>
            <option value="L1">L1</option>
            <option value="TN">TN</option>
          </select>
          <select
            value={searchParams.status}
            onChange={(e) => handleInputChange('status', e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">All Status</option>
            <option value="Active">Active</option>
            <option value="Inactive">Inactive</option>
            <option value="Hired">Hired</option>
            <option value="Not Interested">Not Interested</option>
          </select>
        </div>
        <div className="flex space-x-4">
          <button
            onClick={handleSearch}
            className="px-6 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
          >
            Search
          </button>
          <button
            onClick={() => {
              setSearchParams({
                searchTerm: '',
                location: '',
                currentCompany: '',
                currentTitle: '',
                visaStatus: '',
                status: '',
                skills: [],
                companies: [],
                jobTitles: [],
                page: 1,
                pageSize: 20,
                sortBy: 'lastName',
                sortDirection: 'asc'
              });
            }}
            className="px-6 py-2 bg-gray-500 text-white rounded hover:bg-gray-600"
          >
            Clear
          </button>
        </div>
      </div>

      {/* Results Section */}
      <div className="bg-white shadow rounded-lg">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900">
            Search Results
            {searchResults && (
              <span className="text-sm font-normal text-gray-500 ml-2">
                ({searchResults.totalCount} candidates found)
              </span>
            )}
          </h2>
        </div>

        {isLoading && (
          <div className="px-6 py-4 text-center">
            <div className="text-gray-500">Loading...</div>
          </div>
        )}

        {error && (
          <div className="px-6 py-4 text-center">
            <div className="text-red-500">
              Error: {error instanceof Error ? error.message : 'An error occurred'}
            </div>
          </div>
        )}

        {searchResults && (
          <div className="divide-y divide-gray-200">
            {searchResults.candidates.length === 0 ? (
              <div className="px-6 py-4 text-center text-gray-500">
                No candidates found. Try adjusting your search criteria.
              </div>
            ) : (
              searchResults.candidates.map((candidate) => (
                <div key={candidate.id} className="px-6 py-4 hover:bg-gray-50">
                  <div className="flex items-center justify-between">
                    <div className="flex-1">
                      <div className="flex items-center">
                        <h3 className="text-lg font-medium text-gray-900">
                          {candidate.firstName} {candidate.lastName}
                        </h3>
                        <span className={`ml-2 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                          candidate.status === 'Active' ? 'bg-green-100 text-green-800' :
                          candidate.status === 'Hired' ? 'bg-blue-100 text-blue-800' :
                          'bg-gray-100 text-gray-800'
                        }`}>
                          {candidate.status}
                        </span>
                      </div>
                      <p className="text-sm text-gray-600 mt-1">{candidate.email}</p>
                      <div className="mt-2 flex flex-wrap gap-4 text-sm text-gray-500">
                        {candidate.currentTitle && (
                          <span>{candidate.currentTitle}</span>
                        )}
                        {candidate.currentCompany && (
                          <span>at {candidate.currentCompany}</span>
                        )}
                        {candidate.location && (
                          <span>📍 {candidate.location}</span>
                        )}
                        {candidate.yearsOfExperience && (
                          <span>💼 {candidate.yearsOfExperience} years exp</span>
                        )}
                        {candidate.visaStatus && (
                          <span>🛂 {candidate.visaStatus}</span>
                        )}
                      </div>
                      {candidate.primarySkills.length > 0 && (
                        <div className="mt-2 flex flex-wrap gap-1">
                          {candidate.primarySkills.slice(0, 5).map((skill, index) => (
                            <span
                              key={index}
                              className="inline-flex items-center px-2 py-1 rounded-md text-xs font-medium bg-blue-100 text-blue-800"
                            >
                              {skill}
                            </span>
                          ))}
                          {candidate.primarySkills.length > 5 && (
                            <span className="text-xs text-gray-500">
                              +{candidate.primarySkills.length - 5} more
                            </span>
                          )}
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        )}

        {searchResults && searchResults.totalPages > 1 && (
          <div className="px-6 py-4 border-t border-gray-200 flex items-center justify-between">
            <div className="text-sm text-gray-500">
              Page {searchResults.page} of {searchResults.totalPages}
            </div>
            <div className="flex space-x-2">
              <button
                onClick={() => handleInputChange('page', searchParams.page - 1)}
                disabled={!searchResults.hasPreviousPage}
                className="px-3 py-1 text-sm bg-gray-100 text-gray-700 rounded hover:bg-gray-200 disabled:opacity-50"
              >
                Previous
              </button>
              <button
                onClick={() => handleInputChange('page', searchParams.page + 1)}
                disabled={!searchResults.hasNextPage}
                className="px-3 py-1 text-sm bg-gray-100 text-gray-700 rounded hover:bg-gray-200 disabled:opacity-50"
              >
                Next
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default Candidates;