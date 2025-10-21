import React, { useState, useEffect } from 'react';
import { CandidateSearchDto } from '../../types/candidate';
import { clientConfigApi, PrivacyConfigDto } from '../../services/clientConfigApi';

interface CandidateListProps {
  candidates: CandidateSearchDto[];
  loading: boolean;
  onCandidateSelect?: (candidate: CandidateSearchDto) => void;
  selectedCandidateId?: string;
}

export const CandidateList: React.FC<CandidateListProps> = ({
  candidates,
  loading,
  onCandidateSelect,
  selectedCandidateId,
}) => {
  const [sortField, setSortField] = useState<keyof CandidateSearchDto>('fullName');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [privacyConfig, setPrivacyConfig] = useState<PrivacyConfigDto | null>(null);

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

  const getCandidateDisplayName = (candidate: CandidateSearchDto): string => {
    // If PII sanitization is enabled or privacy config is not loaded yet, show candidate code
    if (!privacyConfig || privacyConfig.piiSanitizationEnabled) {
      return candidate.candidateCode;
    }
    // If PII sanitization is disabled, show full name
    return candidate.fullName || `${candidate.firstName} ${candidate.lastName}`.trim();
  };

  const getColumnHeader = (): string => {
    // If PII sanitization is enabled or privacy config is not loaded yet, show "Code"
    if (!privacyConfig || privacyConfig.piiSanitizationEnabled) {
      return 'Code';
    }
    // If PII sanitization is disabled, show "Name"
    return 'Name';
  };

  const handleSort = (field: keyof CandidateSearchDto) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDirection('asc');
    }
  };

  const sortedCandidates = [...candidates].sort((a, b) => {
    const aVal = a[sortField];
    const bVal = b[sortField];
    
    let comparison = 0;
    
    // Handle undefined/null values
    if (aVal == null && bVal == null) return 0;
    if (aVal == null) return 1;
    if (bVal == null) return -1;
    
    if (aVal < bVal) comparison = -1;
    if (aVal > bVal) comparison = 1;
    
    return sortDirection === 'asc' ? comparison : -comparison;
  });

  const formatSalary = (salary?: number) => {
    if (!salary) return 'Not specified';
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 0,
    }).format(salary);
  };

  const SortIcon = ({ field }: { field: keyof CandidateSearchDto }) => (
    <span className="ml-1 text-gray-400">
      {sortField === field ? (
        sortDirection === 'asc' ? '↑' : '↓'
      ) : (
        '↕'
      )}
    </span>
  );

  if (loading) {
    return (
      <div className="bg-white rounded-lg shadow">
        <div className="p-6 text-center">
          <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
          <p className="mt-2 text-gray-600">Loading candidates...</p>
        </div>
      </div>
    );
  }

  if (candidates.length === 0) {
    return (
      <div className="bg-white rounded-lg shadow">
        <div className="p-6 text-center text-gray-500">
          <p className="text-lg">No candidates found</p>
          <p className="text-sm mt-1">Try adjusting your search criteria</p>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg shadow overflow-hidden">
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th 
                className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                onClick={() => handleSort(privacyConfig?.piiSanitizationEnabled !== false ? 'candidateCode' : 'fullName')}
              >
                {getColumnHeader()}
                <SortIcon field={privacyConfig?.piiSanitizationEnabled !== false ? 'candidateCode' : 'fullName'} />
              </th>
              <th 
                className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                onClick={() => handleSort('fullName')}
              >
                Name
                <SortIcon field="fullName" />
              </th>
              <th 
                className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                onClick={() => handleSort('email')}
              >
                Email
                <SortIcon field="email" />
              </th>
              <th 
                className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                onClick={() => handleSort('currentTitle')}
              >
                Current Title
                <SortIcon field="currentTitle" />
              </th>
              <th 
                className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                onClick={() => handleSort('totalYearsExperience')}
              >
                Experience
                <SortIcon field="totalYearsExperience" />
              </th>
              <th 
                className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                onClick={() => handleSort('salaryExpectation')}
              >
                Salary Expectation
                <SortIcon field="salaryExpectation" />
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Status
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Skills
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {sortedCandidates.map((candidate) => (
              <tr 
                key={candidate.id}
                className={`hover:bg-gray-50 cursor-pointer transition-colors ${
                  selectedCandidateId === candidate.id ? 'bg-blue-50' : ''
                }`}
                onClick={() => onCandidateSelect?.(candidate)}
              >
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                  {getCandidateDisplayName(candidate)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium text-gray-900">
                    {candidate.fullName}
                  </div>
                  <div className="text-sm text-gray-500">
                    {candidate.firstName} {candidate.lastName}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  <a 
                    href={`mailto:${candidate.email}`} 
                    className="text-blue-600 hover:text-blue-800"
                    onClick={(e) => e.stopPropagation()}
                  >
                    {candidate.email}
                  </a>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {candidate.currentTitle || 'Not specified'}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {candidate.totalYearsExperience ? 
                    `${candidate.totalYearsExperience} years` : 
                    'Not specified'
                  }
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {formatSalary(candidate.salaryExpectation)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="flex flex-col space-y-1">
                    <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                      candidate.isActive 
                        ? 'bg-green-100 text-green-800' 
                        : 'bg-gray-100 text-gray-800'
                    }`}>
                      {candidate.isActive ? 'Active' : 'Inactive'}
                    </span>
                    <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                      candidate.isAuthorizedToWork 
                        ? 'bg-green-100 text-green-800' 
                        : 'bg-red-100 text-red-800'
                    }`}>
                      {candidate.isAuthorizedToWork ? 'Authorized' : 'Not Authorized'}
                    </span>
                    {candidate.needsSponsorship && (
                      <span className="inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-yellow-100 text-yellow-800">
                        Needs Sponsorship
                      </span>
                    )}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  <div className="flex flex-wrap gap-1">
                    {candidate.primarySkills.slice(0, 3).map((skill, index) => (
                      <span 
                        key={index}
                        className="inline-flex px-2 py-1 text-xs bg-blue-100 text-blue-800 rounded-full"
                      >
                        {skill}
                      </span>
                    ))}
                    {candidate.primarySkills.length > 3 && (
                      <span className="inline-flex px-2 py-1 text-xs bg-gray-100 text-gray-600 rounded-full">
                        +{candidate.primarySkills.length - 3} more
                      </span>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};