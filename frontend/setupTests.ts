import '@testing-library/jest-dom';

// Define process.env variables needed for tests
process.env.VITE_API_URL = 'http://localhost:3001';
process.env.MODE = 'test';

// Make import.meta.env available globally for tests
(global as any).import = {
  meta: {
    env: {
      VITE_API_URL: 'http://localhost:3001',
      MODE: 'test'
    }
  }
};

// Use a function to create mock functions to avoid reference issues between tests
function createMockFunctions() {
  return {
    get: jest.fn(),
    post: jest.fn(),
    put: jest.fn(),
    patch: jest.fn(),
    delete: jest.fn(),
    // API endpoints can be added here as needed
    getAll: jest.fn(),
    getById: jest.fn(),
    create: jest.fn(),
    update: jest.fn(),
    remove: jest.fn()
  };
}

// Mock the API service directly in setupTests
jest.mock('./src/services/api', () => createMockFunctions());