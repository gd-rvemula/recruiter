# AI Summary Caching - Testing Guide

## Quick Test Instructions

### Prerequisites
- Backend running on `http://localhost:8080`
- Frontend running on `http://localhost:5173`
- PostgreSQL container with `ai_summaries` table created

### Test 1: Cache Miss (First Request)

1. **Open Frontend**: `http://localhost:5173`
2. **Search for Candidates**: Use any search term or click "Search" without a term
3. **Select a Candidate**: Click on any candidate from the left sidebar
4. **Click AI Summarize**: Click the purple "AI Summarize" button next to the candidate code
5. **Observe**:
   - Loading spinner appears
   - Takes ~2-3 seconds (calling Azure OpenAI)
   - Modal displays the AI-generated summary
   
6. **Check Backend Logs**:
```bash
docker logs --tail 20 backend-recruiter-api-1 | grep "AISummary"
```

**Expected Output**:
```
[AISummary] Cache MISS for Resume <resume-id> - Calling Azure OpenAI
[AISummary] Successfully generated and cached summary
```

### Test 2: Cache Hit (Second Request - Same Candidate)

1. **Close the Modal**: Click the "Close" button
2. **Click AI Summarize Again**: Same candidate, same button
3. **Observe**:
   - Loading spinner appears briefly
   - Returns in ~0.06 seconds (50x faster!)
   - Same summary appears instantly
   
4. **Check Backend Logs**:
```bash
docker logs --tail 20 backend-recruiter-api-1 | grep "AISummary"
```

**Expected Output**:
```
[AISummary] Cache HIT for Resume <resume-id> - Returning cached summary
```

### Test 3: Verify Cache in Database

```bash
# Check cache entries
docker exec -e PAGER=cat p3v2-backend-db-1 psql -U postgres -d recruitingdb -c \
  "SELECT 
    id, 
    candidate_id, 
    resume_id, 
    LENGTH(summary_text) as summary_length, 
    LEFT(resume_text_hash, 16) as hash_prefix,
    created_at 
  FROM ai_summaries 
  ORDER BY created_at DESC 
  LIMIT 5;"
```

**Expected Output**:
```
                  id                  |             candidate_id             |              resume_id               | summary_length | hash_prefix | created_at
--------------------------------------+--------------------------------------+--------------------------------------+----------------+-------------+----------------------------
 9262a54b-2f4e-4cc3-982b-6b5e9fa7f65c | f4b148e2-6c58-4b04-82d5-3af7f43bbf21 | 6b75ead6-aec6-4982-b73f-7fe12b866ffc |           1060 | 846bbfab403ce362 | 2025-10-06 21:46:29.865225+00
```

### Test 4: Cache Hit Rate

```bash
# Count total cache entries
docker exec -e PAGER=cat p3v2-backend-db-1 psql -U postgres -d recruitingdb -c \
  "SELECT COUNT(*) as total_cached_summaries FROM ai_summaries;"
```

### Test 5: Performance Comparison

**First Call (Cache Miss)**:
```bash
time curl -s -X POST http://localhost:8080/api/ai-summary \
  -H "Content-Type: application/json" \
  -d '{
    "candidateId": "<your-candidate-id>",
    "resumeId": "<your-resume-id>",
    "resumeText": "Senior Software Engineer..."
  }' > /dev/null
```
Expected: ~2-3 seconds

**Second Call (Cache Hit)**:
```bash
# Same command as above
```
Expected: ~0.06 seconds (50x faster!)

## Visual Indicators

### Frontend UI
- **Loading State**: Purple spinner with "Generating AI summary..."
- **Modal Header**: Purple/blue gradient with "AI Resume Summary"
- **Summary Display**: White text box with gradient border
- **Close Button**: Blue button at bottom

### Backend Logs
- **Cache Miss**: `[AISummary] Cache MISS for Resume <id> - Calling Azure OpenAI`
- **Cache Hit**: `[AISummary] Cache HIT for Resume <id> - Returning cached summary`
- **Azure Call**: Shows Azure OpenAI endpoint and API call details
- **Success**: `[AISummary] Successfully generated and cached summary`

## Expected Behavior

### ✅ Cache Hit (Repeated Request)
- ⚡ **Fast**: ~0.06 seconds
- 💰 **Free**: No Azure OpenAI API call
- 📊 **Database**: 1 SELECT query only
- 🔄 **Same Result**: Identical summary returned

### ✅ Cache Miss (New Resume / Changed Content)
- ⏱️ **Slower**: ~2-3 seconds
- 💳 **Costs**: ~$0.002 for Azure OpenAI API call
- 📊 **Database**: 1 SELECT (check) + 1 INSERT (save)
- ✨ **New Result**: Fresh summary generated

## Troubleshooting

### Issue: Always Cache Miss
**Cause**: Resume text differs slightly (whitespace, newlines)
**Solution**: Resume text hash must be identical

### Issue: Error "Foreign key constraint violation"
**Cause**: Candidate or Resume ID doesn't exist
**Solution**: Use valid IDs from database

### Issue: No summary displayed
**Cause**: Check browser console for errors
**Solution**: Open DevTools (F12) → Console tab

### Issue: Backend not responding
**Cause**: Backend container not running
**Solution**: 
```bash
cd /Users/rvemula/projects/Recruiter/backend
docker compose up -d --build
```

## Success Metrics

After testing, you should see:
- ✅ First call: ~2-3 seconds (Cache MISS)
- ✅ Second call: ~0.06 seconds (Cache HIT)
- ✅ Cache entries in database
- ✅ Backend logs show HIT/MISS correctly
- ✅ Same summary returned on repeated requests
- ✅ Cost savings: $0.00 for cached requests

## Clean Up (Optional)

```bash
# Clear all cached summaries
docker exec -e PAGER=cat p3v2-backend-db-1 psql -U postgres -d recruitingdb -c \
  "DELETE FROM ai_summaries;"

# Verify deletion
docker exec -e PAGER=cat p3v2-backend-db-1 psql -U postgres -d recruitingdb -c \
  "SELECT COUNT(*) FROM ai_summaries;"
```

---

**Status**: Ready for testing
**Last Updated**: October 6, 2025
