import React, { useEffect, useState } from 'react';
import Layout from '../components/common/Layout';
import { candidateApi } from '../services/candidateApi';
import { clientConfigApi, SearchScoringConfigDto, PrivacyConfigDto } from '../services/clientConfigApi';

interface SystemStatistics {
  totalCandidates: number;
  withEmbeddings: number;
  coveragePercent: number;
}

interface UploadResult {
  success: boolean;
  message: string;
  processedRows: number;
  errorCount: number;
  importedCandidates: number;
  errors: string[];
}

const Settings: React.FC = () => {
  const [statistics, setStatistics] = useState<SystemStatistics | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);
  const [uploadResult, setUploadResult] = useState<UploadResult | null>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [generatingEmbeddings, setGeneratingEmbeddings] = useState(false);
  const [embeddingGenerationResult, setEmbeddingGenerationResult] = useState<{
    message: string;
    candidatesQueued: number;
    estimatedTimeMinutes: number;
  } | null>(null);
  const [embeddingGenerationError, setEmbeddingGenerationError] = useState<string | null>(null);
  
  // Scoring strategy state
  const [scoringConfig, setScoringConfig] = useState<SearchScoringConfigDto | null>(null);
  const [availableStrategies, setAvailableStrategies] = useState<Record<string, string>>({});
  const [updatingScoringStrategy, setUpdatingScoringStrategy] = useState(false);
  const [scoringStrategyError, setScoringStrategyError] = useState<string | null>(null);

  // Privacy configuration state
  const [privacyConfig, setPrivacyConfig] = useState<PrivacyConfigDto | null>(null);
  const [updatingPrivacyConfig, setUpdatingPrivacyConfig] = useState(false);
  const [privacyConfigError, setPrivacyConfigError] = useState<string | null>(null);

  // FTS rebuild state
  const [rebuildingFts, setRebuildingFts] = useState(false);
  const [ftsRebuildResult, setFtsRebuildResult] = useState<{
    success: boolean;
    message: string;
    processedItems: number;
    durationMs: number;
  } | null>(null);
  const [ftsRebuildError, setFtsRebuildError] = useState<string | null>(null);

  // Search mode preference state
  const [searchMode, setSearchMode] = useState<'semantic' | 'nameMatch' | 'auto'>('semantic');
  const [updatingSearchMode, setUpdatingSearchMode] = useState(false);
  const [searchModeError, setSearchModeError] = useState<string | null>(null);

  useEffect(() => {
    fetchStatistics();
    fetchScoringConfig();
    fetchPrivacyConfig();
  }, []);

  const fetchScoringConfig = async () => {
    try {
      const [config, strategies] = await Promise.all([
        clientConfigApi.getSearchScoringConfig(),
        clientConfigApi.getScoringStrategies()
      ]);
      setScoringConfig(config);
      setAvailableStrategies(strategies);
    } catch (error) {
      console.error('Error loading scoring config:', error);
      setScoringStrategyError('Failed to load scoring configuration');
    }
  };

  const fetchPrivacyConfig = async () => {
    try {
      const config = await clientConfigApi.getPrivacyConfig();
      setPrivacyConfig(config);
    } catch (error) {
      console.error('Error loading privacy config:', error);
      setPrivacyConfigError('Failed to load privacy configuration');
    }
  };

  const fetchStatistics = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await candidateApi.getSystemStatistics();
      setStatistics(data);
    } catch (err) {
      console.error('Error fetching statistics:', err);
      setError('Failed to load system statistics');
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (coverage: number) => {
    if (coverage >= 80) return 'text-green-600';
    if (coverage >= 50) return 'text-yellow-600';
    return 'text-red-600';
  };

  const getStatusBgColor = (coverage: number) => {
    if (coverage >= 80) return 'bg-green-100';
    if (coverage >= 50) return 'bg-yellow-100';
    return 'bg-red-100';
  };

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      setSelectedFile(file);
      setUploadResult(null);
      setUploadError(null);
    }
  };

  const handleUpload = async () => {
    if (!selectedFile) return;

    try {
      setUploading(true);
      setUploadError(null);
      setUploadResult(null);
      
      const result = await candidateApi.uploadExcel(selectedFile);
      setUploadResult(result);
      
      // Refresh statistics after successful upload
      if (result.success) {
        await fetchStatistics();
      }
    } catch (err) {
      console.error('Error uploading Excel file:', err);
      setUploadError(err instanceof Error ? err.message : 'Failed to upload Excel file');
    } finally {
      setUploading(false);
    }
  };

  const handleClearUpload = () => {
    setSelectedFile(null);
    setUploadResult(null);
    setUploadError(null);
  };

  const handleGenerateMissingEmbeddings = async () => {
    try {
      setGeneratingEmbeddings(true);
      setEmbeddingGenerationError(null);
      setEmbeddingGenerationResult(null);

      const result = await candidateApi.generateMissingEmbeddings();
      setEmbeddingGenerationResult(result);

      // Refresh statistics after queueing
      await fetchStatistics();
    } catch (err) {
      console.error('Error generating embeddings:', err);
      setEmbeddingGenerationError(
        err instanceof Error ? err.message : 'Failed to generate embeddings'
      );
    } finally {
      setGeneratingEmbeddings(false);
    }
  };

  const handleScoringStrategyChange = async (strategy: 'option1' | 'option4') => {
    setUpdatingScoringStrategy(true);
    setScoringStrategyError(null);
    try {
      await clientConfigApi.updateScoringStrategy(strategy);
      setScoringConfig(prev => prev ? { ...prev, scoringStrategy: strategy } : null);
    } catch (error) {
      console.error('Error updating scoring strategy:', error);
      setScoringStrategyError('Failed to update scoring strategy');
    } finally {
      setUpdatingScoringStrategy(false);
    }
  };

  const handlePiiToggle = async (enabled: boolean) => {
    if (!privacyConfig) return;
    
    setUpdatingPrivacyConfig(true);
    setPrivacyConfigError(null);
    try {
      const updatedConfig = {
        ...privacyConfig,
        piiSanitizationEnabled: enabled
      };
      const result = await clientConfigApi.updatePrivacyConfig(updatedConfig);
      setPrivacyConfig(result);
    } catch (error) {
      console.error('Error updating PII configuration:', error);
      setPrivacyConfigError('Failed to update PII sanitization setting');
    } finally {
      setUpdatingPrivacyConfig(false);
    }
  };

  const handlePiiLevelChange = async (level: 'minimal' | 'standard' | 'full') => {
    if (!privacyConfig) return;
    
    setUpdatingPrivacyConfig(true);
    setPrivacyConfigError(null);
    try {
      const updatedConfig = {
        ...privacyConfig,
        piiSanitizationLevel: level
      };
      const result = await clientConfigApi.updatePrivacyConfig(updatedConfig);
      setPrivacyConfig(result);
    } catch (error) {
      console.error('Error updating PII level:', error);
      setPrivacyConfigError('Failed to update PII sanitization level');
    } finally {
      setUpdatingPrivacyConfig(false);
    }
  };

  const handleRebuildFts = async () => {
    setRebuildingFts(true);
    setFtsRebuildError(null);
    setFtsRebuildResult(null);
    
    try {
      const result = await clientConfigApi.rebuildFullTextSearch();
      setFtsRebuildResult(result);
      
      // Refresh statistics after successful rebuild
      if (result.success) {
        await fetchStatistics();
      }
    } catch (error) {
      console.error('Error rebuilding Full-Text Search:', error);
      setFtsRebuildError('Failed to rebuild Full-Text Search infrastructure');
    } finally {
      setRebuildingFts(false);
    }
  };

  const handleSearchModeChange = async (mode: 'semantic' | 'nameMatch' | 'auto') => {
    setUpdatingSearchMode(true);
    setSearchModeError(null);
    
    try {
      // Store in localStorage for now (could be moved to backend config later)
      localStorage.setItem('searchMode', mode);
      setSearchMode(mode);
      
      // Show success feedback
      console.log(`Search mode changed to: ${mode}`);
    } catch (error) {
      console.error('Error updating search mode:', error);
      setSearchModeError('Failed to update search mode preference');
    } finally {
      setUpdatingSearchMode(false);
    }
  };

  // Load search mode from localStorage on component mount
  useEffect(() => {
    const savedSearchMode = localStorage.getItem('searchMode') as 'semantic' | 'nameMatch' | 'auto' | null;
    if (savedSearchMode) {
      setSearchMode(savedSearchMode);
    }
  }, []);

  return (
    <Layout>
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-gray-900">Settings</h1>
          <p className="text-sm text-gray-500 mt-1">System configuration and statistics</p>
        </div>

        {/* System Statistics Card */}
        <div className="bg-white rounded-lg shadow-md p-6 mb-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-semibold text-gray-900">Embedding Statistics</h2>
            <div className="flex gap-2">
              <button
                onClick={handleGenerateMissingEmbeddings}
                disabled={generatingEmbeddings || loading}
                className="px-4 py-2 text-sm bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 transition-colors"
              >
                {generatingEmbeddings ? (
                  <span className="flex items-center gap-2">
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                    Queueing...
                  </span>
                ) : (
                  'Generate Missing'
                )}
              </button>
              <button
                onClick={fetchStatistics}
                disabled={loading}
                className="px-4 py-2 text-sm bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 disabled:opacity-50 transition-colors"
              >
                {loading ? 'Refreshing...' : 'Refresh'}
              </button>
            </div>
          </div>

          {error && (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-4">
              <p className="text-red-800">{error}</p>
            </div>
          )}

          {/* Embedding Generation Success */}
          {embeddingGenerationResult && (
            <div className="bg-green-50 border border-green-200 rounded-lg p-4 mb-4">
              <div className="flex items-start">
                <svg className="h-5 w-5 text-green-600 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                </svg>
                <div className="ml-3">
                  <h3 className="text-sm font-medium text-green-800">Embedding Generation Queued</h3>
                  <p className="text-sm text-green-700 mt-1">{embeddingGenerationResult.message}</p>
                  <div className="mt-2 text-sm text-green-600">
                    <p><strong>{embeddingGenerationResult.candidatesQueued}</strong> candidates queued for processing</p>
                    <p>Estimated time: <strong>~{embeddingGenerationResult.estimatedTimeMinutes} minutes</strong></p>
                    <p className="mt-1 text-xs">Refresh statistics in a few minutes to see updated coverage.</p>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Embedding Generation Error */}
          {embeddingGenerationError && (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-4">
              <div className="flex items-start">
                <svg className="h-5 w-5 text-red-600 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                </svg>
                <div className="ml-3">
                  <h3 className="text-sm font-medium text-red-800">Failed to Generate Embeddings</h3>
                  <p className="text-sm text-red-700 mt-1">{embeddingGenerationError}</p>
                </div>
              </div>
            </div>
          )}

          {loading && !statistics ? (
            <div className="flex items-center justify-center py-12">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
            </div>
          ) : statistics ? (
            <div className="space-y-6">
              {/* Statistics Grid */}
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {/* Total Candidates */}
                <div className="bg-blue-50 rounded-lg p-4">
                  <div className="text-sm text-blue-600 font-medium mb-1">Total Candidates</div>
                  <div className="text-3xl font-bold text-blue-900">{statistics.totalCandidates}</div>
                  <div className="text-xs text-blue-600 mt-1">Active candidates in system</div>
                </div>

                {/* With Embeddings */}
                <div className="bg-green-50 rounded-lg p-4">
                  <div className="text-sm text-green-600 font-medium mb-1">With Embeddings</div>
                  <div className="text-3xl font-bold text-green-900">{statistics.withEmbeddings}</div>
                  <div className="text-xs text-green-600 mt-1">Candidates with semantic search</div>
                </div>

                {/* Coverage Percentage */}
                <div className={`${getStatusBgColor(statistics.coveragePercent)} rounded-lg p-4`}>
                  <div className={`text-sm ${getStatusColor(statistics.coveragePercent)} font-medium mb-1`}>
                    Coverage
                  </div>
                  <div className={`text-3xl font-bold ${getStatusColor(statistics.coveragePercent)}`}>
                    {statistics.coveragePercent}%
                  </div>
                  <div className={`text-xs ${getStatusColor(statistics.coveragePercent)} mt-1`}>
                    Embedding coverage rate
                  </div>
                </div>
              </div>

              {/* Progress Bar */}
              <div>
                <div className="flex justify-between text-sm text-gray-600 mb-2">
                  <span>Embedding Progress</span>
                  <span>{statistics.withEmbeddings} / {statistics.totalCandidates}</span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-4 overflow-hidden">
                  <div
                    className={`h-full transition-all duration-500 ${
                      statistics.coveragePercent >= 80 ? 'bg-green-500' :
                      statistics.coveragePercent >= 50 ? 'bg-yellow-500' :
                      'bg-red-500'
                    }`}
                    style={{ width: `${statistics.coveragePercent}%` }}
                  />
                </div>
              </div>

              {/* Status Message */}
              <div className={`${getStatusBgColor(statistics.coveragePercent)} border ${
                statistics.coveragePercent >= 80 ? 'border-green-200' :
                statistics.coveragePercent >= 50 ? 'border-yellow-200' :
                'border-red-200'
              } rounded-lg p-4`}>
                <div className="flex items-start">
                  <div className="flex-shrink-0">
                    {statistics.coveragePercent >= 80 ? (
                      <svg className="h-5 w-5 text-green-600" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                      </svg>
                    ) : statistics.coveragePercent >= 50 ? (
                      <svg className="h-5 w-5 text-yellow-600" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                      </svg>
                    ) : (
                      <svg className="h-5 w-5 text-red-600" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                      </svg>
                    )}
                  </div>
                  <div className="ml-3">
                    <h3 className={`text-sm font-medium ${getStatusColor(statistics.coveragePercent)}`}>
                      {statistics.coveragePercent >= 80 ? 'Excellent Coverage' :
                       statistics.coveragePercent >= 50 ? 'Good Coverage' :
                       'Low Coverage'}
                    </h3>
                    <div className={`mt-1 text-sm ${getStatusColor(statistics.coveragePercent)}`}>
                      {statistics.coveragePercent >= 80 ? (
                        <p>Your system has excellent embedding coverage. Hybrid search is working at optimal performance.</p>
                      ) : statistics.coveragePercent >= 50 ? (
                        <p>Your system has good embedding coverage. Hybrid search is active but could be improved by generating embeddings for remaining {statistics.totalCandidates - statistics.withEmbeddings} candidates.</p>
                      ) : (
                        <p>Low embedding coverage detected. {statistics.totalCandidates - statistics.withEmbeddings} candidates are missing embeddings. Consider running the embedding generation service.</p>
                      )}
                    </div>
                  </div>
                </div>
              </div>

              {/* Search Strategy Info */}
              <div className="bg-gray-50 rounded-lg p-4">
                <h3 className="text-sm font-medium text-gray-900 mb-2">Current Search Strategy</h3>
                <div className="text-sm text-gray-600 space-y-1">
                  {statistics.coveragePercent >= 30 ? (
                    <>
                      <p>✓ <span className="font-medium">Hybrid Search</span> - Active (combines semantic understanding + keyword matching)</p>
                      <p className="ml-4 text-xs">60% semantic weight, 40% keyword weight</p>
                    </>
                  ) : statistics.coveragePercent > 0 ? (
                    <>
                      <p>⚠ <span className="font-medium">Semantic Search</span> - Limited (pure vector similarity)</p>
                      <p className="ml-4 text-xs">Coverage below 30% threshold for hybrid search</p>
                    </>
                  ) : (
                    <>
                      <p>⚠ <span className="font-medium">Basic Search</span> - Fallback (simple string matching)</p>
                      <p className="ml-4 text-xs">No embeddings available for semantic search</p>
                    </>
                  )}
                </div>
              </div>
            </div>
          ) : null}
        </div>

        {/* Search Scoring Strategy Card */}
        <div className="bg-white rounded-lg shadow-md p-6">
          <div className="flex items-center gap-2 mb-4">
            <svg className="w-6 h-6 text-blue-600" fill="currentColor" viewBox="0 0 20 20">
              <path d="M5 3a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2V5a2 2 0 00-2-2H5zM5 11a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2v-2a2 2 0 00-2-2H5zM11 5a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V5zM14 11a1 1 0 011 1v1h1a1 1 0 110 2h-1v1a1 1 0 11-2 0v-1h-1a1 1 0 110-2h1v-1a1 1 0 011-1z" />
            </svg>
            <h2 className="text-xl font-semibold text-gray-900">Search Scoring Strategy</h2>
          </div>

          <p className="text-sm text-gray-600 mb-4">
            Choose how candidate match scores are calculated when searching with multiple keywords.
          </p>

          {scoringStrategyError && (
            <div className="mb-4 bg-red-50 border border-red-200 rounded-lg p-3">
              <p className="text-sm text-red-700">{scoringStrategyError}</p>
            </div>
          )}

          {scoringConfig ? (
            <div className="space-y-4">
              {/* Option 1 */}
              <label 
                className="flex items-start p-4 border-2 rounded-lg cursor-pointer transition-all hover:bg-gray-50"
                style={{
                  borderColor: scoringConfig.scoringStrategy === 'option1' ? '#2563eb' : '#e5e7eb',
                  backgroundColor: scoringConfig.scoringStrategy === 'option1' ? '#eff6ff' : 'white'
                }}
              >
                <input
                  type="radio"
                  name="scoringStrategy"
                  value="option1"
                  checked={scoringConfig.scoringStrategy === 'option1'}
                  onChange={() => handleScoringStrategyChange('option1')}
                  disabled={updatingScoringStrategy}
                  className="mt-1 mr-3"
                />
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <span className="font-semibold text-gray-900">Option 1: All-or-Nothing</span>
                    {scoringConfig.scoringStrategy === 'option1' && (
                      <span className="px-2 py-0.5 bg-blue-600 text-white text-xs rounded-full">Active</span>
                    )}
                  </div>
                  <p className="text-sm text-gray-600 mt-1">
                    {availableStrategies['option1'] || 'If ALL keywords match, score is 100%. Otherwise, uses semantic similarity only.'}
                  </p>
                  <div className="mt-2 text-xs text-gray-500 bg-gray-50 rounded p-2">
                    <strong>Example:</strong> Search "Kubernetes Yugabyte"
                    <ul className="list-disc ml-5 mt-1 space-y-0.5">
                      <li>Both found → 100% match ⭐</li>
                      <li>Only one found → 55% (semantic score only)</li>
                      <li>Neither found → 35% (semantic score only)</li>
                    </ul>
                    <p className="mt-1.5 italic">Best for strict requirements where all skills are mandatory.</p>
                  </div>
                </div>
              </label>

              {/* Option 4 */}
              <label 
                className="flex items-start p-4 border-2 rounded-lg cursor-pointer transition-all hover:bg-gray-50"
                style={{
                  borderColor: scoringConfig.scoringStrategy === 'option4' ? '#2563eb' : '#e5e7eb',
                  backgroundColor: scoringConfig.scoringStrategy === 'option4' ? '#eff6ff' : 'white'
                }}
              >
                <input
                  type="radio"
                  name="scoringStrategy"
                  value="option4"
                  checked={scoringConfig.scoringStrategy === 'option4'}
                  onChange={() => handleScoringStrategyChange('option4')}
                  disabled={updatingScoringStrategy}
                  className="mt-1 mr-3"
                />
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <span className="font-semibold text-gray-900">Option 4: Tiered Multi-Keyword</span>
                    {scoringConfig.scoringStrategy === 'option4' && (
                      <span className="px-2 py-0.5 bg-blue-600 text-white text-xs rounded-full">Active</span>
                    )}
                  </div>
                  <p className="text-sm text-gray-600 mt-1">
                    {availableStrategies['option4'] || 'Balanced scoring based on keyword coverage and quality. Rewards partial matches.'}
                  </p>
                  <div className="mt-2 text-xs text-gray-500 bg-gray-50 rounded p-2">
                    <strong>Example:</strong> Search "Kubernetes Yugabyte PostgreSQL"
                    <ul className="list-disc ml-5 mt-1 space-y-0.5">
                      <li>All 3 found → 85-100% (based on expertise level) ⭐⭐⭐</li>
                      <li>2 of 3 found → 50-85% (balanced scoring) ⭐⭐</li>
                      <li>1 of 3 found → 40-60% (semantic focused) ⭐</li>
                      <li>0 of 3 found → 20-50% (pure semantic)</li>
                    </ul>
                    <p className="mt-1.5 italic">Best for flexible requirements where partial matches add value.</p>
                  </div>
                </div>
              </label>

              {updatingScoringStrategy && (
                <div className="flex items-center justify-center py-2">
                  <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600 mr-2"></div>
                  <span className="text-sm text-gray-600">Updating strategy...</span>
                </div>
              )}
            </div>
          ) : (
            <div className="flex items-center justify-center py-8">
              <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600"></div>
              <span className="ml-3 text-gray-600">Loading configuration...</span>
            </div>
          )}
        </div>

        {/* Privacy & PII Settings Card */}
        <div className="bg-white rounded-lg shadow-md p-6">
          <div className="flex items-center gap-2 mb-4">
            <svg className="w-6 h-6 text-indigo-600" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clipRule="evenodd" />
            </svg>
            <h2 className="text-xl font-semibold text-gray-900">Privacy & PII Settings</h2>
          </div>

          <p className="text-sm text-gray-600 mb-4">
            Configure how personally identifiable information (PII) is handled in candidate profiles and resumes.
          </p>

          {privacyConfigError && (
            <div className="mb-4 bg-red-50 border border-red-200 rounded-lg p-3">
              <p className="text-sm text-red-700">{privacyConfigError}</p>
            </div>
          )}

          {privacyConfig ? (
            <div className="space-y-6">
              {/* PII Sanitization Toggle */}
              <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <h3 className="font-semibold text-gray-900">PII Sanitization</h3>
                    <span className={`px-2 py-0.5 text-xs rounded-full ${
                      privacyConfig.piiSanitizationEnabled 
                        ? 'bg-green-100 text-green-800' 
                        : 'bg-red-100 text-red-800'
                    }`}>
                      {privacyConfig.piiSanitizationEnabled ? 'Enabled' : 'Disabled'}
                    </span>
                  </div>
                  <p className="text-sm text-gray-600 mt-1">
                    Automatically remove or mask emails, phone numbers, addresses, and names from imported data
                  </p>
                </div>
                <label className="relative inline-flex items-center cursor-pointer ml-4">
                  <input
                    type="checkbox"
                    checked={privacyConfig.piiSanitizationEnabled}
                    onChange={(e) => handlePiiToggle(e.target.checked)}
                    disabled={updatingPrivacyConfig}
                    className="sr-only peer"
                  />
                  <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                </label>
              </div>

              {/* Sanitization Level */}
              {privacyConfig.piiSanitizationEnabled && (
                <div className="space-y-3">
                  <h3 className="font-semibold text-gray-900">Sanitization Level</h3>
                  <div className="space-y-2">
                    {[
                      { value: 'minimal', label: 'Minimal', description: 'Remove email addresses only' },
                      { value: 'standard', label: 'Standard', description: 'Remove emails and phone numbers' },
                      { value: 'full', label: 'Full', description: 'Remove emails, phones, addresses, and personal names' }
                    ].map((level) => (
                      <label 
                        key={level.value}
                        className="flex items-start p-3 border rounded-lg cursor-pointer hover:bg-gray-50 transition-colors"
                        style={{
                          borderColor: privacyConfig.piiSanitizationLevel === level.value ? '#2563eb' : '#e5e7eb',
                          backgroundColor: privacyConfig.piiSanitizationLevel === level.value ? '#eff6ff' : 'white'
                        }}
                      >
                        <input
                          type="radio"
                          name="piiLevel"
                          value={level.value}
                          checked={privacyConfig.piiSanitizationLevel === level.value}
                          onChange={() => handlePiiLevelChange(level.value as 'minimal' | 'standard' | 'full')}
                          disabled={updatingPrivacyConfig}
                          className="mt-1 mr-3"
                        />
                        <div className="flex-1">
                          <div className="flex items-center gap-2">
                            <span className="font-medium text-gray-900">{level.label}</span>
                            {privacyConfig.piiSanitizationLevel === level.value && (
                              <span className="px-2 py-0.5 bg-blue-600 text-white text-xs rounded-full">Active</span>
                            )}
                          </div>
                          <p className="text-sm text-gray-600 mt-1">{level.description}</p>
                        </div>
                      </label>
                    ))}
                  </div>
                </div>
              )}

              {/* Logging Toggle */}
              <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                <div className="flex-1">
                  <h3 className="font-semibold text-gray-900">Log PII Removals</h3>
                  <p className="text-sm text-gray-600 mt-1">
                    Keep audit logs when PII data is removed or sanitized
                  </p>
                </div>
                <label className="relative inline-flex items-center cursor-pointer ml-4">
                  <input
                    type="checkbox"
                    checked={privacyConfig.logPiiRemovals || false}
                    onChange={(e) => {
                      if (privacyConfig) {
                        const updatedConfig = {
                          ...privacyConfig,
                          logPiiRemovals: e.target.checked
                        };
                        clientConfigApi.updatePrivacyConfig(updatedConfig);
                        setPrivacyConfig(updatedConfig);
                      }
                    }}
                    disabled={updatingPrivacyConfig}
                    className="sr-only peer"
                  />
                  <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                </label>
              </div>

              {updatingPrivacyConfig && (
                <div className="flex items-center justify-center py-2">
                  <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-indigo-600 mr-2"></div>
                  <span className="text-sm text-gray-600">Updating privacy settings...</span>
                </div>
              )}

              {/* Info Panel */}
              <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                <div className="flex items-start">
                  <svg className="h-5 w-5 text-blue-600 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                  </svg>
                  <div className="ml-3">
                    <h3 className="text-sm font-medium text-blue-800">Privacy Information</h3>
                    <ul className="mt-1 text-sm text-blue-700 space-y-1">
                      <li>• PII sanitization applies to new Excel imports and data processing</li>
                      <li>• Existing candidate data is not automatically modified</li>
                      <li>• Sanitization helps protect candidate privacy and comply with data regulations</li>
                      <li>• Logs can help track data handling for compliance audits</li>
                    </ul>
                  </div>
                </div>
              </div>
            </div>
          ) : (
            <div className="flex items-center justify-center py-8">
              <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-indigo-600"></div>
              <span className="ml-3 text-gray-600">Loading privacy configuration...</span>
            </div>
          )}
        </div>

        {/* Search Mode Preference Card */}
        <div className="bg-white rounded-lg shadow-md p-6">
          <div className="flex items-center gap-2 mb-4">
            <svg className="w-6 h-6 text-green-600" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clipRule="evenodd" />
            </svg>
            <h2 className="text-xl font-semibold text-gray-900">Search Mode Preference</h2>
          </div>

          <p className="text-sm text-gray-600 mb-4">
            Choose your preferred search engine for candidate queries. Each mode is optimized for different use cases.
          </p>

          <div className="space-y-4">
            {/* Semantic Search Mode */}
            <label 
              className="flex items-start p-4 border-2 rounded-lg cursor-pointer transition-all hover:bg-gray-50"
              style={{
                borderColor: searchMode === 'semantic' ? '#2563eb' : '#e5e7eb',
                backgroundColor: searchMode === 'semantic' ? '#eff6ff' : 'white'
              }}
            >
              <input
                type="radio"
                name="searchMode"
                value="semantic"
                checked={searchMode === 'semantic'}
                onChange={() => handleSearchModeChange('semantic')}
                disabled={updatingSearchMode}
                className="mt-1 mr-3"
              />
              <div className="flex-1">
                <div className="flex items-center gap-2">
                  <span className="font-semibold text-gray-900">Semantic Search</span>
                  {searchMode === 'semantic' ? (
                    <span className="px-2 py-0.5 bg-blue-600 text-white text-xs rounded-full">Active</span>
                  ) : (
                    <span className="px-2 py-0.5 bg-gray-600 text-white text-xs rounded-full">Default</span>
                  )}
                </div>
                <p className="text-sm text-gray-600 mt-1">
                  AI-powered search that understands meaning and context. Best for skills, technologies, and job descriptions.
                </p>
                <div className="mt-2 text-xs text-gray-500 bg-white/70 rounded p-2">
                  <strong>Best for:</strong> "React developer with 5 years experience", "Machine learning engineer", "Backend JavaScript"
                  <br />
                  <strong>Features:</strong> ✓ Understanding context ✓ Synonym matching ✓ Skill relationships
                </div>
              </div>
            </label>

            {/* Name Match Mode */}
            <label 
              className="flex items-start p-4 border-2 rounded-lg cursor-pointer transition-all hover:bg-gray-50"
              style={{
                borderColor: searchMode === 'nameMatch' ? '#2563eb' : '#e5e7eb',
                backgroundColor: searchMode === 'nameMatch' ? '#eff6ff' : 'white'
              }}
            >
              <input
                type="radio"
                name="searchMode"
                value="nameMatch"
                checked={searchMode === 'nameMatch'}
                onChange={() => handleSearchModeChange('nameMatch')}
                disabled={updatingSearchMode}
                className="mt-1 mr-3"
              />
              <div className="flex-1">
                <div className="flex items-center gap-2">
                  <span className="font-semibold text-gray-900">Name Match</span>
                  {searchMode === 'nameMatch' ? (
                    <span className="px-2 py-0.5 bg-blue-600 text-white text-xs rounded-full">Active</span>
                  ) : (
                    <span className="px-2 py-0.5 bg-green-600 text-white text-xs rounded-full">Fast</span>
                  )}
                </div>
                <p className="text-sm text-gray-600 mt-1">
                  Optimized for finding candidates by name or exact text matches. Ultra-fast full-text search.
                </p>
                <div className="mt-2 text-xs text-gray-500 bg-gray-50 rounded p-2">
                  <strong>Best for:</strong> "Steven Henn", "John Smith", "Microsoft", "Amazon Web Services"
                  <br />
                  <strong>Features:</strong> ✓ Exact name matching ✓ Company names ✓ Instant results ✓ Fuzzy matching
                </div>
              </div>
            </label>

            {/* Auto Mode */}
            <label 
              className="flex items-start p-4 border-2 rounded-lg cursor-pointer transition-all hover:bg-gray-50"
              style={{
                borderColor: searchMode === 'auto' ? '#2563eb' : '#e5e7eb',
                backgroundColor: searchMode === 'auto' ? '#eff6ff' : 'white'
              }}
            >
              <input
                type="radio"
                name="searchMode"
                value="auto"
                checked={searchMode === 'auto'}
                onChange={() => handleSearchModeChange('auto')}
                disabled={updatingSearchMode}
                className="mt-1 mr-3"
              />
              <div className="flex-1">
                <div className="flex items-center gap-2">
                  <span className="font-semibold text-gray-900">Auto-Detect</span>
                  {searchMode === 'auto' ? (
                    <span className="px-2 py-0.5 bg-blue-600 text-white text-xs rounded-full">Active</span>
                  ) : (
                    <span className="px-2 py-0.5 bg-purple-600 text-white text-xs rounded-full">Smart</span>
                  )}
                </div>
                <p className="text-sm text-gray-600 mt-1">
                  Automatically chooses the best search method based on your query. Names use name match, skills use semantic.
                </p>
                <div className="mt-2 text-xs text-gray-500 bg-gray-50 rounded p-2">
                  <strong>How it works:</strong> Detects names (2+ capitalized words) → Name Match. Otherwise → Semantic Search.
                  <br />
                  <strong>Examples:</strong> "John Doe" → Name Match, "React developer" → Semantic Search
                </div>
              </div>
            </label>
          </div>

          {/* Error Display */}
          {searchModeError && (
            <div className="mt-4 bg-red-50 border border-red-200 rounded-lg p-3">
              <p className="text-sm text-red-700">{searchModeError}</p>
            </div>
          )}

          {/* Loading State */}
          {updatingSearchMode && (
            <div className="mt-4 flex items-center justify-center py-2">
              <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600 mr-2"></div>
              <span className="text-sm text-gray-600">Updating search mode...</span>
            </div>
          )}

          <div className="mt-4 p-3 bg-amber-50 border border-amber-200 rounded-lg">
            <div className="flex items-start gap-2">
              <svg className="w-4 h-4 text-amber-600 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
              </svg>
              <div className="text-xs text-amber-700">
                <strong>Note:</strong> This setting only affects the main search. The scoring strategies above apply specifically to semantic search mode.
                Name Match mode uses PostgreSQL full-text search with ranking instead of similarity scoring.
              </div>
            </div>
          </div>
        </div>

        {/* System Administration Card */}
        <div className="bg-white rounded-lg shadow-md p-6 mb-6">
          <div className="mb-4">
            <h2 className="text-xl font-semibold text-gray-900">System Administration</h2>
            <p className="text-sm text-gray-500 mt-1">Advanced system maintenance operations</p>
          </div>

          {/* Full-Text Search Rebuild */}
          <div className="space-y-4">
            <div className="border border-gray-200 rounded-lg p-4">
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <h3 className="text-lg font-medium text-gray-900">Rebuild Full-Text Search</h3>
                  <p className="text-sm text-gray-600 mt-1">
                    Rebuilds the PostgreSQL Full-Text Search infrastructure including search vectors, 
                    triggers, materialized views, and indexes. Use this if search is not working properly.
                  </p>
                  
                  {ftsRebuildResult && (
                    <div className={`mt-3 p-3 rounded-md ${
                      ftsRebuildResult.success 
                        ? 'bg-green-50 border border-green-200' 
                        : 'bg-red-50 border border-red-200'
                    }`}>
                      <div className="flex items-start">
                        <svg 
                          className={`h-5 w-5 mt-0.5 ${
                            ftsRebuildResult.success ? 'text-green-600' : 'text-red-600'
                          }`} 
                          fill="currentColor" 
                          viewBox="0 0 20 20"
                        >
                          {ftsRebuildResult.success ? (
                            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                          ) : (
                            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                          )}
                        </svg>
                        <div className="ml-3">
                          <p className={`text-sm font-medium ${
                            ftsRebuildResult.success ? 'text-green-800' : 'text-red-800'
                          }`}>
                            {ftsRebuildResult.message}
                          </p>
                          {ftsRebuildResult.success && (
                            <p className="text-xs text-green-700 mt-1">
                              Processed {ftsRebuildResult.processedItems} candidates in {Math.round(ftsRebuildResult.durationMs)}ms
                            </p>
                          )}
                        </div>
                      </div>
                    </div>
                  )}

                  {ftsRebuildError && (
                    <div className="mt-3 p-3 bg-red-50 border border-red-200 rounded-md">
                      <div className="flex items-start">
                        <svg className="h-5 w-5 text-red-600 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                          <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                        </svg>
                        <p className="ml-3 text-sm text-red-800">{ftsRebuildError}</p>
                      </div>
                    </div>
                  )}
                </div>
                
                <button
                  onClick={handleRebuildFts}
                  disabled={rebuildingFts}
                  className="ml-4 flex-shrink-0 px-4 py-2 bg-orange-600 text-white rounded-md hover:bg-orange-700 disabled:opacity-50 transition-colors"
                >
                  {rebuildingFts ? (
                    <span className="flex items-center gap-2">
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                      Rebuilding...
                    </span>
                  ) : (
                    'Rebuild FTS'
                  )}
                </button>
              </div>
            </div>

            {/* Info Panel */}
            <div className="bg-amber-50 border border-amber-200 rounded-lg p-4">
              <div className="flex items-start">
                <svg className="h-5 w-5 text-amber-600 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                </svg>
                <div className="ml-3">
                  <h3 className="text-sm font-medium text-amber-800">Administration Warning</h3>
                  <ul className="mt-1 text-sm text-amber-700 space-y-1">
                    <li>• This operation rebuilds core search infrastructure and may take several seconds</li>
                    <li>• Search functionality may be temporarily unavailable during rebuild</li>
                    <li>• Only use this if search is not working or after major database changes</li>
                    <li>• The rebuild will process all existing candidates and create search indexes</li>
                  </ul>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Excel Upload Card */}
        <div className="bg-white rounded-lg shadow-md p-6">
          <div className="mb-4">
            <h2 className="text-xl font-semibold text-gray-900">Import Candidates from Excel</h2>
            <p className="text-sm text-gray-500 mt-1">Upload an Excel file to import candidate data</p>
          </div>

          {/* File Selection */}
          <div className="space-y-4">
            <div className="flex items-center gap-3">
              <label
                htmlFor="excel-file"
                className="flex-shrink-0 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 cursor-pointer transition-colors"
              >
                Choose File
              </label>
              <input
                id="excel-file"
                type="file"
                accept=".xlsx,.xls"
                onChange={handleFileSelect}
                className="hidden"
              />
              {selectedFile && (
                <div className="flex items-center gap-2 flex-1">
                  <span className="text-sm text-gray-700 truncate">{selectedFile.name}</span>
                  <span className="text-xs text-gray-500">({(selectedFile.size / 1024).toFixed(2)} KB)</span>
                  <button
                    onClick={handleClearUpload}
                    className="ml-auto text-gray-500 hover:text-gray-700"
                  >
                    <svg className="h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
                      <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
                    </svg>
                  </button>
                </div>
              )}
            </div>

            {/* Upload Button */}
            {selectedFile && (
              <button
                onClick={handleUpload}
                disabled={uploading}
                className="w-full px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {uploading ? (
                  <span className="flex items-center justify-center gap-2">
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                    Uploading...
                  </span>
                ) : (
                  'Upload and Import'
                )}
              </button>
            )}

            {/* Upload Error */}
            {uploadError && (
              <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                <div className="flex items-start">
                  <svg className="h-5 w-5 text-red-600 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                  </svg>
                  <div className="ml-3">
                    <h3 className="text-sm font-medium text-red-800">Upload Failed</h3>
                    <p className="text-sm text-red-700 mt-1">{uploadError}</p>
                  </div>
                </div>
              </div>
            )}

            {/* Upload Success Result */}
            {uploadResult && uploadResult.success && (
              <div className="bg-green-50 border border-green-200 rounded-lg p-4">
                <div className="flex items-start">
                  <svg className="h-5 w-5 text-green-600 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                  </svg>
                  <div className="ml-3 flex-1">
                    <h3 className="text-sm font-medium text-green-800">Upload Successful</h3>
                    <p className="text-sm text-green-700 mt-1">{uploadResult.message}</p>
                    <div className="mt-3 grid grid-cols-3 gap-4 text-center">
                      <div className="bg-white rounded p-2">
                        <div className="text-lg font-bold text-green-900">{uploadResult.processedRows}</div>
                        <div className="text-xs text-green-600">Rows Processed</div>
                      </div>
                      <div className="bg-white rounded p-2">
                        <div className="text-lg font-bold text-green-900">{uploadResult.importedCandidates}</div>
                        <div className="text-xs text-green-600">Candidates Imported</div>
                      </div>
                      <div className="bg-white rounded p-2">
                        <div className="text-lg font-bold text-red-900">{uploadResult.errorCount}</div>
                        <div className="text-xs text-red-600">Errors</div>
                      </div>
                    </div>
                    {uploadResult.errors.length > 0 && (
                      <details className="mt-3">
                        <summary className="text-sm text-green-700 cursor-pointer hover:text-green-800">
                          View {uploadResult.errors.length} error(s)
                        </summary>
                        <ul className="mt-2 space-y-1 text-xs text-red-600 max-h-40 overflow-y-auto">
                          {uploadResult.errors.map((error, idx) => (
                            <li key={idx} className="pl-4">• {error}</li>
                          ))}
                        </ul>
                      </details>
                    )}
                  </div>
                </div>
              </div>
            )}

            {/* Upload Failed Result */}
            {uploadResult && !uploadResult.success && (
              <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                <div className="flex items-start">
                  <svg className="h-5 w-5 text-red-600 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                  </svg>
                  <div className="ml-3 flex-1">
                    <h3 className="text-sm font-medium text-red-800">Import Failed</h3>
                    <p className="text-sm text-red-700 mt-1">{uploadResult.message}</p>
                    {uploadResult.errors.length > 0 && (
                      <ul className="mt-2 space-y-1 text-xs text-red-600 max-h-40 overflow-y-auto">
                        {uploadResult.errors.map((error, idx) => (
                          <li key={idx} className="pl-4">• {error}</li>
                        ))}
                      </ul>
                    )}
                  </div>
                </div>
              </div>
            )}

            {/* Info Box */}
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <div className="flex items-start">
                <svg className="h-5 w-5 text-blue-600 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                </svg>
                <div className="ml-3">
                  <h3 className="text-sm font-medium text-blue-800">Import Guidelines</h3>
                  <ul className="mt-1 text-sm text-blue-700 space-y-1">
                    <li>• Excel file should be in .xlsx or .xls format</li>
                    <li>• File should contain candidate information with appropriate columns</li>
                    <li>• Duplicate candidates will be automatically handled</li>
                    <li>• Statistics will be automatically refreshed after successful import</li>
                  </ul>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Layout>
  );
};

export default Settings;
