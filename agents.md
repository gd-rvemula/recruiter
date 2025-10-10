# Recruiter System - Agent Guide

This document provides comprehensive information for agents working with the Recruiter system, including architecture, setup, and operational procedures.

## Project Overview

The Recruiter system is a full-stack application for recruitment management consisting of:
- **Backend**: .NET 8 Web API with PostgreSQL database
- **Frontend**: React TypeScript application with Vite
- **Database**: PostgreSQL 15 in Docker container
- **Containerization**: Docker Compose for orchestration
- **Background Processing**: Foundatio framework for queues and jobs (ready for new features)

## Project Structure

```
Recruiter/
├── backend/                          # .NET 8 Web API
│   ├── Controllers/                  # API controllers
│   │   ├── CandidatesController.cs   # Main candidate operations
│   │   ├── TestController.cs         # Health/test endpoints
│   │   └── RawSqlController.cs       # Raw SQL operations
│   ├── Models/                       # Entity Framework models
│   │   ├── Candidate.cs              # Core candidate entity
│   │   ├── Skill.cs                  # Skills management
│   │   ├── WorkExperience.cs         # Work history
│   │   ├── Education.cs              # Education records
│   │   ├── Resume.cs                 # Resume/CV handling
│   │   ├── CandidateSkill.cs         # Candidate-skill relationships
│   │   └── JobApplication.cs         # Job applications
│   ├── Data/                         # Database context
│   │   └── RecruiterDbContext.cs     # EF Core context
│   ├── Dtos/                         # Data transfer objects
│   ├── Services/                     # Business logic services
│   ├── docker-compose.yml            # Container orchestration
│   ├── Dockerfile                    # Backend container config
│   ├── Program.cs                    # Application entry point
│   └── README.md                     # Backend documentation
│
└── frontend/                         # React TypeScript UI
    ├── src/
    │   ├── components/               # React components
    │   │   ├── common/               # Reusable UI components
    │   │   ├── dashboard/            # Dashboard components
    │   │   └── employees/            # Employee management
    │   ├── hooks/                    # Custom React hooks
    │   ├── pages/                    # Page components
    │   ├── services/                 # API integration
    │   ├── types/                    # TypeScript type definitions
    │   └── utils/                    # Utility functions
    ├── mock-server/                  # Development mock server
    ├── coverage/                     # Test coverage reports
    ├── package.json                  # Node.js dependencies
    ├── vite.config.ts               # Vite configuration
    └── README.md                    # Frontend documentation
```

## Database Schema

The PostgreSQL database contains the following main tables:
- `candidates` - Core candidate information
- `skills` - Available skills catalog
- `work_experience` - Candidate work history
- `education` - Education records
- `resumes` - Resume/CV documents
- `candidate_skills` - Many-to-many candidate-skill relationships
- `job_applications` - Job application tracking

**Important**: Database uses snake_case naming (e.g., `first_name`, `skill_name`) while Entity Framework models use PascalCase with proper [Column] attribute mappings.

## Full-Text Search Infrastructure (Permanent)

The system includes **permanent full-text search (FTS)** infrastructure that is automatically initialized:

### FTS Components
- **Search Vector Columns**: All main tables have `search_vector` columns for fast text search
- **Automatic Triggers**: Database triggers maintain search vectors on INSERT/UPDATE
- **Materialized View**: `candidate_search_view` combines candidate, skills, and resume data
- **Search Functions**: PostgreSQL functions for optimized search and suggestions
- **GIN Indexes**: High-performance indexes for sub-second search results

### FTS Features
- ✅ **Multi-field Search**: Names, titles, skills, resume content
- ✅ **Relevance Ranking**: PostgreSQL ts_rank scoring
- ✅ **Fuzzy Matching**: Trigram similarity for typo tolerance  
- ✅ **Auto-complete**: Search suggestions API
- ✅ **Performance**: Sub-second search across 1000+ candidates

### FTS Initialization
- **Automatic**: FTS infrastructure is created when application starts
- **Manual**: Run `backend/Migrations/PermanentFTS.sql` for complete setup
- **Entity Framework**: Models include search vector properties permanently

### Search Endpoints
- `POST /api/candidates/search` - Main search (uses FTS when searchTerm provided)
- `POST /api/search/fts` - Advanced FTS endpoint with pure PostgreSQL ranking

## Backend Setup & Operations

### Prerequisites
- Docker and Docker Compose
- .NET 8 SDK (for local development)
- PostgreSQL client (optional, for direct DB access)

### Running Backend with Docker (Recommended)

1. **Navigate to backend directory**:
   ```bash
   cd /Users/rvemula/projects/Recruiter/backend
   ```

2. **Start services** (builds and runs API + PostgreSQL):
   ```bash
   docker compose up --build
   ```

3. **Alternative: Start in detached mode**:
   ```bash
   docker compose up -d --build
   ```

4. **View logs**:
   ```bash
   docker compose logs -f recruiter-api
   ```

### Backend Endpoints

- **Base URL**: `http://localhost:8080`
- **Health Check**: `http://localhost:8080/health`
- **API Health**: `http://localhost:8080/api/health`
- **Swagger UI**: `http://localhost:8080/swagger`

#### Key API Endpoints
- `POST /api/candidates/search` - Search candidates with pagination and filters (including sponsorship filter)
- `GET /api/candidates` - Get all candidates
- `GET /api/candidates/{id}` - Get specific candidate
- `GET /api/test/database` - Test database connectivity
- `POST /api/rawsql/execute` - Execute raw SQL queries
- `POST /api/excelimport/upload` - Import candidates from Excel file (multipart/form-data)

#### Search with Sponsorship Filter
```bash
# Search all candidates (no filter)
curl -X POST http://localhost:8080/api/candidates/search \
  -H "Content-Type: application/json" \
  -d '{"searchTerm": ".net developer", "page": 1, "pageSize": 10}'

# Search only candidates NOT needing sponsorship
curl -X POST http://localhost:8080/api/candidates/search \
  -H "Content-Type: application/json" \
  -d '{"searchTerm": ".net developer", "sponsorshipFilter": "no", "page": 1, "pageSize": 10}'

# Search only candidates needing sponsorship
curl -X POST http://localhost:8080/api/candidates/search \
  -H "Content-Type: application/json" \
  -d '{"searchTerm": ".net developer", "sponsorshipFilter": "yes", "page": 1, "pageSize": 10}'
```

**Note**: The sponsorship filter works with all search strategies (hybrid, semantic, and basic search). It supports three values:
- `"all"` or omitted - No filter, returns all candidates
- `"yes"` - Only candidates needing sponsorship
- `"no"` - Only candidates NOT needing sponsorship

#### Excel Import Example
```bash
# Import a single Excel file
curl -X POST http://localhost:8080/api/excelimport/upload \
  -F "file=@/Users/rvemula/projects/Recruiter/data/R3654_Lead_Product_Engineer_Candidates.xlsx" \
  -H "Accept: application/json" | jq .

# Import multiple files sequentially
echo "=== [1/4] Importing R3654_Lead_Product_Engineer_Candidates.xlsx ===" && \
curl -X POST http://localhost:8080/api/excelimport/upload \
  -F "file=@/Users/rvemula/projects/Recruiter/data/R3654_Lead_Product_Engineer_Candidates.xlsx" \
  -H "Accept: application/json" 2>/dev/null | jq .

echo "=== [2/4] Importing R3655_Lead_Product_Engineer_–Candidates.xlsx ===" && \
curl -X POST http://localhost:8080/api/excelimport/upload \
  -F "file=@/Users/rvemula/projects/Recruiter/data/R3655_Lead_Product_Engineer_–Candidates.xlsx" \
  -H "Accept: application/json" 2>/dev/null | jq .

echo "=== [3/4] Importing R3656_Lead_Product_Engineer_–Candidates.xlsx ===" && \
curl -X POST http://localhost:8080/api/excelimport/upload \
  -F "file=@/Users/rvemula/projects/Recruiter/data/R3656_Lead_Product_Engineer_–Candidates.xlsx" \
  -H "Accept: application/json" 2>/dev/null | jq .

echo "=== [4/4] Importing R3681_Lead_Software_Engineer.xlsx ===" && \
curl -X POST http://localhost:8080/api/excelimport/upload \
  -F "file=@/Users/rvemula/projects/Recruiter/data/R3681_Lead_Software_Engineer.xlsx" \
  -H "Accept: application/json" 2>/dev/null | jq .
```

**Note**: The Excel import endpoint uses multipart/form-data with `-F "file=@path"` syntax, not JSON payload.

### Docker Commands

#### Container Management
```bash
# View running containers
docker ps

# Stop all services
docker compose down

# Remove containers and volumes
docker compose down -v

# Rebuild without cache
docker compose build --no-cache

# Remove old images (if caching issues)
docker rmi recruiter-api backend-recruiter-api
```

#### Database Access
```bash
# Connect to PostgreSQL container
docker exec -it p3v2-backend-db-1 psql -U postgres -d recruitingdb

# View database logs
docker logs p3v2-backend-db-1
```

### Local Development (Without Docker)

1. **Ensure PostgreSQL is running** on port 5433
2. **Run the API**:
   ```bash
   cd /Users/rvemula/projects/Recruiter/backend
   dotnet run --urls="http://localhost:5000"
   ```

### Testing Backend

```bash
# Test health endpoint
curl http://localhost:8080/health

# Test candidate search
curl -X POST http://localhost:8080/api/candidates/search \
  -H "Content-Type: application/json" \
  -d '{"Page": 1, "PageSize": 10}' \
  -v
```

## Frontend Setup & Operations

### Prerequisites
- Node.js (v16 or later)
- npm

### Running Frontend

1. **Navigate to frontend directory**:
   ```bash
   cd /Users/rvemula/projects/Recruiter/frontend
   ```

2. **Install dependencies** (first time):
   ```bash
   npm install
   ```

3. **Start development server**:
   ```bash
   npm run dev
   ```

4. **Access application**: `http://localhost:5173`

### Frontend Scripts

```bash
# Development
npm run dev                    # Start dev server
npm run dev:full              # Start dev + tests + mock server
npm run dev:with-coverage     # Start dev + coverage + mock server

# Building
npm run build                 # Build for production
npm run preview              # Preview production build

# Testing
npm run test                 # Run tests once
npm run test:watch           # Run tests in watch mode
npm run test:coverage        # Generate coverage report
npm run test:coverage:open   # Generate and open coverage

# Mock Server (for testing without backend)
npm run mock-server          # Start JSON server on port 3001
```

## Development Workflow

### Starting Full Development Environment

1. **Start Backend** (in terminal 1):
   ```bash
   cd /Users/rvemula/projects/Recruiter/backend
   docker compose up -d --build
   ```

2. **Start Frontend** (in terminal 2):
   ```bash
   cd /Users/rvemula/projects/Recruiter/frontend
   npm run dev
   ```

3. **Access Applications**:
   - Frontend: `http://localhost:5173`
   - Backend API: `http://localhost:8080`
   - Swagger: `http://localhost:8080/swagger`

### Common Development Tasks

#### Database Operations
```bash
# Check database connectivity
curl http://localhost:8080/api/test/database

# Execute raw SQL
curl -X POST http://localhost:8080/api/rawsql/execute \
  -H "Content-Type: application/json" \
  -d '{"query": "SELECT COUNT(*) FROM candidates;"}'
```

#### Entity Framework Operations
```bash
cd /Users/rvemula/projects/Recruiter/backend

# Add migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Drop database
dotnet ef database drop
```

## Troubleshooting

### Common Issues

1. **Port Conflicts**:
   - Backend: Check if ports 8080/8081 are available
   - Frontend: Check if port 5173 is available
   - Database: PostgreSQL runs on port 5433

2. **Docker Caching Issues**:
   ```bash
   # Complete cleanup and rebuild
   docker compose down
   docker rmi recruiter-api backend-recruiter-api
   docker compose build --no-cache
   docker compose up -d
   ```

3. **Database Connection Issues**:
   - Verify PostgreSQL container is running: `docker ps`
   - Check connection string in appsettings.json
   - Ensure database exists: `recruitingdb`

4. **Entity Framework Issues**:
   - Verify model properties match database columns
   - Check [Column] attribute mappings for snake_case
   - Ensure proper navigation properties

5. **Docker command output issues**:
   - when you run docker exec, use PAGER=cat option so that user input is not needed to quit out of the shell

### Useful Debugging Commands

```bash
# Check backend logs
docker compose logs -f recruiter-api

# Check database logs
docker logs p3v2-backend-db-1

# Test API connectivity
curl -v http://localhost:8080/health

# Check running processes
ps aux | grep -E "(dotnet|node|postgres)"

# Kill all related processes
pkill -f "Recruiter"
```

## Environment Configuration

### Backend Environment Variables
- `ASPNETCORE_ENVIRONMENT=Development`
- `ASPNETCORE_URLS=http://+:8080`
- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string

### Database Configuration
- **Host**: localhost (or host.docker.internal in Docker)
- **Port**: 5433
- **Database**: recruitingdb
- **Username**: postgres
- **Password**: P3v2_S3cur3_Passw0rd

## Testing Strategy

### Backend Testing
- Unit tests for controllers and services
- Integration tests for database operations
- API endpoint testing with curl or Postman

### Frontend Testing
- Jest for unit testing
- React Testing Library for component testing
- Coverage reports in `coverage/` directory

## Security Considerations

- Database credentials in environment variables
- API endpoints should implement proper authentication
- Input validation on all endpoints
- CORS configuration for production deployment

## Foundatio Framework Integration

### Overview
The backend includes **Foundatio (v10.7.1)** - a pluggable foundation for building loosely coupled distributed apps. While currently underutilized, Foundatio will be leveraged for new features requiring background processing, queueing, and distributed operations.

### Foundatio Capabilities Available
- ✅ **Queues** (`IQueue<T>`) - In-memory and persistent message queues
- ✅ **Jobs** (`IJob`) - Background job scheduling and execution
- ✅ **Caching** (`ICacheClient`) - Distributed caching layer
- ✅ **Messaging** (`IMessageBus`) - Pub/sub messaging patterns
- ✅ **File Storage** (`IFileStorage`) - Abstract file storage operations
- ✅ **Metrics** (`IMetricsClient`) - Application metrics and monitoring

### Current Status
- **Package**: Installed in RecruiterApi.csproj (v10.7.1)
- **Usage**: Currently minimal (ExcelProcessingBackgroundService placeholder)
- **Future Plans**: Will be leveraged for new features (see below)

### Planned Foundatio Use Cases

#### 1. **Semantic Search Embedding Generation**
```csharp
// Queue-based embedding generation for candidates
public class EmbeddingGenerationJob
{
    public Guid CandidateId { get; set; }
    public string ProfileText { get; set; }
}

// Enqueue job when candidate is created/updated
await _queue.EnqueueAsync(new EmbeddingGenerationJob 
{ 
    CandidateId = candidate.Id,
    ProfileText = candidate.GetProfileText()
});

// Background service processes queue asynchronously
```

**Benefits**:
- Non-blocking API responses (immediate return)
- Retry logic for failed embedding generation
- Batch processing optimization
- Progress tracking via job status

#### 2. **Excel Import Background Processing**
```csharp
// Queue Excel import jobs instead of synchronous processing
public class ExcelImportJob
{
    public string FilePath { get; set; }
    public Guid JobId { get; set; }
    public string UploadedBy { get; set; }
}

// Controller returns immediately with job ID
var jobId = Guid.NewGuid();
await _importQueue.EnqueueAsync(new ExcelImportJob 
{ 
    FilePath = savedPath, 
    JobId = jobId 
});
return Accepted(new { jobId, status = "Processing" });
```

**Benefits**:
- Handle large Excel files (1000+ rows) without timeout
- Progress reporting via job status
- Error handling and retry for failed imports
- User can continue working while import processes

#### 3. **Resume Text Processing Pipeline**
```csharp
// Multi-stage processing pipeline using message bus
// Stage 1: Extract text from resume file
await _messageBus.PublishAsync(new ResumeUploadedEvent { ResumeId = id });

// Stage 2: Extract skills from text (subscriber)
// Stage 3: Generate embeddings (subscriber)
// Stage 4: Update search indexes (subscriber)
```

**Benefits**:
- Decoupled processing stages
- Parallel processing of different stages
- Easy to add new processing steps
- Fault isolation (one stage failure doesn't affect others)

#### 4. **Search Result Caching**
```csharp
// Cache frequently-accessed search results
var cacheKey = $"search:{query}:{page}";
var cached = await _cache.GetAsync<SearchResults>(cacheKey);

if (cached.HasValue)
    return cached.Value;

var results = await PerformSearchAsync(query, page);
await _cache.SetAsync(cacheKey, results, TimeSpan.FromMinutes(5));
```

**Benefits**:
- Reduced database load for popular searches
- Faster response times
- Easy cache invalidation when data changes

#### 5. **Candidate Status Change Notifications**
```csharp
// Publish status change events
await _messageBus.PublishAsync(new CandidateStatusChangedEvent 
{ 
    CandidateId = id,
    OldStatus = oldStatus,
    NewStatus = newStatus
});

// Multiple subscribers can react:
// - Send email notification
// - Update analytics
// - Trigger workflow automation
// - Log to audit trail
```

**Benefits**:
- Event-driven architecture
- Loosely coupled notification system
- Easy to add new notification channels
- Non-blocking status updates

### Implementation Guidelines for New Features

When implementing new features that require background processing:

1. **Use Foundatio Queues** for:
   - Long-running operations (>30 seconds)
   - Operations that can fail and need retry logic
   - Batch processing of multiple items
   - Operations that benefit from rate limiting

2. **Use Foundatio Message Bus** for:
   - Event-driven workflows
   - Pub/sub patterns with multiple subscribers
   - Decoupled service communication
   - Real-time notifications

3. **Use Foundatio Caching** for:
   - Frequently accessed data (search results, aggregations)
   - Data that changes infrequently
   - Reducing database load
   - Improving response times

4. **Use Foundatio Jobs** for:
   - Scheduled maintenance tasks
   - Periodic data synchronization
   - Cleanup operations
   - Batch processing schedules

### Configuration Example

```csharp
// Program.cs - Future Foundatio setup
builder.Services.AddSingleton<IQueue<EmbeddingGenerationJob>>(provider => 
    new InMemoryQueue<EmbeddingGenerationJob>(new InMemoryQueueOptions()));

builder.Services.AddSingleton<IMessageBus>(provider => 
    new InMemoryMessageBus(new InMemoryMessageBusOptions()));

builder.Services.AddSingleton<ICacheClient>(provider => 
    new InMemoryCacheClient(new InMemoryCacheClientOptions()));
```

### Migration Strategy

**Current State**: Foundatio installed but minimal usage  
**Future State**: Leverage Foundatio for all new async features

**Approach**:
- ✅ Keep existing synchronous features as-is (no refactoring)
- ✅ Use Foundatio for ALL new features requiring background processing
- ✅ Gradually refactor Excel import when capacity allows
- ✅ Document Foundatio patterns in code for consistency

### Benefits of Foundatio Framework

1. **Abstraction**: Switch between in-memory and distributed implementations
2. **Testing**: Easy to test with in-memory implementations
3. **Scalability**: Can scale to Redis, RabbitMQ, Azure Service Bus later
4. **Reliability**: Built-in retry logic and error handling
5. **Performance**: Non-blocking operations improve API responsiveness
6. **Monitoring**: Built-in metrics and health checks

---

## Performance Notes

- Entity Framework uses PostgreSQL with proper indexing
- Frontend uses React Query for efficient data fetching
- Docker containers optimized for development
- Consider connection pooling for production
- **Foundatio queues**: Enable async processing for long-running operations
- **Foundatio caching**: Reduce database load for frequently accessed data

## Production Deployment

1. **Backend**: Configure production database connection strings
2. **Frontend**: Build with `npm run build` and serve static files
3. **Database**: Use managed PostgreSQL service
4. **Security**: Implement authentication, HTTPS, proper CORS
5. **Monitoring**: Add logging, health checks, and monitoring

---

## open source usage

**Always ask before including any open source library
**If the library is not a MIT or Apache 2.0 licensed, provide details and ask for confirmation

## database updates

**Always create scripts in Migrations folder
**Copy the script to the docker container running datbase and execute from there

**Last Updated**: October 4, 2025
**Project Status**: Development - Core functionality operational, Foundatio ready for new features
**Key Contact Points**: Backend API on port 8080, Frontend on port 5173, PostgreSQL on port 5433
**Background Processing**: Foundatio v10.7.1 installed and ready for async features



DO NOT:
 - Do not hardcode URLs or other variables like below when a variable is not found, if a variable is not defined, throw error and exit.
 ```csharp
   private static readonly string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") 
        ?? "https://.....";
```