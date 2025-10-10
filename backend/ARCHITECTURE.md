# Recruiter API - Backend Architecture Overview

**Date**: October 4, 2025  
**Technology Stack**: .NET 8, PostgreSQL 15, Entity Framework Core 8.0  
**Project Type**: RESTful Web API for Recruitment Management System

---

## 📊 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLIENT LAYER                              │
│                   (React Frontend on Port 5173)                  │
└───────────────────────────┬─────────────────────────────────────┘
                            │ HTTP/REST API
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                      PRESENTATION LAYER                          │
│                       (Controllers)                              │
│  ┌──────────────────┐  ┌──────────────────┐  ┌───────────────┐│
│  │  Candidates      │  │  Search          │  │  Excel Import ││
│  │  Controller      │  │  Controller      │  │  Controller   ││
│  └──────────────────┘  └──────────────────┘  └───────────────┘│
│  ┌──────────────────┐  ┌──────────────────┐                    │
│  │  Test            │  │  RawSql          │                    │
│  │  Controller      │  │  Controller      │                    │
│  └──────────────────┘  └──────────────────┘                    │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                      BUSINESS LOGIC LAYER                        │
│                         (Services)                               │
│  ┌──────────────────┐  ┌──────────────────┐  ┌───────────────┐│
│  │  Excel Import    │  │  Full-Text       │  │  Skill        ││
│  │  Service         │  │  Search Service  │  │  Extraction   ││
│  └──────────────────┘  └──────────────────┘  └───────────────┘│
│  ┌──────────────────┐  ┌──────────────────┐  ┌───────────────┐│
│  │  Semantic Search │  │  OpenAI          │  │  Ollama       ││
│  │  Service (NEW)   │  │  Embedding (NEW) │  │  Embedding    ││
│  └──────────────────┘  └──────────────────┘  └───────────────┘│
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Excel Processing Background Service                     │  │
│  └──────────────────────────────────────────────────────────┘  │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                      DATA ACCESS LAYER                           │
│                    (Entity Framework Core)                       │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              RecruiterDbContext                           │  │
│  │  DbSet<Candidate>, DbSet<Resume>, DbSet<Skill>, etc.     │  │
│  └──────────────────────────────────────────────────────────┘  │
└───────────────────────────┬─────────────────────────────────────┘
                            │ ADO.NET / Npgsql
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                      DATABASE LAYER                              │
│                  PostgreSQL 15 in Docker                         │
│  ┌────────────────┐  ┌────────────────┐  ┌──────────────────┐ │
│  │  candidates    │  │  resumes       │  │  skills          │ │
│  │  (651 rows)    │  │  (651 rows)    │  │  (113 rows)      │ │
│  └────────────────┘  └────────────────┘  └──────────────────┘ │
│  ┌────────────────┐  ┌────────────────┐  ┌──────────────────┐ │
│  │ candidate_     │  │  work_         │  │  education       │ │
│  │ skills         │  │  experience    │  │                  │ │
│  │ (24K+ rows)    │  │                │  │                  │ │
│  └────────────────┘  └────────────────┘  └──────────────────┘ │
│                                                                  │
│  Extensions: pg_trgm (full-text), pgvector (semantic search)   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🗂️ Project Structure

```
backend/
├── Controllers/          # API Endpoints (Presentation Layer)
│   ├── CandidatesController.cs      [Main CRUD + Search + Skills]
│   ├── SearchController.cs          [Full-Text Search]
│   ├── ExcelImportController.cs     [Bulk Import]
│   ├── TestController.cs            [Health/Test Endpoints]
│   └── RawSqlController.cs          [Raw SQL Execution]
│
├── Services/             # Business Logic Layer
│   ├── ExcelImportService.cs        [Excel Processing]
│   ├── FullTextSearchService.cs     [PostgreSQL FTS]
│   ├── SkillExtractionService.cs    [Resume-based Skill Extraction]
│   ├── SemanticSearchService.cs     [NEW: Vector Search]
│   ├── OpenAIEmbeddingService.cs    [NEW: OpenAI Embeddings]
│   ├── OllamaEmbeddingService.cs    [NEW: Local Embeddings]
│   └── ExcelProcessingBackgroundService.cs [Background Jobs]
│
├── Models/               # Entity/Domain Models (Database Schema)
│   ├── Candidate.cs                 [Core entity]
│   ├── Resume.cs                    [Resume data]
│   ├── Skill.cs                     [Skills catalog]
│   ├── CandidateSkill.cs            [Many-to-many junction]
│   ├── WorkExperience.cs            [Work history]
│   ├── Education.cs                 [Education records]
│   ├── JobApplication.cs            [Job applications]
│   ├── CandidateStatusHistory.cs    [Status tracking]
│   └── CandidateStatus.cs           [Status enum]
│
├── DTOs/                 # Data Transfer Objects
│   ├── CandidateDto.cs              [Candidate API responses]
│   ├── SearchDto.cs                 [Search requests/responses]
│   ├── SkillFrequencyDto.cs         [Skills word cloud data]
│   ├── CandidateStatusDto.cs        [Status data]
│   └── ExcelImportResultDto.cs      [Import results]
│
├── Data/                 # Data Access Layer
│   └── RecruiterDbContext.cs        [EF Core DbContext]
│
├── Migrations/           # SQL Migration Scripts
│   ├── PermanentFTS.sql             [Full-text search setup]
│   ├── Add100ComprehensiveSkills.sql [Skills catalog]
│   ├── SimplifiedSkillExtraction.sql [Skill population]
│   ├── InstallPgVector.sql          [NEW: Vector extension]
│   └── AddEmbeddingColumns.sql      [NEW: Embedding columns]
│
├── Program.cs            # Application Entry Point & Configuration
├── RecruiterApi.csproj   # Project Dependencies
├── appsettings.json      # Configuration (Production)
├── appsettings.Development.json  # Configuration (Dev)
└── docker-compose.yml    # Docker orchestration
```

---

## 🎯 Core Components

### 1. **Controllers** (API Endpoints)

#### CandidatesController
- **Purpose**: Main candidate management API
- **Key Endpoints**:
  - `POST /api/candidates/search` - Search candidates with pagination
  - `GET /api/candidates` - Get all candidates
  - `GET /api/candidates/{id}` - Get single candidate
  - `POST /api/candidates` - Create candidate
  - `PUT /api/candidates/{id}` - Update candidate
  - `DELETE /api/candidates/{id}` - Delete candidate
  - `GET /api/candidates/skills/frequency` - Skills word cloud data
  - `GET /api/candidates/status/totals` - Dashboard statistics
- **Dependencies**: RecruiterDbContext, FullTextSearchService, ILogger
- **Lines of Code**: ~520 lines

#### SearchController
- **Purpose**: Advanced full-text search
- **Key Endpoints**:
  - `POST /api/search/fts` - Pure PostgreSQL full-text search
- **Features**: Uses `candidate_search_view` materialized view
- **Lines of Code**: ~120 lines

#### ExcelImportController
- **Purpose**: Bulk candidate import from Excel files
- **Key Endpoints**:
  - `POST /api/excelimport/upload` - Upload and process Excel
  - `GET /api/excelimport/status/{jobId}` - Check import status
- **Features**: Background processing with Foundatio queue
- **Lines of Code**: ~80 lines

---

### 2. **Services** (Business Logic)

#### ExcelImportService
- **Purpose**: Process Excel files and import candidates
- **Key Methods**:
  - `ProcessExcelFileAsync()` - Parse and validate Excel
  - `ProcessCandidateRow()` - Extract candidate data
  - `ProcessCandidateSkills()` - Extract skills (currently basic)
- **Technologies**: NPOI library for Excel parsing
- **Status**: ⚠️ Skills extraction needs enhancement (uses only 19 skills)

#### FullTextSearchService
- **Purpose**: PostgreSQL full-text search operations
- **Features**: 
  - ts_vector based search
  - Relevance ranking with ts_rank
  - Trigram similarity matching
- **Performance**: Sub-second search on 651 candidates

#### SkillExtractionService (NEW)
- **Purpose**: Extract skills from resume text
- **Features**:
  - Word frequency analysis
  - Matches against 113 skills catalog
  - Handles skill variations (JS → JavaScript)
- **Status**: ✅ Created but not yet integrated

#### SemanticSearchService (NEW - Not Yet Implemented)
- **Purpose**: AI-powered semantic search with embeddings
- **Key Methods**:
  - `SemanticSearchCandidatesAsync()` - Vector similarity search
  - `HybridSearchAsync()` - Combine semantic + keyword search
  - `GenerateAllCandidateEmbeddingsAsync()` - Batch embedding generation
- **Status**: ⚠️ Service created, awaiting integration

#### OpenAIEmbeddingService (NEW - Not Yet Implemented)
- **Purpose**: Generate embeddings using OpenAI API
- **Model**: text-embedding-3-small (1536 dimensions)
- **Cost**: ~$0.00002 per 1K tokens
- **Status**: ⚠️ Service created, needs API key configuration

#### OllamaEmbeddingService (NEW - Not Yet Implemented)
- **Purpose**: Generate embeddings locally (free, private)
- **Model**: nomic-embed-text (768 dimensions)
- **Status**: ⚠️ Service created, needs Ollama Docker setup

---

### 3. **Models** (Database Entities)

#### Candidate
- **Core Fields**: Name, Email, Phone, Title, Experience, Status
- **Relationships**: 1:N with Resumes, Skills, WorkExperience, Education
- **Special Columns**: `search_vector` (tsvector), `profile_embedding` (vector - NEW)

#### Resume
- **Fields**: File metadata, `resume_text`, `resume_text_processed`
- **Size**: 651 resumes with text content
- **Special Columns**: `search_vector` (tsvector), `resume_embedding` (vector - NEW)

#### Skill
- **Fields**: `skill_name`, `category`, `description`
- **Count**: 113 comprehensive skills
- **Special Columns**: `search_vector` (tsvector), `skill_embedding` (vector - NEW)

#### CandidateSkill (Junction Table)
- **Purpose**: Many-to-many relationship between Candidates and Skills
- **Additional Fields**: `proficiency_level`, `years_of_experience`, `is_extracted`
- **Current Data**: 24,067 skill assignments across 651 candidates

---

## 🔌 Dependencies (NuGet Packages)

### Core Framework
- `Microsoft.AspNetCore` (v8.0) - ASP.NET Core framework
- `Swashbuckle.AspNetCore` (v6.5.0) - Swagger/OpenAPI

### Database
- `Npgsql.EntityFrameworkCore.PostgreSQL` (v8.0.0) - PostgreSQL provider
- `Microsoft.EntityFrameworkCore` (v8.0.0) - ORM framework
- `Npgsql` (v8.0.4) - PostgreSQL client

### Utilities
- `NPOI` (v2.7.1) - Excel file processing
- `Serilog.AspNetCore` (v8.0.0) - Structured logging
- `Foundatio` (v10.7.1) - Background job queue
- `AspNetCore.HealthChecks.Npgsql` (v8.0.0) - Health monitoring

### **Missing (Needed for Semantic Search)**
- ⚠️ No OpenAI/Azure.AI.OpenAI package yet
- ⚠️ No vector type support in EF Core models yet

---

## 📡 API Endpoints Summary

| Endpoint | Method | Purpose | Status |
|----------|--------|---------|--------|
| `/api/candidates` | GET | List all candidates | ✅ Working |
| `/api/candidates/{id}` | GET | Get single candidate | ✅ Working |
| `/api/candidates/search` | POST | Search with FTS | ✅ Working |
| `/api/candidates/skills/frequency` | GET | Skills word cloud | ✅ Working |
| `/api/candidates/status/totals` | GET | Dashboard stats | ✅ Working |
| `/api/search/fts` | POST | Advanced FTS | ✅ Working |
| `/api/excelimport/upload` | POST | Bulk import | ✅ Working |
| `/health` | GET | Health check | ✅ Working |
| `/swagger` | GET | API docs | ✅ Working |
| `/api/search/semantic` | POST | Semantic search | ❌ Not Yet Implemented |
| `/api/search/hybrid` | POST | Hybrid search | ❌ Not Yet Implemented |

---

## 🗄️ Database Schema

### Main Tables
```sql
candidates          (651 rows)
  ├── profile_embedding vector(1536)  -- NEW, not populated yet
  └── search_vector tsvector           -- Populated, working

resumes             (651 rows)
  ├── resume_text text                 -- Populated with content
  ├── resume_embedding vector(1536)    -- NEW, not populated yet
  └── search_vector tsvector           -- Populated, working

skills              (113 rows)
  ├── skill_name varchar(100)          -- Populated
  ├── skill_embedding vector(1536)     -- NEW, not populated yet
  └── search_vector tsvector           -- Populated, working

candidate_skills    (24,067 rows)
  ├── proficiency_level varchar(50)    -- Populated (Expert/Advanced/etc)
  └── years_of_experience int          -- Populated based on candidate experience
```

### Extensions
- ✅ `pg_trgm` - Trigram matching for fuzzy search
- ❌ `pgvector` - Vector similarity search (not installed yet)

---

## 🚀 Current Capabilities

### ✅ **Working Features**
1. **CRUD Operations** - Full candidate management
2. **Full-Text Search** - PostgreSQL ts_vector based search
3. **Fuzzy Matching** - Trigram similarity for typo tolerance
4. **Excel Import** - Bulk candidate upload with background processing
5. **Skills Management** - 113 skills catalog with 24K+ assignments
6. **Skills Word Cloud** - Real-time skill frequency aggregation
7. **Dashboard Statistics** - Status-based candidate counts
8. **Health Monitoring** - Database connectivity checks
9. **Swagger Documentation** - Interactive API explorer

### ⚠️ **Partially Implemented**
1. **Skill Extraction** - Basic service created, not integrated
2. **Semantic Search Services** - Code written, not integrated
3. **Embedding Services** - OpenAI/Ollama services ready, not configured

### ❌ **Not Yet Implemented**
1. **Vector Search** - pgvector extension not installed
2. **AI Embeddings** - No embeddings generated yet
3. **Hybrid Search** - Semantic + keyword combination
4. **Embedding Background Jobs** - Automatic embedding generation

---

## 🔧 Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=recruitingdb;..."
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### **Needed for Semantic Search**
```json
{
  "OpenAI": {
    "ApiKey": "sk-...",                    // ⚠️ Not configured yet
    "EmbeddingModel": "text-embedding-3-small"
  },
  "Ollama": {
    "Url": "http://localhost:11434",      // ⚠️ Not configured yet
    "EmbeddingModel": "nomic-embed-text"
  }
}
```

---

## 🎯 Integration Points for Semantic Search

### What Needs to Happen:

1. **Database Setup** (10 minutes)
   - Install pgvector extension
   - Add vector columns to tables
   - Create HNSW indexes

2. **Service Registration** (5 minutes)
   - Register IEmbeddingService in Program.cs
   - Choose OpenAI or Ollama implementation
   - Add configuration settings

3. **Generate Embeddings** (10-30 minutes one-time)
   - Run batch job to generate embeddings for 651 candidates
   - Store vectors in profile_embedding columns
   - Monitor progress and errors

4. **Add API Endpoints** (15 minutes)
   - Add semantic search endpoint
   - Add hybrid search endpoint
   - Update Swagger documentation

5. **Frontend Integration** (30 minutes)
   - Update search API calls
   - Add semantic search toggle
   - Display similarity scores

---

## 📈 Performance Metrics

### Current Performance
- **Candidate Search**: <50ms (full-text search)
- **Skills Frequency**: <100ms (aggregation of 24K records)
- **Excel Import**: ~2-5 seconds per 100 candidates
- **Database Queries**: Sub-second for most operations

### Expected Performance (with Semantic Search)
- **Embedding Generation**: ~50-200ms per candidate (one-time)
- **Semantic Search**: ~100-300ms (vector similarity)
- **Hybrid Search**: ~150-400ms (combined approach)

---

## 🎨 Architecture Patterns

### Design Patterns Used
1. **Repository Pattern** - Via Entity Framework DbContext
2. **Service Layer Pattern** - Business logic in services
3. **DTO Pattern** - Separate API contracts from entities
4. **Dependency Injection** - Built-in ASP.NET Core DI
5. **Background Service Pattern** - Excel processing queue

### Database Access Patterns
1. **EF Core LINQ** - Type-safe queries for CRUD operations
2. **Raw SQL** - For complex full-text search queries
3. **Stored Procedures** - Not used (prefer code-based logic)
4. **Migrations** - SQL scripts in Migrations folder

---

## 🔒 Security Considerations

### Current State
- ⚠️ **No Authentication** - API is publicly accessible
- ⚠️ **No Authorization** - No role-based access control
- ✅ **CORS Enabled** - Allows all origins (dev mode)
- ✅ **SQL Injection Protected** - Parameterized queries
- ⚠️ **API Keys Not Secured** - Would be in appsettings.json

### Recommendations for Production
1. Add JWT authentication
2. Implement role-based authorization
3. Store API keys in Azure Key Vault or AWS Secrets Manager
4. Restrict CORS to specific origins
5. Add rate limiting
6. Enable HTTPS only

---

## 📦 Docker Setup

### docker-compose.yml
```yaml
services:
  db:
    image: postgres:15
    environment:
      POSTGRES_PASSWORD: P3v2_S3cur3_Passw0rd
    ports:
      - "5433:5432"
  
  api:
    build: .
    ports:
      - "8080:8080"
    depends_on:
      - db
```

---

## 🎓 Key Learnings & Notes

### Strengths
- ✅ Clean separation of concerns
- ✅ Good use of Entity Framework Core
- ✅ Comprehensive logging with Serilog
- ✅ Full-text search working well
- ✅ Extensive skills catalog (113 skills)
- ✅ Real resume-based skill extraction

### Areas for Improvement
- ⚠️ Skill extraction in Excel import uses only 19 hardcoded skills
- ⚠️ No authentication/authorization
- ⚠️ Limited error handling in some controllers
- ⚠️ No caching layer
- ⚠️ Background jobs could use better monitoring

### Ready for Semantic Search Integration
- ✅ Services are written and ready
- ✅ Database schema prepared (migration scripts created)
- ⚠️ Needs configuration (API keys, Docker setup)
- ⚠️ Needs one-time embedding generation job

---

**Summary**: Well-structured .NET 8 API with solid foundation. Full-text search working great. Semantic search components are prepared and ready for integration. Main missing pieces are pgvector installation, embedding generation, and API configuration.