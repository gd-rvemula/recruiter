import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import Button from '../Button';

describe('Button Component', () => {
  test('renders correctly with default props', () => {
    render(<Button>Click Me</Button>);
    
    const button = screen.getByText('Click Me');
    expect(button).toBeInTheDocument();
    expect(button).toHaveClass('bg-blue-600'); // primary variant by default
    expect(button).toHaveClass('px-4 py-2'); // medium size by default
    expect(button).not.toHaveClass('w-full'); // not full width by default
    expect(button).not.toBeDisabled();
  });

  test('applies different variants correctly', () => {
    const { rerender } = render(<Button variant="primary">Primary</Button>);
    expect(screen.getByText('Primary')).toHaveClass('bg-blue-600');
    
    rerender(<Button variant="secondary">Secondary</Button>);
    expect(screen.getByText('Secondary')).toHaveClass('bg-gray-200');
    
    rerender(<Button variant="success">Success</Button>);
    expect(screen.getByText('Success')).toHaveClass('bg-green-600');
    
    rerender(<Button variant="danger">Danger</Button>);
    expect(screen.getByText('Danger')).toHaveClass('bg-red-600');
    
    rerender(<Button variant="warning">Warning</Button>);
    expect(screen.getByText('Warning')).toHaveClass('bg-yellow-500');
    
    rerender(<Button variant="info">Info</Button>);
    expect(screen.getByText('Info')).toHaveClass('bg-indigo-600');
  });

  test('applies different sizes correctly', () => {
    const { rerender } = render(<Button size="sm">Small</Button>);
    expect(screen.getByText('Small')).toHaveClass('px-3 py-1.5 text-sm');
    
    rerender(<Button size="md">Medium</Button>);
    expect(screen.getByText('Medium')).toHaveClass('px-4 py-2 text-base');
    
    rerender(<Button size="lg">Large</Button>);
    expect(screen.getByText('Large')).toHaveClass('px-6 py-3 text-lg');
  });

  test('renders as full width when specified', () => {
    render(<Button fullWidth>Full Width</Button>);
    expect(screen.getByText('Full Width')).toHaveClass('w-full');
  });

  test('disables the button when disabled prop is true', () => {
    render(<Button disabled>Disabled</Button>);
    const button = screen.getByText('Disabled');
    expect(button).toBeDisabled();
    expect(button).toHaveClass('opacity-60');
    expect(button).toHaveClass('cursor-not-allowed');
  });

  test('shows loading spinner when isLoading is true', () => {
    render(<Button isLoading>Loading</Button>);
    
    // Check if the loading spinner (SVG) is rendered
    const loadingSpinner = document.querySelector('svg');
    expect(loadingSpinner).toBeInTheDocument();
    
    // Button should be disabled while loading
    const button = screen.getByText('Loading');
    expect(button).toBeDisabled();
    expect(button).toHaveClass('opacity-60');
  });

  test('applies additional className when provided', () => {
    render(<Button className="custom-class">Custom Class</Button>);
    expect(screen.getByText('Custom Class')).toHaveClass('custom-class');
  });

  test('calls onClick handler when clicked', () => {
    const handleClick = jest.fn();
    render(<Button onClick={handleClick}>Click Me</Button>);
    
    fireEvent.click(screen.getByText('Click Me'));
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  test('does not call onClick when disabled', () => {
    const handleClick = jest.fn();
    render(<Button onClick={handleClick} disabled>Click Me</Button>);
    
    fireEvent.click(screen.getByText('Click Me'));
    expect(handleClick).not.toHaveBeenCalled();
  });

  test('does not call onClick when loading', () => {
    const handleClick = jest.fn();
    render(<Button onClick={handleClick} isLoading>Click Me</Button>);
    
    fireEvent.click(screen.getByText('Click Me'));
    expect(handleClick).not.toHaveBeenCalled();
  });
});