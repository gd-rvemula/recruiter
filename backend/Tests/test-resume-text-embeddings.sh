#!/bin/bash

###############################################################################
# Test Script: Resume Text Embedding Generation
# 
# Purpose: 
#   - Check how many candidates have resume text
#   - Test Excel import with focus on Resume Text column
#   - Verify embeddings are generated from resume content
#   - Test semantic search quality with resume-based embeddings
###############################################################################

set -e

API_URL="http://localhost:8080"
EXCEL_FILE="/Users/rvemula/projects/Recruiter/data/TestData.xlsx"
DB_CONTAINER="p3v2-backend-db-1"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Resume Text Embedding Test${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Step 1: Check candidates with resume text
echo -e "${YELLOW}Step 1: Checking candidates with resume text...${NC}"
RESUME_SQL="
SELECT 
    COUNT(DISTINCT c.id) as total_candidates,
    COUNT(DISTINCT CASE WHEN r.resume_text IS NOT NULL THEN c.id END) as with_resume_text,
    COUNT(DISTINCT CASE WHEN c.profile_embedding IS NOT NULL THEN c.id END) as with_embeddings
FROM candidates c
LEFT JOIN resumes r ON c.id = r.candidate_id
WHERE c.is_active = true;
"
echo -e "${BLUE}Candidate Resume Status:${NC}"
docker exec -e PAGER=cat "$DB_CONTAINER" psql -U postgres -d recruitingdb -c "$RESUME_SQL"
echo ""

# Step 2: Sample candidates with resume text
echo -e "${YELLOW}Step 2: Sample candidates with resume text and embeddings:${NC}"
SAMPLE_SQL="
SELECT 
    c.candidate_code,
    LEFT(c.first_name || ' ' || c.last_name, 25) as name,
    LEFT(c.current_title, 30) as title,
    CASE WHEN r.resume_text IS NOT NULL THEN '✓' ELSE '✗' END as has_resume,
    LENGTH(r.resume_text) as resume_length,
    CASE WHEN c.profile_embedding IS NOT NULL THEN '✓' ELSE '✗' END as has_embedding,
    c.embedding_model
FROM candidates c
LEFT JOIN resumes r ON c.id = r.candidate_id
WHERE c.is_active = true
ORDER BY c.created_at DESC
LIMIT 10;
"
docker exec -e PAGER=cat "$DB_CONTAINER" psql -U postgres -d recruitingdb -c "$SAMPLE_SQL"
echo ""

# Step 3: Import test Excel file
echo -e "${YELLOW}Step 3: Importing TestData.xlsx...${NC}"
BEFORE_COUNT=$(docker exec -e PAGER=cat "$DB_CONTAINER" psql -U postgres -d recruitingdb -t -c "SELECT COUNT(*) FROM candidates;")
BEFORE_COUNT=$(echo "$BEFORE_COUNT" | xargs)

UPLOAD_RESPONSE=$(curl -s -X POST \
  -H "Content-Type: multipart/form-data" \
  -F "file=@$EXCEL_FILE" \
  http://localhost:8080/api/excelimport/upload)

echo "$UPLOAD_RESPONSE" | jq '.'

IMPORTED=$(echo "$UPLOAD_RESPONSE" | jq -r '.candidatesImported // 0')
EMBEDDING_JOBS=$(echo "$UPLOAD_RESPONSE" | jq -r '.message' | grep -oP '\d+(?= embedding jobs queued)' || echo "0")

echo -e "   Candidates imported: ${BLUE}$IMPORTED${NC}"
echo -e "   Embedding jobs queued: ${BLUE}$EMBEDDING_JOBS${NC}"
echo ""

# Step 4: Wait for embeddings
echo -e "${YELLOW}Step 4: Waiting 30 seconds for embedding generation...${NC}"
for i in {1..30}; do echo -n "."; sleep 1; done
echo ""
echo ""

# Step 5: Check embedding results
echo -e "${YELLOW}Step 5: Checking embedding generation results...${NC}"
EMBEDDING_CHECK="
SELECT 
    candidate_code,
    LEFT(first_name || ' ' || last_name, 25) as name,
    embedding_model,
    embedding_tokens,
    ROUND(EXTRACT(EPOCH FROM (NOW() - embedding_generated_at)), 1) as seconds_ago
FROM candidates 
WHERE embedding_generated_at > NOW() - INTERVAL '2 minutes'
ORDER BY embedding_generated_at DESC;
"
echo -e "${BLUE}Recently Generated Embeddings:${NC}"
docker exec -e PAGER=cat "$DB_CONTAINER" psql -U postgres -d recruitingdb -c "$EMBEDDING_CHECK"
echo ""

# Step 6: Test semantic search with resume-relevant queries
echo -e "${YELLOW}Step 6: Testing semantic search with resume-relevant queries...${NC}"
echo ""

# Get a sample resume text to create a relevant query
SAMPLE_TITLE=$(docker exec -e PAGER=cat "$DB_CONTAINER" psql -U postgres -d recruitingdb -t -c \
  "SELECT current_title FROM candidates WHERE current_title IS NOT NULL AND profile_embedding IS NOT NULL LIMIT 1;" | xargs)

if [ -n "$SAMPLE_TITLE" ]; then
    echo -e "${BLUE}Testing search for: '$SAMPLE_TITLE'${NC}"
    SEARCH_RESPONSE=$(curl -s -X POST \
      -H "Content-Type: application/json" \
      -d "{
        \"query\": \"$SAMPLE_TITLE\",
        \"page\": 1,
        \"pageSize\": 5,
        \"similarityThreshold\": 0.3
      }" \
      http://localhost:8080/api/semanticsearch/search)
    
    RESULT_COUNT=$(echo "$SEARCH_RESPONSE" | jq -r '.candidates | length')
    echo -e "   Results found: ${GREEN}$RESULT_COUNT${NC}"
    
    if [ "$RESULT_COUNT" -gt 0 ]; then
        echo -e "${BLUE}Top Results:${NC}"
        echo "$SEARCH_RESPONSE" | jq -r '.candidates[] | "   \(.firstName) \(.lastName) - \(.currentTitle // "N/A") (Similarity: \(.similarityScore // 0 | tonumber | . * 100 | round / 100))"'
    fi
fi
echo ""

# Step 7: Check API logs for resume text processing
echo -e "${YELLOW}Step 7: Checking logs for resume text processing...${NC}"
echo -e "${BLUE}Recent embedding logs:${NC}"
docker logs backend-recruiter-api-1 2>&1 | grep -E "Skipping candidate|Truncating resume|embedding job|Successfully generated" | tail -15
echo ""

# Summary
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Summary${NC}"
echo -e "${BLUE}========================================${NC}"
echo -e "Focus: Resume Text is now the PRIMARY data source for embeddings"
echo -e "Change: Candidates without resume text are SKIPPED"
echo -e "Benefit: Higher quality semantic search based on actual resume content"
echo ""
echo -e "${GREEN}✅ Test Complete!${NC}"
echo -e "Check the results above to verify embeddings are being generated from resume text."
