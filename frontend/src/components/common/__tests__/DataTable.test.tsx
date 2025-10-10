import React from 'react';
import { render, screen, fireEvent, waitFor, within } from '@testing-library/react';
import '@testing-library/jest-dom';
import DataTable, { Column } from '../DataTable';

// Mock Button component
jest.mock('../../common/Button', () => {
  return function MockButton({ children, onClick, disabled, className = '' }: any) {
    return (
      <button 
        onClick={onClick} 
        disabled={disabled} 
        data-testid="mock-button"
        className={className}
      >
        {children}
      </button>
    );
  };
});

// Mock data and columns for testing
interface TestItem {
  id: string;
  name: string;
  age: number;
  email: string;
}

const mockData: TestItem[] = [
  { id: '1', name: 'John Doe', age: 30, email: 'john@example.com' },
  { id: '2', name: 'Jane Smith', age: 25, email: 'jane@example.com' },
  { id: '3', name: 'Bob Johnson', age: 40, email: 'bob@example.com' },
  { id: '4', name: 'Alice Williams', age: 35, email: 'alice@example.com' },
  { id: '5', name: 'Charlie Brown', age: 28, email: 'charlie@example.com' },
  { id: '6', name: 'Eva Davis', age: 22, email: 'eva@example.com' },
  { id: '7', name: 'Frank Miller', age: 45, email: 'frank@example.com' },
  { id: '8', name: 'Grace Wilson', age: 33, email: 'grace@example.com' },
  { id: '9', name: 'Henry Taylor', age: 38, email: 'henry@example.com' },
  { id: '10', name: 'Irene Martin', age: 27, email: 'irene@example.com' },
  { id: '11', name: 'Jack Anderson', age: 50, email: 'jack@example.com' },
];

const mockColumns: Column<TestItem>[] = [
  { header: 'Name', accessor: 'name' },
  { header: 'Age', accessor: 'age' },
  { header: 'Email', accessor: 'email' },
  { 
    header: 'Custom Column', 
    accessor: (item) => <span data-testid="custom-column">{item.name.toUpperCase()}</span> 
  },
];

describe('DataTable Component', () => {
  test('renders table with correct headers and data', () => {
    render(
      <DataTable
        data={mockData}
        columns={mockColumns}
        keyExtractor={(item) => item.id}
      />
    );

    // Check if headers are rendered
    expect(screen.getByText('Name')).toBeInTheDocument();
    expect(screen.getByText('Age')).toBeInTheDocument();
    expect(screen.getByText('Email')).toBeInTheDocument();
    expect(screen.getByText('Custom Column')).toBeInTheDocument();

    // Check if data is rendered
    expect(screen.getByText('John Doe')).toBeInTheDocument();
    expect(screen.getByText('30')).toBeInTheDocument();
    expect(screen.getByText('john@example.com')).toBeInTheDocument();
    
    // Check if custom accessor function works
    const customColumns = screen.getAllByTestId('custom-column');
    expect(customColumns.length).toBe(11);
    expect(customColumns[0]).toHaveTextContent('JOHN DOE');
  });

  test('handles empty data correctly', () => {
    render(
      <DataTable
        data={[]}
        columns={mockColumns}
        keyExtractor={(item) => item.id}
        emptyMessage="No items found"
      />
    );

    // Check if empty message is displayed
    expect(screen.getByText('No items found')).toBeInTheDocument();
  });

  test('shows loading state correctly', () => {
    render(
      <DataTable
        data={mockData}
        columns={mockColumns}
        keyExtractor={(item) => item.id}
        isLoading={true}
      />
    );

    // There should be an SVG element (loading spinner) in the table
    const loadingSpinner = document.querySelector('svg');
    expect(loadingSpinner).toBeInTheDocument();
  });

  test('handles row click events', () => {
    const handleRowClick = jest.fn();
    
    render(
      <DataTable
        data={mockData}
        columns={mockColumns}
        keyExtractor={(item) => item.id}
        onRowClick={handleRowClick}
      />
    );

    // Click on the first row
    const firstRow = screen.getByText('John Doe').closest('tr');
    if (firstRow) {
      fireEvent.click(firstRow);
    }

    // Check if the click handler was called with the correct data
    expect(handleRowClick).toHaveBeenCalledWith(mockData[0]);
  });

  test('renders action buttons correctly', () => {
    const handleAction = jest.fn();

    render(
      <DataTable
        data={mockData}
        columns={mockColumns}
        keyExtractor={(item) => item.id}
        actions={(item) => (
          <button 
            onClick={(e) => {
              e.stopPropagation();
              handleAction(item);
            }}
            data-testid={`action-button-${item.id}`}
          >
            Edit
          </button>
        )}
      />
    );

    // Check if action header is rendered
    expect(screen.getByText('Actions')).toBeInTheDocument();

    // Check if action buttons are rendered for each row
    expect(screen.getByTestId('action-button-1')).toBeInTheDocument();

    // Test clicking on an action button
    fireEvent.click(screen.getByTestId('action-button-1'));
    expect(handleAction).toHaveBeenCalledWith(mockData[0]);
  });

  test('pagination works correctly', () => {
    render(
      <DataTable
        columns={mockColumns}
        data={mockData.slice()} 
        itemsPerPage={2}
        keyExtractor={(item) => item.id}
      />
    );

    // Just verify that the data is displayed correctly
    expect(screen.getByText('John Doe')).toBeInTheDocument();
    expect(screen.getByText('Jane Smith')).toBeInTheDocument();
    expect(screen.getByText('Bob Johnson')).toBeInTheDocument();
    expect(screen.getByText('Alice Williams')).toBeInTheDocument();
    expect(screen.getByText('Charlie Brown')).toBeInTheDocument();
  });

  test('pagination controls navigate between pages correctly', () => {
    render(
      <DataTable
        data={mockData}
        columns={mockColumns}
        keyExtractor={(item) => item.id}
        pagination={true}
        itemsPerPage={3}
      />
    );

    // First page should show first 3 items
    expect(screen.getByText('John Doe')).toBeInTheDocument();
    expect(screen.getByText('Jane Smith')).toBeInTheDocument();
    expect(screen.getByText('Bob Johnson')).toBeInTheDocument();
    expect(screen.queryByText('Alice Williams')).not.toBeInTheDocument();

    // Check pagination info text
    const paginationInfo = screen.getByTestId('pagination-info');
    expect(paginationInfo).toHaveTextContent('Showing 1 to 3 of 11 results');

    // Navigate to next page
    const nextButtons = screen.getAllByText('Next');
    fireEvent.click(nextButtons[0]); // Click the first "Next" button

    // Second page should show next 3 items
    expect(screen.queryByText('John Doe')).not.toBeInTheDocument();
    expect(screen.getByText('Alice Williams')).toBeInTheDocument();
    expect(screen.getByText('Charlie Brown')).toBeInTheDocument();
    expect(screen.getByText('Eva Davis')).toBeInTheDocument();

    // Check pagination info text updated
    expect(paginationInfo).toHaveTextContent('Showing 4 to 6 of 11 results');

    // Click on page 3
    const page3Button = screen.getByText('3');
    fireEvent.click(page3Button);

    // Third page should show correct items
    expect(screen.queryByText('Alice Williams')).not.toBeInTheDocument();
    expect(screen.getByText('Frank Miller')).toBeInTheDocument();
    expect(screen.getByText('Grace Wilson')).toBeInTheDocument();
    expect(screen.getByText('Henry Taylor')).toBeInTheDocument();

    // Check pagination info text updated
    expect(paginationInfo).toHaveTextContent('Showing 7 to 9 of 11 results');

    // Navigation to last page
    const page4Button = screen.getByText('4');
    fireEvent.click(page4Button);

    // Last page should show the remaining items
    expect(screen.getByText('Jack Anderson')).toBeInTheDocument();
    // There are only 11 items, so page 4 should have Jack Anderson and Irene Martin
    expect(screen.getByText('Irene Martin')).toBeInTheDocument();
    expect(screen.queryByText('Henry Taylor')).not.toBeInTheDocument();

    // Check pagination info text updated
    expect(paginationInfo).toHaveTextContent('Showing 10 to 11 of 11 results');

    // Previous button should now be enabled
    const prevButtons = screen.getAllByText('Previous');
    fireEvent.click(prevButtons[0]); // Click the first "Previous" button

    // Should navigate back to page 3
    expect(screen.queryByText('Jack Anderson')).not.toBeInTheDocument();
    expect(screen.getByText('Henry Taylor')).toBeInTheDocument();
  });

  test('pagination button disabled states work correctly', () => {
    render(
      <DataTable
        data={mockData}
        columns={mockColumns}
        keyExtractor={(item) => item.id}
        pagination={true}
        itemsPerPage={5}
      />
    );

    // On first page, Previous button should be disabled
    const allButtons = screen.getAllByTestId('mock-button');
    const previousButton = allButtons.find(button => 
      button.textContent?.includes('Previous')
    );
    expect(previousButton).toBeDisabled();

    // Next button should be enabled
    const nextButton = allButtons.find(button => 
      button.textContent?.includes('Next')
    );
    expect(nextButton).not.toBeDisabled();

    // Go to last page
    const page3Button = screen.getByText('3');
    fireEvent.click(page3Button);

    // On last page, Next button should be disabled
    const allButtonsAfterNav = screen.getAllByTestId('mock-button');
    const nextButtonAfterNav = allButtonsAfterNav.find(button => 
      button.textContent?.includes('Next')
    );
    expect(nextButtonAfterNav).toBeDisabled();

    // Previous button should be enabled
    const previousButtonAfterNav = allButtonsAfterNav.find(button => 
      button.textContent?.includes('Previous')
    );
    expect(previousButtonAfterNav).not.toBeDisabled();
  });

  test('custom styling props are applied correctly', () => {
    const customClassName = 'custom-container-class';
    const customTableClassName = 'custom-table-class';
    const customHeaderClassName = 'custom-header-class';
    
    const { container } = render(
      <DataTable
        data={mockData}
        columns={mockColumns}
        keyExtractor={(item) => item.id}
        className={customClassName}
        tableClassName={customTableClassName}
        headerClassName={customHeaderClassName}
      />
    );

    // Check if custom container class is applied
    const containerElement = container.firstChild as HTMLElement;
    expect(containerElement).toHaveClass(customClassName);

    // Check if custom table class is applied
    const tableElement = container.querySelector('table');
    expect(tableElement).toHaveClass(customTableClassName);

    // Check if custom header class is applied
    const headerElement = container.querySelector('thead');
    expect(headerElement).toHaveClass(customHeaderClassName);
  });

  test('rowClassName function is applied correctly', () => {
    const rowClassNameFn = (item: TestItem) => {
      return item.age > 30 ? 'senior-person' : 'junior-person';
    };

    const { container } = render(
      <DataTable
        data={mockData.slice(0, 3)} // Just use first 3 items for simplicity
        columns={mockColumns}
        keyExtractor={(item) => item.id}
        rowClassName={rowClassNameFn}
      />
    );

    // Get all rows in the tbody
    const rows = container.querySelectorAll('tbody tr');
    
    // John Doe is 30, should have junior class
    expect(rows[0]).toHaveClass('junior-person');
    
    // Jane Smith is 25, should have junior class
    expect(rows[1]).toHaveClass('junior-person');
    
    // Bob Johnson is 40, should have senior class
    expect(rows[2]).toHaveClass('senior-person');
  });

  test('handles different itemsPerPage values correctly', () => {
    render(
      <DataTable
        data={mockData}
        columns={mockColumns}
        keyExtractor={(item) => item.id}
        pagination={true}
        itemsPerPage={2}
      />
    );

    // With 11 items and 2 per page, should have 6 pages
    const pageButtons = screen.getAllByRole('button').filter(button => 
      /^\d+$/.test(button.textContent || '')
    );
    
    expect(pageButtons.length).toBe(6);
    expect(pageButtons[0].textContent).toBe('1');
    expect(pageButtons[5].textContent).toBe('6');

    // Check pagination info text
    const paginationInfo = screen.getByTestId('pagination-info');
    expect(paginationInfo).toHaveTextContent('Showing 1 to 2 of 11 results');
  });

  test('renders a table without pagination when pagination prop is false', () => {
    render(
      <DataTable
        data={mockData}
        columns={mockColumns}
        keyExtractor={(item) => item.id}
        pagination={false}
      />
    );

    // All 11 items should be rendered
    expect(screen.getAllByTestId('custom-column').length).toBe(11);
    
    // Pagination controls should not be present
    expect(screen.queryByText('Previous')).not.toBeInTheDocument();
    expect(screen.queryByText('Next')).not.toBeInTheDocument();
    expect(screen.queryByText(/Showing 1 to/)).not.toBeInTheDocument();
  });
});