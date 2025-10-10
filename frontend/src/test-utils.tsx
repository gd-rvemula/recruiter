// This is a test utility file to provide mock router context for tests
import * as React from 'react';
import { render, RenderOptions, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import '@testing-library/jest-dom';

const mockUseLocationValue = { pathname: '/time-entries' };

// Mock React Router hooks
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useLocation: () => mockUseLocationValue,
  useNavigate: () => jest.fn(),
}));

// Create a custom render utility that includes providers
const customRender = (
  ui: React.ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>,
) => {
  const Wrapper: React.FC<{children: React.ReactNode}> = ({ children }) => {
    return <BrowserRouter>{children}</BrowserRouter>;
  };
  
  return render(ui, { wrapper: Wrapper, ...options });
};

// Re-export everything from testing-library
export * from '@testing-library/react';

// Override render method
export { customRender as render };
