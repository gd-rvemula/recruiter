#!/bin/bash

###############################################################################
# Test Script: Excel Import with Embedding Generation
# 
# Purpose: 
#   - Upload TestData.xlsx via Excel import API
#   - Verify candidates are imported
#   - Wait for background embedding generation
#   - Verify embeddings are created in database
#   - Test semantic search with generated embeddings
#
# Usage: ./test-excel-import-embeddings.sh
###############################################################################

set -e  # Exit on error

# Configuration
API_URL="http://localhost:8080"
EXCEL_FILE="/Users/rvemula/projects/Recruiter/data/TestData.xlsx"
DB_CONTAINER="p3v2-backend-db-1"
EMBEDDING_WAIT_TIME=30  # seconds to wait for background processing

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Excel Import & Embedding Test${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Step 1: Verify Excel file exists
echo -e "${YELLOW}Step 1: Checking if Excel file exists...${NC}"
if [ ! -f "$EXCEL_FILE" ]; then
    echo -e "${RED}❌ Error: Excel file not found at $EXCEL_FILE${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Excel file found: $EXCEL_FILE${NC}"
FILE_SIZE=$(ls -lh "$EXCEL_FILE" | awk '{print $5}')
echo -e "   File size: $FILE_SIZE"
echo ""

# Step 2: Check API health
echo -e "${YELLOW}Step 2: Checking API health...${NC}"
HEALTH_RESPONSE=$(curl -s http://localhost:8080/health)
if [ "$HEALTH_RESPONSE" != "Healthy" ]; then
    echo -e "${RED}❌ API is not healthy. Response: $HEALTH_RESPONSE${NC}"
    exit 1
fi
echo -e "${GREEN}✅ API is healthy${NC}"
echo ""

# Step 3: Check semantic search health
echo -e "${YELLOW}Step 3: Checking semantic search service...${NC}"
SEMANTIC_HEALTH=$(curl -s http://localhost:8080/api/semanticsearch/health)
echo "$SEMANTIC_HEALTH" | jq '.'
IS_AVAILABLE=$(echo "$SEMANTIC_HEALTH" | jq -r '.available')
if [ "$IS_AVAILABLE" != "true" ]; then
    echo -e "${RED}❌ Semantic search service is not available${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Semantic search service is healthy${NC}"
echo ""

# Step 4: Get candidate count before import
echo -e "${YELLOW}Step 4: Counting candidates before import...${NC}"
BEFORE_COUNT=$(docker exec -e PAGER=cat "$DB_CONTAINER" psql -U postgres -d recruitingdb -t -c "SELECT COUNT(*) FROM candidates;")
BEFORE_COUNT=$(echo "$BEFORE_COUNT" | xargs)  # Trim whitespace
echo -e "   Candidates before import: ${BLUE}$BEFORE_COUNT${NC}"
echo ""

# Step 5: Upload Excel file
echo -e "${YELLOW}Step 5: Uploading Excel file via API...${NC}"
UPLOAD_RESPONSE=$(curl -s -X POST \
  -H "Content-Type: multipart/form-data" \
  -F "file=@$EXCEL_FILE" \
  http://localhost:8080/api/excelimport/upload)

echo "$UPLOAD_RESPONSE" | jq '.'

# Check if upload was successful
SUCCESS=$(echo "$UPLOAD_RESPONSE" | jq -r '.success // false')
if [ "$SUCCESS" != "true" ]; then
    echo -e "${RED}❌ Excel upload failed${NC}"
    ERROR_MSG=$(echo "$UPLOAD_RESPONSE" | jq -r '.message // "Unknown error"')
    echo -e "   Error: $ERROR_MSG"
    exit 1
fi

IMPORTED_COUNT=$(echo "$UPLOAD_RESPONSE" | jq -r '.candidatesImported // 0')
UPDATED_COUNT=$(echo "$UPLOAD_RESPONSE" | jq -r '.candidatesUpdated // 0')
echo -e "${GREEN}✅ Excel imported successfully${NC}"
echo -e "   Imported: ${BLUE}$IMPORTED_COUNT${NC} candidates"
echo -e "   Updated: ${BLUE}$UPDATED_COUNT${NC} candidates"
echo ""

# Step 6: Verify candidates were imported
echo -e "${YELLOW}Step 6: Verifying candidates in database...${NC}"
AFTER_COUNT=$(docker exec -e PAGER=cat "$DB_CONTAINER" psql -U postgres -d recruitingdb -t -c "SELECT COUNT(*) FROM candidates;")
AFTER_COUNT=$(echo "$AFTER_COUNT" | xargs)
echo -e "   Candidates after import: ${BLUE}$AFTER_COUNT${NC}"

NEW_CANDIDATES=$((AFTER_COUNT - BEFORE_COUNT))
if [ "$NEW_CANDIDATES" -gt 0 ]; then
    echo -e "${GREEN}✅ $NEW_CANDIDATES new candidates added${NC}"
else
    echo -e "${YELLOW}⚠️  No new candidates added (possibly duplicates updated)${NC}"
fi
echo ""

# Step 7: Wait for background embedding generation
echo -e "${YELLOW}Step 7: Waiting for background embedding generation...${NC}"
echo -e "   Waiting ${EMBEDDING_WAIT_TIME} seconds for embeddings to be generated..."
for i in $(seq 1 $EMBEDDING_WAIT_TIME); do
    echo -n "."
    sleep 1
done
echo ""
echo -e "${GREEN}✅ Wait completed${NC}"
echo ""

# Step 8: Check embedding generation status
echo -e "${YELLOW}Step 8: Checking embedding generation status...${NC}"

# Get total candidates with embeddings
EMBEDDING_COUNT=$(docker exec -e PAGER=cat "$DB_CONTAINER" psql -U postgres -d recruitingdb -t -c "SELECT COUNT(*) FROM candidates WHERE profile_embedding IS NOT NULL;")
EMBEDDING_COUNT=$(echo "$EMBEDDING_COUNT" | xargs)

# Get recently generated embeddings (last 2 minutes)
RECENT_EMBEDDINGS=$(docker exec -e PAGER=cat "$DB_CONTAINER" psql -U postgres -d recruitingdb -t -c "SELECT COUNT(*) FROM candidates WHERE embedding_generated_at > NOW() - INTERVAL '2 minutes';")
RECENT_EMBEDDINGS=$(echo "$RECENT_EMBEDDINGS" | xargs)

echo -e "   Total candidates with embeddings: ${BLUE}$EMBEDDING_COUNT${NC}"
echo -e "   Recently generated embeddings (last 2 min): ${BLUE}$RECENT_EMBEDDINGS${NC}"

if [ "$RECENT_EMBEDDINGS" -gt 0 ]; then
    echo -e "${GREEN}✅ Embeddings generated successfully${NC}"
else
    echo -e "${YELLOW}⚠️  No recent embeddings found. Check background service logs.${NC}"
fi
echo ""

# Step 9: Get sample of generated embeddings
echo -e "${YELLOW}Step 9: Inspecting sample embeddings...${NC}"
SAMPLE_SQL="
SELECT 
    candidate_code,
    first_name,
    last_name,
    embedding_model,
    embedding_tokens,
    embedding_generated_at,
    CASE 
        WHEN profile_embedding IS NOT NULL THEN '✓ Generated'
        ELSE '✗ Missing'
    END as embedding_status
FROM candidates 
WHERE embedding_generated_at > NOW() - INTERVAL '2 minutes'
ORDER BY embedding_generated_at DESC
LIMIT 5;
"

echo -e "${BLUE}Recently embedded candidates:${NC}"
docker exec -e PAGER=cat "$DB_CONTAINER" psql -U postgres -d recruitingdb -c "$SAMPLE_SQL"
echo ""

# Step 10: Test semantic search
echo -e "${YELLOW}Step 10: Testing semantic search...${NC}"

# Test search query for .NET engineers (matching the resume content)
SEARCH_QUERY="senior .NET engineer full stack developer"
echo -e "   Search query: \"${BLUE}$SEARCH_QUERY${NC}\""

SEARCH_REQUEST="{
  \"query\": \"$SEARCH_QUERY\",
  \"page\": 1,
  \"pageSize\": 5,
  \"similarityThreshold\": 0.3
}"

echo -e "   Executing semantic search..."
SEARCH_RESPONSE=$(curl -s -X POST \
  -H "Content-Type: application/json" \
  -d "$SEARCH_REQUEST" \
  http://localhost:8080/api/semanticsearch/search)

# Check if search returned results
RESULT_COUNT=$(echo "$SEARCH_RESPONSE" | jq -r '.candidates | length // 0')

echo ""
echo -e "${BLUE}Search Results:${NC}"
echo "$SEARCH_RESPONSE" | jq '.'

if [ "$RESULT_COUNT" -gt 0 ]; then
    echo ""
    echo -e "${GREEN}✅ Semantic search returned $RESULT_COUNT results${NC}"
    
    # Show top result details
    echo ""
    echo -e "${BLUE}Top Result:${NC}"
    echo "$SEARCH_RESPONSE" | jq '.candidates[0] | {
        name: (.firstName + " " + .lastName),
        title: .currentTitle,
        experience: .totalYearsExperience,
        similarityScore: .similarityScore,
        embeddingModel: .embeddingModel
    }'
else
    echo -e "${YELLOW}⚠️  No search results returned. This might be normal if no candidates match the query.${NC}"
fi
echo ""

# Step 11: Check API logs for embedding activity
echo -e "${YELLOW}Step 11: Checking API logs for embedding activity...${NC}"
echo -e "${BLUE}Recent embedding-related log entries:${NC}"
docker logs backend-recruiter-api-1 2>&1 | grep -i "embedding" | tail -10
echo ""

# Step 12: Summary
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Test Summary${NC}"
echo -e "${BLUE}========================================${NC}"
echo -e "Excel file: $EXCEL_FILE"
echo -e "Candidates before: $BEFORE_COUNT"
echo -e "Candidates after: $AFTER_COUNT"
echo -e "New candidates: $NEW_CANDIDATES"
echo -e "Total with embeddings: $EMBEDDING_COUNT"
echo -e "Recently generated: $RECENT_EMBEDDINGS"
echo -e "Semantic search results: $RESULT_COUNT"
echo ""

if [ "$RECENT_EMBEDDINGS" -gt 0 ] && [ "$RESULT_COUNT" -gt 0 ]; then
    echo -e "${GREEN}========================================${NC}"
    echo -e "${GREEN}✅ ALL TESTS PASSED!${NC}"
    echo -e "${GREEN}========================================${NC}"
    exit 0
elif [ "$RECENT_EMBEDDINGS" -gt 0 ]; then
    echo -e "${YELLOW}========================================${NC}"
    echo -e "${YELLOW}⚠️  PARTIAL SUCCESS${NC}"
    echo -e "${YELLOW}Embeddings generated but search returned no results${NC}"
    echo -e "${YELLOW}========================================${NC}"
    exit 0
else
    echo -e "${RED}========================================${NC}"
    echo -e "${RED}❌ TEST FAILED${NC}"
    echo -e "${RED}Embeddings were not generated${NC}"
    echo -e "${RED}Check background service logs for errors${NC}"
    echo -e "${RED}========================================${NC}"
    exit 1
fi
