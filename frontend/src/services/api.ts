/**
 * API service for the Recruiteration System
 * Handles all communication with the backend server
 */

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

/**
 * Fetch data from the API with proper error handling
 */
export async function fetchAPI<T>(
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
      throw new Error(
        errorData.message || `API request failed: ${response.status} ${response.statusText}`
      );
    }

    return await response.json();
  } catch (error) {
    console.error(`API Error: ${endpoint}`, error);
    throw error;
  }
}

/**
 * Get a list of resources
 */
export const getAll = async <T>(resource: string): Promise<T[]> => {
  return fetchAPI<T[]>(`/${resource}`);
};

/**
 * Get a resource by ID
 */
export const getById = async <T>(resource: string, id: string): Promise<T> => {
  return fetchAPI<T>(`/${resource}/${id}`);
};

/**
 * Create a new resource
 */
export const create = async <T>(resource: string, data: any): Promise<T> => {
  return fetchAPI<T>(`/${resource}`, {
    method: 'POST',
    body: JSON.stringify(data),
  });
};

/**
 * Update a resource
 */
export const update = async <T>(resource: string, id: string, data: any): Promise<T> => {
  return fetchAPI<T>(`/${resource}/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
};

/**
 * Patch a resource (partial update)
 */
export const patch = async <T>(resource: string, id: string, data: any): Promise<T> => {
  return fetchAPI<T>(`/${resource}/${id}`, {
    method: 'PATCH',
    body: JSON.stringify(data),
  });
};

/**
 * Delete a resource
 */
export const remove = async (resource: string, id: string): Promise<boolean> => {
  try {
    const response = await fetch(`${API_BASE_URL}/${resource}/${id}`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
      },
    });
    
    if (response.ok) {
      return true; // Return true to indicate success
    }
    
    throw new Error(`API request failed: ${response.status} ${response.statusText}`);
  } catch (error) {
    console.error(`Error removing ${resource} with ID ${id}:`, error);
    throw error;
  }
};

/**
 * Get filtered data
 */
export const getFiltered = async <T>(
  resource: string,
  filterParams: Record<string, string | number | boolean>
): Promise<T[]> => {
  const queryParams = new URLSearchParams();
  
  Object.entries(filterParams).forEach(([key, value]) => {
    if (value !== undefined && value !== null) {
      queryParams.append(key, value.toString());
    }
  });
  
  const queryString = queryParams.toString();
  const endpoint = `/${resource}${queryString ? `?${queryString}` : ''}`;
  
  return fetchAPI<T[]>(endpoint);
};

/**
 * Resource-specific API functions
 */

// API endpoints can be added here as needed

export default {
  fetchAPI,
  getAll,
  getById,
  create,
  update,
  patch,
  remove,
  getFiltered,
};