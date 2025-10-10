# Recruiter System - Candidate Management

A complete full-stack candidate management system built with .NET 8 Web API backend and React TypeScript frontend.

## Features

### Backend (.NET 8 Web API)
- **PostgreSQL Database** with normalized schema for candidates, job applications, resumes, work experience, education, and skills
- **RESTful APIs** for candidate search with advanced filtering
- **Excel Import** functionality with EPPlus library
- **Background Job Processing** for handling large Excel imports
- **Entity Framework Core** for data access
- **Swagger/OpenAPI** documentation
- **Health Checks** and logging with Serilog

### Frontend (React + TypeScript)
- **Modern React 18** with TypeScript
- **TanStack Query (React Query)** for server state management
- **Zustand** for global state management
- **Tailwind CSS** for styling
- **Vite** for fast development and building
- **Candidate Search Interface** with advanced filtering
- **Excel Upload** functionality
- **Responsive Design**

## Database Schema

### Core Tables
- `candidates` - Main candidate information
- `job_applications` - Application tracking
- `resumes` - File management and parsed content
- `work_experience` - Employment history
- `education` - Academic background
- `skills` - Master skills list
- `candidate_skills` - Many-to-many relationship with proficiency levels

## Getting Started

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- PostgreSQL 14+

### Backend Setup

1. **Clone and navigate to backend:**
   ```bash
   cd backend
   ```

2. **Configure database connection:**
   Update `appsettings.json` with your PostgreSQL connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=recruitingdb;Username=postgres;Password=your_password;"
     }
   }
   ```

3. **Create database:**
   ```bash
   # Create database manually in PostgreSQL or run:
   psql -U postgres -c "CREATE DATABASE recruitingdb;"
   
   # Run the database schema script
   psql -U postgres -d recruitingdb -f Database/01_create_tables.sql
   ```

4. **Install dependencies and run:**
   ```bash
   dotnet restore
   dotnet build
   dotnet run --urls="http://localhost:5000"
   ```

5. **Access Swagger UI:**
   Open http://localhost:5000/swagger

### Frontend Setup

1. **Navigate to frontend:**
   ```bash
   cd frontend
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Start development server:**
   ```bash
   npm run dev
   ```

4. **Access application:**
   Open http://localhost:3000

## API Endpoints

### Candidates API (`/api/candidates`)
- `POST /search` - Search candidates with filters
- `GET /{id}` - Get candidate details
- `POST /` - Create new candidate
- `PUT /{id}` - Update candidate
- `DELETE /{id}` - Delete candidate

### Excel Import API (`/api/excelimport`)
- `POST /upload` - Upload and process Excel file immediately
- `POST /preview` - Preview Excel data before import
- `POST /queue` - Queue Excel file for background processing
- `GET /job-status/{jobId}` - Get background job status
- `POST /import-by-path` - Import by file path (for background jobs)

## Excel File Format

The system expects Excel files with the following columns:
- First Name
- Last Name
- Email (required)
- Phone
- Location
- Current Company
- Current Title
- Years of Experience
- Visa Status
- Salary Expectation
- Relocation (Yes/No)
- Work Type
- LinkedIn
- University
- Degree
- Field of Study
- Skills (comma-separated)
- Notes

## Architecture

### Backend Architecture
```
Controllers/
├── CandidatesController.cs    # Main candidate API endpoints
└── ExcelImportController.cs   # Excel import endpoints

Services/
├── ExcelImportService.cs          # Excel processing logic
└── ExcelProcessingBackgroundService.cs  # Background job processor

Models/
├── Candidate.cs         # Core entity models
├── JobApplication.cs
├── Resume.cs
├── WorkExperience.cs
├── Education.cs
├── Skill.cs
└── CandidateSkill.cs

DTOs/
├── CandidateDto.cs      # API response DTOs
├── RelatedDto.cs
└── SearchDto.cs

Data/
└── RecruiterDbContext.cs  # EF Core DbContext
```

### Frontend Architecture
```
src/
├── pages/
│   ├── Dashboard.tsx      # Dashboard page
│   └── Candidates.tsx     # Candidate management page
├── components/
│   └── common/           # Reusable components
├── hooks/
│   └── useAppStore.ts    # Zustand store
├── services/
│   └── api.ts           # API service layer
└── types/
    └── employee.ts      # TypeScript types
```

## Development Notes

### Key Features Implemented
1. **Complete CRUD operations** for candidates
2. **Advanced search** with multiple filter criteria
3. **Excel import** with error handling and validation
4. **Background job processing** for large imports
5. **Normalized database design** for data integrity
6. **Modern React patterns** with hooks and context
7. **Type-safe** development with TypeScript
8. **Responsive UI** with Tailwind CSS

### Technical Decisions
- **PostgreSQL** chosen for robust relational data support
- **Entity Framework Core** for type-safe database operations
- **EPPlus** for Excel processing (better .NET 8 support than NPOI)
- **Background services** for processing large files without blocking UI
- **React Query** for efficient server state management
- **Zustand** for simple global state management

## Production Considerations

### Security
- Add authentication/authorization
- Implement input validation and sanitization
- Add rate limiting
- Use HTTPS in production

### Performance
- Add database indexes for search columns
- Implement pagination for large datasets
- Add caching for frequently accessed data
- Optimize Excel processing for large files

### Monitoring
- Add application insights/telemetry
- Implement proper error tracking
- Add performance monitoring
- Set up database monitoring

## Testing

### Backend Tests
```bash
cd backend
dotnet test
```

### Frontend Tests
```bash
cd frontend
npm test
```

## Deployment

### Backend Deployment
- Configure production connection strings
- Set up database migrations
- Deploy to cloud service (Azure, AWS, etc.)
- Configure environment variables

### Frontend Deployment
- Build production bundle: `npm run build`
- Deploy to static hosting (Vercel, Netlify, etc.)
- Configure API endpoints for production

## License

This project is licensed under the MIT License.