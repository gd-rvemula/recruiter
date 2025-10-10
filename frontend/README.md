# Recruiter Configuration

# Recruiter System

A modern React application for recruitment management. Built with TypeScript, Vite, and Jest for robust testing.

## Features

- **React & TypeScript:** Type-safe component development
- **Vite:** Fast development server and optimized builds
- **Jest:** Comprehensive testing framework
- **React Router:** Client-side routing
- **React Query (@tanstack/react-query):** Data fetching and state management
- **Date-fns:** Date manipulation utilities
- **Mock Server:** JSON Server for simulated backend during development

## Project Structure

```
├── mock-server/          # JSON Server mock API
│   ├── data/             # Mock data files
│   └── routes.json       # API route definitions
├── src/
│   ├── assets/           # Static assets and styles
│   ├── components/       # React components
│   ├── hooks/            # Custom React hooks
│   ├── pages/            # Page components
│   │   └── Dashboard.tsx # Main dashboard view
│   ├── services/         # API services
│   ├── utils/            # Utility functions
│   │   └── date-utils.ts # Date manipulation utilities
│   ├── App.tsx           # Main application component
│   └── main.tsx          # Application entry point
```

## Getting Started

### Prerequisites
- Node.js (v16 or later)
- npm

### Installation

1. Clone the repository
```bash
git clone <repository-url>
cd Recruiter/frontend
```

2. Install dependencies
```bash
npm install
```

3. Start the development server
```bash
npm run dev
```

4. Start the mock server in a separate terminal
```bash
npm run mock-server
```

5. Run tests
```bash
npm test
```

## Available Scripts

- `npm run dev` - Start the development server
- `npm run build` - Build for production
- `npm run preview` - Preview the production build
- `npm run lint` - Lint the codebase
- `npm run mock-server` - Start the JSON Server mock API on port 3001
- `npm test` - Run tests
- `npm run test:watch` - Run tests in watch mode
- `npm run test:coverage` - Generate test coverage report
- `npm run test:coverage:watch` - Run tests with coverage in watch mode
- `npm run test:coverage:open` - Generate coverage report and open in browser
- `npm run dev:full` - Start development server, tests in watch mode, and mock server
- `npm run dev:with-coverage` - Start development server, tests with coverage, and mock server
- `npm run start:all` - Start the application with mock server and documentation

## Application Pages

The application currently has the following implemented pages:

- **Dashboard** (`/`): Main dashboard view

## Key Features

- **Dashboard**: Main application dashboard
- **Date Utilities**: Helper functions for managing date ranges and week calculations
- **API Integration**: React Query integration for efficient data fetching and caching

## Development Notes

- The application uses React Query for data fetching and state management
- Jest is used for testing with comprehensive coverage
- Date-fns is used for date manipulation
- Mock server is powered by JSON Server for local development

### Environment Variables

The application uses the following environment variables:

- `VITE_API_URL` - The base URL for the API (defaults to http://localhost:3001)



## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Commit your changes: `git commit -m 'Add some feature'`
4. Push to the branch: `git push origin feature/your-feature-name`
5. Submit a pull request

## License

Proprietary. All rights reserved.



## Testing

Tests are written using Jest and React Testing Library. To run tests:

\`\`\`bash
npm test
\`\`\`

## Acknowledgements

- [React](https://reactjs.org/)
- [TypeScript](https://www.typescriptlang.org/)
- [Vite](https://vitejs.dev/)
- [Jest](https://jestjs.io/)
- [React Router](https://reactrouter.com/)
- [React Query](https://tanstack.com/query)
- [JSON Server](https://github.com/typicode/json-server)
- [Date-fns](https://date-fns.org/)
