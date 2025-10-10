import { fetchAPI } from './api';

export interface ClientConfigDto {
  id: string;
  clientId: string;
  configKey: string;
  configValue: string;
  configType: string;
  description?: string;
  createdAt: string;
  updatedAt: string;
}

export interface SearchScoringConfigDto {
  clientId: string;
  scoringStrategy: 'option1' | 'option4';
  semanticWeight: number;
  keywordWeight: number;
  similarityThreshold: number;
}

export interface UpdateConfigRequest {
  configKey: string;
  configValue: string;
}

export const clientConfigApi = {
  /**
   * Get all configurations for a client
   * @param clientId - Optional client ID (defaults to GLOBAL on backend)
   */
  getAllConfigs: async (clientId?: string): Promise<ClientConfigDto[]> => {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };
    if (clientId) {
      headers['X-Client-ID'] = clientId;
    }
    return fetchAPI('/api/clientconfig', { headers });
  },

  /**
   * Get specific configuration by key
   * @param key - Configuration key
   * @param clientId - Optional client ID (defaults to GLOBAL on backend)
   */
  getConfig: async (key: string, clientId?: string): Promise<ClientConfigDto> => {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };
    if (clientId) {
      headers['X-Client-ID'] = clientId;
    }
    return fetchAPI(`/api/clientconfig/${key}`, { headers });
  },

  /**
   * Update or create configuration
   * @param request - Configuration update request
   * @param clientId - Optional client ID (defaults to GLOBAL on backend)
   */
  upsertConfig: async (
    request: UpdateConfigRequest,
    clientId?: string
  ): Promise<ClientConfigDto> => {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };
    if (clientId) {
      headers['X-Client-ID'] = clientId;
    }
    return fetchAPI('/api/clientconfig', {
      method: 'POST',
      headers,
      body: JSON.stringify(request),
    });
  },

  /**
   * Get search scoring configuration
   * @param clientId - Optional client ID (defaults to GLOBAL on backend)
   */
  getSearchScoringConfig: async (clientId?: string): Promise<SearchScoringConfigDto> => {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };
    if (clientId) {
      headers['X-Client-ID'] = clientId;
    }
    return fetchAPI('/api/clientconfig/search/scoring', { headers });
  },

  /**
   * Get available scoring strategies with descriptions
   */
  getScoringStrategies: async (): Promise<Record<string, string>> => {
    return fetchAPI('/api/clientconfig/search/strategies');
  },

  /**
   * Update scoring strategy
   * @param strategy - Scoring strategy (option1 or option4)
   * @param clientId - Optional client ID (defaults to GLOBAL on backend)
   */
  updateScoringStrategy: async (
    strategy: 'option1' | 'option4',
    clientId?: string
  ): Promise<ClientConfigDto> => {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };
    if (clientId) {
      headers['X-Client-ID'] = clientId;
    }
    return fetchAPI('/api/clientconfig', {
      method: 'POST',
      headers,
      body: JSON.stringify({
        configKey: 'search.scoring_strategy',
        configValue: strategy,
      }),
    });
  },
};
