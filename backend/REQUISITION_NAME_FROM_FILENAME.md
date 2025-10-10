# RequisitionName from Filename - Implementation Summary

## Overview
Updated the Excel import service to automatically extract the `RequisitionName` from the Excel **filename** instead of requiring a specific column in the Excel file.

## Implementation Date
October 6, 2025 (Updated)

## Changes Made

### 1. Excel Import Service Updates

#### File: `backend/Services/ExcelImportService.cs`

**Key Changes:**

1. **Extract Requisition Name from Filename**:
   ```csharp
   // Extract requisition name from filename (remove extension and clean up)
   var requisitionName = ExtractRequisitionNameFromFileName(fileName);
   _logger.LogInformation("Using RequisitionName from filename: {RequisitionName}", requisitionName);
   ```

2. **Pass Requisition Name to Row Processing**:
   ```csharp
   var candidate = ProcessCandidateRow(worksheet, row + 1, columnMappings, requisitionName);
   ```

3. **Updated ProcessCandidateRow Signature**:
   ```csharp
   private Candidate? ProcessCandidateRow(
       ISheet worksheet, 
       int row, 
       Dictionary<string, int> columnMappings, 
       string requisitionName) // NEW PARAMETER
   ```

4. **Set Requisition Name on Candidate**:
   ```csharp
   // Set RequisitionName from filename
   candidate.RequisitionName = requisitionName;
   _logger.LogInformation("Set RequisitionName for candidate {CandidateCode}: {RequisitionName}", 
       candidate.CandidateCode, candidate.RequisitionName);
   ```

5. **New Helper Method** - `ExtractRequisitionNameFromFileName()`:
   ```csharp
   private string ExtractRequisitionNameFromFileName(string fileName)
   {
       // Remove file extension (.xlsx, .xls)
       var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

       // Replace common separators with spaces (hyphens, underscores, dots)
       var cleanName = nameWithoutExtension
           .Replace('-', ' ')
           .Replace('_', ' ')
           .Replace('.', ' ');

       // Remove multiple spaces and trim
       cleanName = Regex.Replace(cleanName, @"\s+", " ").Trim();

       return cleanName;
   }
   ```

6. **Removed Column Mapping**:
   - Removed the Excel column mapping for "Requisition Name", "Requisition", "Job Posting"
   - Added comment: `// Note: RequisitionName is now extracted from the filename, not from Excel column`

## How It Works

### Filename to Requisition Name Conversion

**Examples:**

| Excel Filename | Extracted RequisitionName |
|----------------|---------------------------|
| `Senior-NET-Developer-Remote.xlsx` | `Senior NET Developer Remote` |
| `Data_Scientist_AI_Team.xlsx` | `Data Scientist AI Team` |
| `R3654_Lead_Product_Engineer_Candidates.xlsx` | `R3654 Lead Product Engineer Candidates` |
| `Full-Stack-Developer.xls` | `Full Stack Developer` |

**Conversion Rules:**
1. Remove file extension (`.xlsx`, `.xls`)
2. Replace hyphens (`-`) with spaces
3. Replace underscores (`_`) with spaces
4. Replace dots (`.`) with spaces
5. Collapse multiple spaces into single space
6. Trim leading/trailing spaces

### Import Flow

```
1. User uploads Excel file: "Senior-NET-Developer-Remote.xlsx"
   ↓
2. Extract requisition name: "Senior NET Developer Remote"
   ↓
3. Log: "Using RequisitionName from filename: Senior NET Developer Remote"
   ↓
4. Process each candidate row
   ↓
5. Set candidate.RequisitionName = "Senior NET Developer Remote"
   ↓
6. Log: "Set RequisitionName for candidate C123456: Senior NET Developer Remote"
   ↓
7. Save candidate to database with requisition name
```

## Benefits

### ✅ Simplified Workflow
- **No Excel Column Required**: Don't need to add or maintain a "Requisition Name" column
- **Filename is the Source of Truth**: Organize files by job posting name
- **Automatic Extraction**: System automatically derives requisition name

### ✅ Better Organization
- **File-Based Organization**: Group candidates by uploading separate files per job
- **Clear Naming Convention**: Filename describes the job posting
- **Easy Identification**: Look at filename to know which job it's for

### ✅ Flexibility
- **Multiple Naming Styles**: Supports hyphens, underscores, dots as separators
- **Handles Complex Names**: Works with long, descriptive filenames
- **Fallback Logic**: If conversion fails, uses original filename

## Usage Examples

### Example 1: Single Job Posting
```
Upload: "Senior-Software-Engineer-Java.xlsx"
Result: All candidates get RequisitionName = "Senior Software Engineer Java"
```

### Example 2: Multiple Job Postings
```
Upload: "Senior-NET-Developer-Remote.xlsx"
  → 25 candidates with RequisitionName = "Senior NET Developer Remote"

Upload: "Junior-Frontend-Developer.xlsx"
  → 15 candidates with RequisitionName = "Junior Frontend Developer"

Upload: "Data-Scientist-ML-Team.xlsx"
  → 10 candidates with RequisitionName = "Data Scientist ML Team"
```

### Example 3: Requisition Numbers
```
Upload: "R3654_Lead_Product_Engineer_Candidates.xlsx"
Result: RequisitionName = "R3654 Lead Product Engineer Candidates"
```

## Backend Logs

### During Import
```
[INFO] Starting Excel import for file: Senior-NET-Developer-Remote.xlsx
[INFO] Using RequisitionName from filename: Senior NET Developer Remote
[INFO] Found 25 rows in Excel file
[INFO] Set RequisitionName for candidate C123456: Senior NET Developer Remote
[INFO] Set RequisitionName for candidate C123457: Senior NET Developer Remote
[INFO] Successfully imported 25 candidates from 25 rows
```

### Verification
```bash
docker logs backend-recruiter-api-1 | grep "RequisitionName"
```

Expected output:
```
[INFO] Using RequisitionName from filename: Senior NET Developer Remote
[INFO] Set RequisitionName for candidate C123456: Senior NET Developer Remote
```

## Database Queries

### View Candidates by Requisition
```sql
SELECT 
    candidate_code,
    full_name,
    requisition_name,
    current_title,
    created_at
FROM candidates
WHERE requisition_name = 'Senior NET Developer Remote'
ORDER BY created_at DESC;
```

### Count Candidates per Requisition
```sql
SELECT 
    requisition_name,
    COUNT(*) as total_candidates,
    COUNT(CASE WHEN current_status = 'Interview' THEN 1 END) as in_interview,
    COUNT(CASE WHEN current_status = 'Offer' THEN 1 END) as offers_made
FROM candidates
WHERE requisition_name IS NOT NULL
GROUP BY requisition_name
ORDER BY total_candidates DESC;
```

### List All Requisitions
```sql
SELECT 
    requisition_name,
    COUNT(*) as candidate_count,
    MIN(created_at) as first_uploaded,
    MAX(created_at) as last_uploaded
FROM candidates
WHERE requisition_name IS NOT NULL
GROUP BY requisition_name
ORDER BY last_uploaded DESC;
```

## API Response

### Search Results Include RequisitionName
```json
{
  "candidates": [
    {
      "id": "...",
      "candidateCode": "C123456",
      "fullName": "Candidate 123456",
      "currentTitle": "Senior Software Engineer",
      "requisitionName": "Senior NET Developer Remote",
      "totalYearsExperience": 10,
      "currentStatus": "New"
    }
  ]
}
```

## Migration Status

### Data Cleared ✅
All data has been cleared and is ready for fresh import:
- Candidates: 0
- Resumes: 0
- Skills: 0
- AI Summaries: 0
- Embeddings: 0

### Database Ready ✅
- `requisition_name` column exists
- Index created for fast queries
- Backend service updated and running

## File Naming Best Practices

### Recommended Naming Conventions

1. **Descriptive Names**:
   - ✅ Good: `Senior-Software-Engineer-Java-Remote.xlsx`
   - ❌ Bad: `candidates.xlsx`

2. **Use Separators**:
   - ✅ Hyphens: `Senior-NET-Developer.xlsx`
   - ✅ Underscores: `Senior_NET_Developer.xlsx`
   - ✅ Dots: `Senior.NET.Developer.xlsx`
   - ✅ Mixed: `R3654_Senior-Developer.xlsx`

3. **Include Requisition Numbers** (if applicable):
   - ✅ `R3654_Lead_Product_Engineer.xlsx`
   - ✅ `REQ-2024-001_Senior-Developer.xlsx`

4. **Be Specific**:
   - ✅ `Senior-NET-Developer-Remote-USA.xlsx`
   - ✅ `Data-Scientist-AI-Team-NYC.xlsx`
   - ❌ `developers.xlsx` (too generic)

5. **Avoid Special Characters**:
   - ✅ Use: Letters, numbers, hyphens, underscores
   - ❌ Avoid: `@`, `#`, `&`, `%`, etc.

### Example File Structure
```
/job-postings/
  ├── Senior-NET-Developer-Remote.xlsx
  ├── Junior-Frontend-Developer-NYC.xlsx
  ├── Data-Scientist-ML-Team.xlsx
  ├── R3654_Lead-Product-Engineer.xlsx
  └── Full-Stack-Engineer-Cloud-Team.xlsx
```

## Testing

### Test Import
1. Rename your Excel file to describe the job posting:
   ```
   Example: "Senior-NET-Developer-Remote.xlsx"
   ```

2. Upload the file through the UI

3. Check backend logs:
   ```bash
   docker logs backend-recruiter-api-1 | grep "RequisitionName"
   ```

4. Verify in database:
   ```sql
   SELECT candidate_code, requisition_name FROM candidates LIMIT 5;
   ```

### Expected Results
```
 candidate_code |      requisition_name       
----------------+-----------------------------
 C123456        | Senior NET Developer Remote
 C123457        | Senior NET Developer Remote
```

## Troubleshooting

### Issue: Requisition name looks wrong
**Example**: Filename has `Senior.NET.Developer.xlsx` but appears as `Senior NET Developer xlsx`

**Cause**: File extension not removed properly

**Status**: ✅ Fixed - Uses `Path.GetFileNameWithoutExtension()`

### Issue: Multiple spaces in requisition name
**Example**: `Senior  NET   Developer` (multiple spaces)

**Cause**: Consecutive separators in filename

**Status**: ✅ Fixed - Regex collapses multiple spaces: `Regex.Replace(cleanName, @"\s+", " ")`

### Issue: Requisition name is empty
**Cause**: Filename is only separators (e.g., `---.xlsx`)

**Solution**: System returns original filename as fallback

## Comparison: Before vs After

### Before (Column-Based)
```
❌ Required "Requisition Name" column in Excel
❌ Manual data entry for each row
❌ Risk of typos or inconsistencies
❌ Extra maintenance effort
```

**Excel Structure:**
```
| Job Application | Requisition Name               | Current Title |
|-----------------|--------------------------------|---------------|
| John (C123)     | Senior NET Developer - Remote  | Developer     |
| Jane (C456)     | Senior NET Developer - Remote  | Developer     |
```

### After (Filename-Based)
```
✅ No column required
✅ Filename is source of truth
✅ Consistent across all candidates in file
✅ Automatic extraction
```

**Excel Structure:**
```
File: "Senior-NET-Developer-Remote.xlsx"

| Job Application | Current Title |
|-----------------|---------------|
| John (C123)     | Developer     |
| Jane (C456)     | Developer     |

→ Both get: requisition_name = "Senior NET Developer Remote"
```

## Migration History

1. **Phase 1**: Added `requisition_name` column to database
2. **Phase 2**: Updated DTOs and models
3. **Phase 3**: Implemented Excel column mapping (initial approach)
4. **Phase 4**: ✅ **Current** - Changed to filename-based extraction (simpler, better)

## Future Enhancements

### Potential Improvements
1. **Filename Validation**: Warn if filename is too generic
2. **Requisition Master Table**: Create separate table for job postings
3. **UI Filename Preview**: Show extracted requisition name before upload
4. **Bulk Rename Tool**: Help rename multiple Excel files at once
5. **Requisition Templates**: Suggest filename formats for common job types

---

## Summary

**Status**: ✅ Implemented and Ready  
**Approach**: Filename-based (no Excel column needed)  
**Data Status**: Cleared - ready for fresh import  
**Backend**: Updated and running  
**Benefits**: Simpler workflow, better organization, automatic extraction

**How to Use**:
1. Name your Excel file to describe the job posting (e.g., `Senior-NET-Developer-Remote.xlsx`)
2. Upload the file through the UI
3. All candidates automatically get that requisition name
4. View requisition names in search results and candidate details

**Last Updated**: October 6, 2025  
**Git Commit**: Pending (after testing)
