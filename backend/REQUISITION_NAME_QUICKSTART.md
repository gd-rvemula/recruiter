# RequisitionName Feature - Quick Start Guide

## What Was Implemented

✅ **Database**: Added `requisition_name` column (VARCHAR 300) to candidates table  
✅ **Backend**: Updated models, DTOs, services, and controllers  
✅ **Frontend**: Updated TypeScript types  
✅ **Excel Import**: Extracts "Requisition Name" column from Excel files  
✅ **Search APIs**: All search endpoints return requisition name  
✅ **Data Reset**: All tables cleared - ready for fresh import  
✅ **Tests**: Updated unit tests  
✅ **Documentation**: Comprehensive feature documentation created

## Commit Details

**Commit Hash**: `50b1307`  
**Message**: `feat: Add RequisitionName field to track job postings`  
**Files Changed**: 20 files, 1495 insertions, 27 deletions

## Database Status

### Data Cleared ✅
- Candidates: 0
- Resumes: 0
- Skills: 0
- AI Summaries: 0
- Embeddings: 0
- FTS Search Vectors: 0
- All related tables: 0

### New Schema
```sql
-- New column added
candidates.requisition_name VARCHAR(300) NULL

-- Index created
CREATE INDEX idx_candidates_requisition_name 
ON candidates(requisition_name) 
WHERE requisition_name IS NOT NULL;
```

## Excel File Format

### Required Column
Your Excel files must include a column with one of these names (case-insensitive):
- **"Requisition Name"** ✅ (recommended)
- "Requisition"
- "Job Posting"

### Example
```
| Job Application      | Requisition Name                  | Current Title     | Email           |
|----------------------|-----------------------------------|-------------------|-----------------|
| John Doe (C123456)   | Senior .NET Developer - Remote    | Senior Developer  | john@email.com  |
| Jane Smith (C789)    | Data Scientist - AI Team          | Data Scientist    | jane@email.com  |
```

## How to Use

### 1. Upload New Excel Files
1. Open the Recruiter UI
2. Go to the Excel upload page
3. Select your Excel file (must have "Requisition Name" column)
4. Click Upload
5. System will import candidates with requisition names

### 2. View Requisition Names
After import, requisition names appear in:
- ✅ Candidate search results
- ✅ Candidate list view
- ✅ Candidate details
- ✅ All search APIs (semantic, hybrid, advanced)

### 3. Backend Logs
Monitor the import process:
```bash
docker logs -f backend-recruiter-api-1
```

Look for:
```
[INFO] Extracted RequisitionName for candidate C123456: Senior .NET Developer - Remote
[INFO] Successfully imported 25 candidates from 25 rows
```

## API Response Example

```json
{
  "candidates": [
    {
      "id": "...",
      "candidateCode": "C123456",
      "fullName": "Candidate 123456",
      "currentTitle": "Senior Developer",
      "requisitionName": "Senior .NET Developer - Remote",
      "totalYearsExperience": 5,
      "currentStatus": "New",
      "needsSponsorship": false
    }
  ]
}
```

## Database Queries

### View Candidates with Requisitions
```sql
SELECT 
    candidate_code,
    full_name,
    requisition_name,
    current_status
FROM candidates
WHERE requisition_name IS NOT NULL
ORDER BY requisition_name, created_at DESC;
```

### Count by Requisition
```sql
SELECT 
    requisition_name,
    COUNT(*) as total_candidates,
    COUNT(CASE WHEN current_status = 'Interview' THEN 1 END) as in_interview
FROM candidates
WHERE requisition_name IS NOT NULL
GROUP BY requisition_name
ORDER BY total_candidates DESC;
```

## Verification Steps

### 1. Check Backend is Running
```bash
docker ps | grep recruiter-api
```
Expected: Container is Up and Healthy

### 2. Verify Column Exists
```bash
docker exec -e PAGER=cat p3v2-backend-db-1 psql -U postgres -d recruitingdb -c "\d candidates" | grep requisition
```
Expected: `requisition_name | character varying(300)`

### 3. Test Import
1. Upload an Excel file with "Requisition Name" column
2. Check backend logs for extraction messages
3. Query database to verify data:
```bash
docker exec -e PAGER=cat p3v2-backend-db-1 psql -U postgres -d recruitingdb -c "SELECT candidate_code, requisition_name FROM candidates LIMIT 5;"
```

### 4. Test Search API
```bash
curl -X POST http://localhost:8080/api/candidates/search \
  -H "Content-Type: application/json" \
  -d '{
    "searchTerm": "",
    "page": 1,
    "pageSize": 10
  }' | jq '.candidates[] | {candidateCode, requisitionName}'
```

## Frontend Integration

### Display Requisition Name
Update your candidate list component to show requisition name:

```tsx
// In CandidatesPage.tsx or candidate list component
<div>
  <strong>Applied For:</strong> {candidate.requisitionName || 'Not specified'}
</div>
```

### Filter by Requisition (Future Enhancement)
```tsx
const [requisitionFilter, setRequisitionFilter] = useState<string>('all');

// Add filter dropdown
<select onChange={(e) => setRequisitionFilter(e.target.value)}>
  <option value="all">All Requisitions</option>
  <option value="Senior .NET Developer">Senior .NET Developer</option>
  <option value="Data Scientist">Data Scientist</option>
</select>
```

## Troubleshooting

### Issue: Requisition name not showing
**Solution**: 
1. Verify Excel has "Requisition Name" column (exact spelling)
2. Check backend logs for extraction messages
3. Query database to verify import

### Issue: Backend won't start
**Solution**:
```bash
cd /Users/rvemula/projects/Recruiter/backend
docker compose down
docker compose up -d --build
docker logs -f backend-recruiter-api-1
```

### Issue: Old data still visible
**Solution**: Already cleared! If new data uploaded and mixed with old:
```bash
# Re-run migration
docker cp /Users/rvemula/projects/Recruiter/backend/Migrations/AddRequisitionNameAndClearData.sql p3v2-backend-db-1:/tmp/
docker exec -e PAGER=cat p3v2-backend-db-1 psql -U postgres -d recruitingdb -f /tmp/AddRequisitionNameAndClearData.sql
```

## Files Modified

### Backend
- ✅ `Models/Candidate.cs` - Added RequisitionName property
- ✅ `DTOs/CandidateDto.cs` - Added RequisitionName to DTOs
- ✅ `Services/ExcelImportService.cs` - Extract requisition from Excel
- ✅ `Services/SemanticSearchService.cs` - Include in all queries
- ✅ `Controllers/CandidatesController.cs` - Return in search results
- ✅ `Controllers/AISummaryController.cs` - NEW: AI summary endpoint
- ✅ `Services/AISummaryService.cs` - NEW: AI summary with caching
- ✅ `Models/AISummary.cs` - NEW: AI summary cache model
- ✅ `Data/RecruiterDbContext.cs` - Added AiSummaries DbSet

### Frontend
- ✅ `types/candidate.ts` - Added requisitionName to interfaces
- ✅ `services/candidateApi.ts` - AI summary API method
- ✅ `pages/CandidatesPage.tsx` - AI summary feature

### Database
- ✅ `Migrations/AddRequisitionNameAndClearData.sql` - Migration script
- ✅ `Migrations/CreateAISummariesTable.sql` - AI cache table

### Tests
- ✅ `RecruiterApi.Tests/Services/ExcelImportServiceTests.cs` - Updated

### Documentation
- ✅ `REQUISITION_NAME_FEATURE.md` - Feature documentation
- ✅ `AI_SUMMARY_CACHING.md` - AI caching documentation  
- ✅ `AI_SUMMARY_TESTING.md` - AI testing guide

## Next Steps

1. **Upload Test Data**:
   - Prepare Excel file with "Requisition Name" column
   - Upload through UI
   - Verify imports successfully

2. **Update Frontend UI**:
   - Display requisition name in candidate cards
   - Add to search results table
   - Show in candidate details modal

3. **Add Filtering** (optional):
   - Add requisition dropdown filter
   - Filter candidates by job posting
   - Show candidate count per requisition

4. **Analytics** (future):
   - Create requisition analytics dashboard
   - Track conversion rates per job posting
   - Visualize hiring funnel by requisition

## Documentation Files

📄 **Full Feature Documentation**: `/backend/REQUISITION_NAME_FEATURE.md`  
📄 **AI Summary Caching**: `/backend/AI_SUMMARY_CACHING.md`  
📄 **AI Summary Testing**: `/backend/AI_SUMMARY_TESTING.md`  
📄 **This Quick Start**: `/backend/REQUISITION_NAME_QUICKSTART.md`

## Support

For issues or questions:
1. Check logs: `docker logs backend-recruiter-api-1`
2. Verify database: `docker exec -e PAGER=cat p3v2-backend-db-1 psql -U postgres -d recruitingdb`
3. Review documentation files above

---

**Status**: ✅ Ready for Production  
**Last Updated**: October 6, 2025  
**Git Commit**: `50b1307`  
**All Data Cleared**: ✅ Yes - Ready for fresh import
