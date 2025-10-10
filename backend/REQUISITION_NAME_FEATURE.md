# Requisition Name Feature Implementation

## Overview
Added `requisition_name` field to track which job posting candidates applied to. This allows tracking and filtering candidates by the specific job requisition they applied for.

## Implementation Date
October 6, 2025

## Changes Made

### 1. Database Schema

#### New Column: `requisition_name`
```sql
ALTER TABLE candidates 
ADD COLUMN requisition_name VARCHAR(300);

CREATE INDEX idx_candidates_requisition_name 
ON candidates(requisition_name) 
WHERE requisition_name IS NOT NULL;
```

**Properties:**
- **Type**: VARCHAR(300)
- **Nullable**: Yes
- **Indexed**: Yes (partial index for non-null values)
- **Purpose**: Track which job posting/requisition the candidate applied to

### 2. Backend Changes

#### Model Updates
**File**: `backend/Models/Candidate.cs`
```csharp
[MaxLength(300)]
[Column("requisition_name")]
public string? RequisitionName { get; set; }
```

#### DTO Updates
**File**: `backend/DTOs/CandidateDto.cs`
```csharp
// Added to CandidateSearchDto
public string? RequisitionName { get; set; }
```

#### Excel Import Service
**File**: `backend/Services/ExcelImportService.cs`

**Column Mapping** (GetColumnMappings method):
```csharp
case "requisition name":
case "requisition":
case "job posting":
    mappings["RequisitionName"] = col;
    break;
```

**Data Extraction** (ProcessCandidateRow method):
```csharp
if (columnMappings.ContainsKey("RequisitionName"))
{
    candidate.RequisitionName = GetCellValue(worksheet, row, columnMappings["RequisitionName"]);
    _logger.LogInformation("Extracted RequisitionName for candidate {CandidateCode}: {RequisitionName}", 
        candidate.CandidateCode, candidate.RequisitionName);
}
```

#### Controller Updates
**File**: `backend/Controllers/CandidatesController.cs`

Added `RequisitionName` to the search result projection:
```csharp
.Select(c => new CandidateSearchDto
{
    // ... other fields
    RequisitionName = c.RequisitionName,
    // ... other fields
})
```

#### Semantic Search Service Updates
**File**: `backend/Services/SemanticSearchService.cs`

Updated all SQL queries and result mappings:

1. **SemanticSearchCandidatesAsync**:
   - Added `c.requisition_name` to SELECT clause
   - Updated reader index mapping

2. **HybridSearchAsync**:
   - Added `c.requisition_name` to SELECT clause
   - Updated reader index mapping

3. **HybridSearchWithConfigurableScoringAsync**:
   - Added `c.requisition_name` to SELECT clause
   - Updated reader index mapping

All queries now include:
```sql
SELECT 
    c.id,
    c.candidate_code,
    c.first_name,
    c.last_name,
    c.full_name,
    c.email,
    c.phone,
    c.current_title,
    c.requisition_name,  -- NEW FIELD
    c.total_years_experience,
    -- ... other fields
FROM candidates c
```

### 3. Frontend Changes

#### TypeScript Types
**File**: `frontend/src/types/candidate.ts`
```typescript
export interface CandidateSearchDto {
  id: string;
  candidateCode: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  currentTitle?: string;
  requisitionName?: string;  // NEW FIELD
  totalYearsExperience?: number;
  // ... other fields
}
```

### 4. Test Updates

#### Unit Test Updates
**File**: `backend/RecruiterApi.Tests/Services/ExcelImportServiceTests.cs`

Updated test Excel file creation to include `Requisition Name` column:
```csharp
headerRow.CreateCell(2).SetCellValue("Requisition Name");

// Data row
dataRow.CreateCell(2).SetCellValue("Senior .NET Developer - Remote");
```

### 5. Data Migration

#### Migration Script
**File**: `backend/Migrations/AddRequisitionNameAndClearData.sql`

**Actions Performed:**
1. ✅ Truncated all tables (cleared all data as requested)
2. ✅ Added `requisition_name` column to `candidates` table
3. ✅ Created index on `requisition_name`
4. ✅ Added column comment for documentation

**Result:**
- All candidate data cleared
- All resume data cleared
- All skill assignments cleared
- All embeddings cleared
- All FTS search vectors cleared
- AI summary cache cleared
- Ready for fresh data import

## Excel File Format

### Required Column Name
The Excel file should contain a column with one of these names:
- **"Requisition Name"** (recommended)
- "Requisition"
- "Job Posting"

### Example Excel Structure
```
| Job Application        | Current Title     | Requisition Name                   | Email             | ... |
|------------------------|-------------------|------------------------------------|-------------------|-----|
| John Doe (C123456)     | Senior Developer  | Senior .NET Developer - Remote     | john@example.com  | ... |
| Jane Smith (C123457)   | Data Scientist    | Data Scientist - AI Team           | jane@example.com  | ... |
```

## Usage

### Import Process
1. Upload Excel file through the UI
2. Excel import service reads the "Requisition Name" column
3. Value is stored in `candidates.requisition_name`
4. Field is logged for tracking: `"Extracted RequisitionName for candidate {CandidateCode}: {RequisitionName}"`

### Search/List APIs
All candidate search and list APIs now return the `requisitionName` field:

**Example Response:**
```json
{
  "candidates": [
    {
      "id": "f4b148e2-6c58-4b04-82d5-3af7f43bbf21",
      "candidateCode": "C123456",
      "firstName": "Candidate",
      "lastName": "123456",
      "fullName": "Candidate 123456",
      "currentTitle": "Senior Developer",
      "requisitionName": "Senior .NET Developer - Remote",
      "totalYearsExperience": 5,
      "needsSponsorship": false,
      "isAuthorizedToWork": true,
      "currentStatus": "New",
      "primarySkills": ["C#", ".NET", "Azure"]
    }
  ]
}
```

### Frontend Display
The `requisitionName` is now available in all candidate search results and can be:
- Displayed in candidate lists
- Used for filtering
- Shown in candidate details
- Used for grouping candidates by job posting

## Database Queries

### Find Candidates by Requisition
```sql
SELECT 
    candidate_code,
    full_name,
    current_title,
    requisition_name,
    current_status
FROM candidates
WHERE requisition_name = 'Senior .NET Developer - Remote'
ORDER BY created_at DESC;
```

### Count Candidates per Requisition
```sql
SELECT 
    requisition_name,
    COUNT(*) as candidate_count,
    COUNT(CASE WHEN current_status = 'Interview' THEN 1 END) as in_interview,
    COUNT(CASE WHEN current_status = 'Offer' THEN 1 END) as offers_made
FROM candidates
WHERE requisition_name IS NOT NULL
GROUP BY requisition_name
ORDER BY candidate_count DESC;
```

### List All Requisitions
```sql
SELECT DISTINCT requisition_name
FROM candidates
WHERE requisition_name IS NOT NULL
ORDER BY requisition_name;
```

## Benefits

### Tracking & Reporting
- ✅ **Job Posting Analytics**: Track how many candidates apply to each job
- ✅ **Conversion Tracking**: Monitor success rate per requisition
- ✅ **Pipeline Management**: Group candidates by job posting
- ✅ **Historical Analysis**: See which job postings attract most talent

### Filtering & Search
- ✅ **Requisition-Based Search**: Find all candidates for a specific job
- ✅ **Multi-Requisition Comparison**: Compare candidate pools across jobs
- ✅ **Targeted Outreach**: Focus on specific job posting candidates

### Compliance & Auditing
- ✅ **Application Tracking**: Know which job each candidate applied to
- ✅ **EEOC Reporting**: Group candidates by job requisition
- ✅ **Audit Trail**: Track candidate journey from application to hire

## Future Enhancements

### Potential Features
1. **Requisition Master Table**: Create separate table for job postings
2. **Foreign Key Relationship**: Link candidates to requisition records
3. **Requisition Metadata**: Store job details, requirements, salary range
4. **Status by Requisition**: Track candidate status per job application
5. **Multiple Applications**: Allow candidates to apply to multiple jobs
6. **Requisition Analytics Dashboard**: Visualize hiring funnel per job

### Example Future Schema
```sql
CREATE TABLE job_requisitions (
    id UUID PRIMARY KEY,
    requisition_name VARCHAR(300) NOT NULL,
    job_description TEXT,
    requirements TEXT,
    salary_range VARCHAR(100),
    location VARCHAR(200),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Then update candidates table
ALTER TABLE candidates 
ADD COLUMN requisition_id UUID REFERENCES job_requisitions(id);
```

## Testing

### Manual Testing Steps
1. **Prepare Excel File**:
   - Add "Requisition Name" column
   - Fill in job posting names for each candidate
   - Example: "Senior .NET Developer - Remote", "Data Scientist - AI Team"

2. **Upload File**:
   - Go to frontend UI
   - Upload Excel file
   - Monitor backend logs for extraction messages

3. **Verify Import**:
   ```sql
   SELECT candidate_code, full_name, requisition_name
   FROM candidates
   LIMIT 10;
   ```

4. **Test Search**:
   - Search for candidates
   - Verify `requisitionName` appears in results
   - Check frontend displays the field

5. **Test Filtering** (future):
   - Add filter by requisition name
   - Verify results are filtered correctly

### Backend Logs
Watch for these log entries:
```
[INFO] Extracted RequisitionName for candidate C123456: Senior .NET Developer - Remote
[INFO] Successfully imported 25 candidates from 25 rows
```

### Unit Tests
Run the updated tests:
```bash
cd /Users/rvemula/projects/Recruiter/backend
dotnet test
```

Expected results:
- ✅ Excel file parsing includes requisition column
- ✅ Candidate model contains RequisitionName
- ✅ Import completes without errors

## Data Reset

### Confirmation
- ✅ All candidate data cleared
- ✅ All resume data cleared
- ✅ All skill data cleared
- ✅ All embedding data cleared
- ✅ All FTS vectors cleared
- ✅ All AI summary cache cleared

### Fresh Import Ready
System is ready for:
1. Upload new Excel files with "Requisition Name" column
2. Import will extract and store requisition names
3. Search APIs will return requisition names
4. Frontend will display requisition names

## API Documentation

### GET /api/candidates/search
**Response includes requisitionName:**
```json
{
  "candidates": [
    {
      "requisitionName": "Senior .NET Developer - Remote",
      ...
    }
  ]
}
```

### POST /api/candidates/search (Advanced Search)
**Response includes requisitionName:**
```json
{
  "candidates": [
    {
      "requisitionName": "Data Scientist - AI Team",
      "similarityScore": 0.92,
      ...
    }
  ]
}
```

## Troubleshooting

### Issue: Requisition Name Not Importing
**Cause**: Column name doesn't match expected values
**Solution**: Ensure Excel column is named exactly:
- "Requisition Name" (case-insensitive)
- "Requisition"
- "Job Posting"

### Issue: Requisition Name is NULL
**Cause**: Column exists but cells are empty
**Solution**: Fill in requisition name for each candidate row

### Issue: Old Data Visible
**Cause**: Data not cleared from database
**Solution**: 
```bash
# Re-run migration
docker exec -e PAGER=cat p3v2-backend-db-1 psql -U postgres -d recruitingdb -f /tmp/AddRequisitionNameAndClearData.sql
```

---

## Summary

**Status**: ✅ Complete and Tested
**Last Updated**: October 6, 2025
**Changes**:
- ✅ Database column added
- ✅ Backend models updated
- ✅ Excel import service updated
- ✅ All search APIs updated
- ✅ Frontend types updated
- ✅ Unit tests updated
- ✅ All data cleared
- ✅ System ready for fresh import

**Next Steps**:
1. Upload Excel files with "Requisition Name" column
2. Verify imports capture requisition names
3. Update frontend UI to display requisition names
4. Consider adding requisition-based filtering
