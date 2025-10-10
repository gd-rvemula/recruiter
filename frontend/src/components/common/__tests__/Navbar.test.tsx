import * as React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import Navbar from '../Navbar';
import { BrowserRouter } from 'react-router-dom';

// Mock useLocation to control the current path
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useLocation: () => ({ pathname: '/' })
}));

describe('Navbar', () => {
  test('renders the application name', () => {
    render(
      <BrowserRouter>
        <Navbar />
      </BrowserRouter>
    );
    
    expect(screen.getByText('Recruiter')).toBeInTheDocument();
  });

  test('renders navigation links', () => {
    render(
      <BrowserRouter>
        <Navbar />
      </BrowserRouter>
    );
    
    // Use the data-testid attributes we added to find the links
    expect(screen.getByTestId('nav-link-dashboard')).toBeInTheDocument();
    // Navigation links are rendered correctly
  });

  test('renders mobile menu button', () => {
    render(
      <BrowserRouter>
        <Navbar />
      </BrowserRouter>
    );
    
    // Find the menu button using a more reliable selector
    const menuButton = screen.getByRole('button', { name: /open main menu/i });
    expect(menuButton).toBeInTheDocument();
  });
});