import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import Card from '../Card';

describe('Card Component', () => {
  test('renders children correctly', () => {
    render(<Card>Card Content</Card>);
    expect(screen.getByText('Card Content')).toBeInTheDocument();
  });

  test('renders title when provided', () => {
    render(<Card title="Card Title">Card Content</Card>);
    expect(screen.getByText('Card Title')).toBeInTheDocument();
    expect(screen.getByText('Card Content')).toBeInTheDocument();
  });

  test('applies custom className when provided', () => {
    render(<Card className="custom-class">Card Content</Card>);
    const card = screen.getByText('Card Content').closest('.bg-white');
    expect(card).toHaveClass('custom-class');
  });
});
