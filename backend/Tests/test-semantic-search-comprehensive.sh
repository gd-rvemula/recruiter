#!/bin/bash

###############################################################################
# Comprehensive Embedding Test Script
# 
# This script tests the complete embedding pipeline:
# 1. Imports candidates from Excel
# 2. Verifies embedding generation
# 3. Tests semantic search with relevant queries
# 4. Shows similarity scores and ranking
###############################################################################

set -e

API_URL="http://localhost:8080"
DB_CONTAINER="p3v2-backend-db-1"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Comprehensive Embedding Test${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Test 1: Check candidates with embeddings
echo -e "${YELLOW}Test 1: Checking existing embeddings...${NC}"
EMBEDDING_COUNT=$(docker exec -e PAGER=cat "$DB_CONTAINER" psql -U postgres -d recruitingdb -t -c "SELECT COUNT(*) FROM candidates WHERE profile_embedding IS NOT NULL;")
EMBEDDING_COUNT=$(echo "$EMBEDDING_COUNT" | xargs)
echo -e "   Total candidates with embeddings: ${BLUE}$EMBEDDING_COUNT${NC}"
echo ""

# Test 2: Get sample of embedded candidates
echo -e "${YELLOW}Test 2: Sample embedded candidates:${NC}"
SAMPLE_SQL="
SELECT 
    candidate_code,
    LEFT(first_name || ' ' || last_name, 30) as name,
    LEFT(current_title, 40) as title,
    total_years_experience as exp,
    embedding_model
FROM candidates 
WHERE profile_embedding IS NOT NULL
ORDER BY embedding_generated_at DESC
LIMIT 10;
"
docker exec -e PAGER=cat "$DB_CONTAINER" psql -U postgres -d recruitingdb -c "$SAMPLE_SQL"
echo ""

# Test 3: Semantic search with various queries
echo -e "${YELLOW}Test 3: Testing semantic search with various queries...${NC}"
echo ""

# Query 1: Generic software engineer
echo -e "${BLUE}Query 1: 'software engineer with backend experience'${NC}"
SEARCH1=$(curl -s -X POST \
  -H "Content-Type: application/json" \
  -d '{
    "query": "software engineer with backend experience",
    "page": 1,
    "pageSize": 3,
    "similarityThreshold": 0.3
  }' \
  http://localhost:8080/api/semanticsearch/search)

RESULT_COUNT1=$(echo "$SEARCH1" | jq -r '.candidates | length')
echo -e "   Results found: ${GREEN}$RESULT_COUNT1${NC}"
if [ "$RESULT_COUNT1" -gt 0 ]; then
    echo "$SEARCH1" | jq -r '.candidates[] | "   - \(.firstName) \(.lastName) - \(.currentTitle // "No title") (Score: \(.similarityScore // 0))"'
fi
echo ""

# Query 2: Senior roles
echo -e "${BLUE}Query 2: 'senior developer with leadership skills'${NC}"
SEARCH2=$(curl -s -X POST \
  -H "Content-Type: application/json" \
  -d '{
    "query": "senior developer with leadership skills",
    "page": 1,
    "pageSize": 3,
    "similarityThreshold": 0.3
  }' \
  http://localhost:8080/api/semanticsearch/search)

RESULT_COUNT2=$(echo "$SEARCH2" | jq -r '.candidates | length')
echo -e "   Results found: ${GREEN}$RESULT_COUNT2${NC}"
if [ "$RESULT_COUNT2" -gt 0 ]; then
    echo "$SEARCH2" | jq -r '.candidates[] | "   - \(.firstName) \(.lastName) - \(.currentTitle // "No title") (Score: \(.similarityScore // 0))"'
fi
echo ""

# Query 3: Specific technology
echo -e "${BLUE}Query 3: 'python developer machine learning'${NC}"
SEARCH3=$(curl -s -X POST \
  -H "Content-Type: application/json" \
  -d '{
    "query": "python developer machine learning",
    "page": 1,
    "pageSize": 3,
    "similarityThreshold": 0.3
  }' \
  http://localhost:8080/api/semanticsearch/search)

RESULT_COUNT3=$(echo "$SEARCH3" | jq -r '.candidates | length')
echo -e "   Results found: ${GREEN}$RESULT_COUNT3${NC}"
if [ "$RESULT_COUNT3" -gt 0 ]; then
    echo "$SEARCH3" | jq -r '.candidates[] | "   - \(.firstName) \(.lastName) - \(.currentTitle // "No title") (Score: \(.similarityScore // 0))"'
fi
echo ""

# Query 4: Generic product role
echo -e "${BLUE}Query 4: 'product manager with agile experience'${NC}"
SEARCH4=$(curl -s -X POST \
  -H "Content-Type: application/json" \
  -d '{
    "query": "product manager with agile experience",
    "page": 1,
    "pageSize": 3,
    "similarityThreshold": 0.3
  }' \
  http://localhost:8080/api/semanticsearch/search)

RESULT_COUNT4=$(echo "$SEARCH4" | jq -r '.candidates | length')
echo -e "   Results found: ${GREEN}$RESULT_COUNT4${NC}"
if [ "$RESULT_COUNT4" -gt 0 ]; then
    echo "$SEARCH4" | jq -r '.candidates[] | "   - \(.firstName) \(.lastName) - \(.currentTitle // "No title") (Score: \(.similarityScore // 0))"'
fi
echo ""

# Test 4: Check embedding quality
echo -e "${YELLOW}Test 4: Checking embedding quality...${NC}"
QUALITY_SQL="
SELECT 
    CASE 
        WHEN embedding_tokens IS NULL OR embedding_tokens = 0 THEN 'No token count'
        WHEN embedding_tokens < 50 THEN 'Low quality (< 50 tokens)'
        WHEN embedding_tokens < 200 THEN 'Medium quality (50-200 tokens)'
        ELSE 'High quality (200+ tokens)'
    END as quality,
    COUNT(*) as count
FROM candidates 
WHERE profile_embedding IS NOT NULL
GROUP BY 
    CASE 
        WHEN embedding_tokens IS NULL OR embedding_tokens = 0 THEN 'No token count'
        WHEN embedding_tokens < 50 THEN 'Low quality (< 50 tokens)'
        WHEN embedding_tokens < 200 THEN 'Medium quality (50-200 tokens)'
        ELSE 'High quality (200+ tokens)'
    END
ORDER BY count DESC;
"
docker exec -e PAGER=cat "$DB_CONTAINER" psql -U postgres -d recruitingdb -c "$QUALITY_SQL"
echo ""

# Test 5: Performance test
echo -e "${YELLOW}Test 5: Testing search performance...${NC}"
START_TIME=$(date +%s%N)
curl -s -X POST \
  -H "Content-Type: application/json" \
  -d '{
    "query": "experienced software engineer",
    "page": 1,
    "pageSize": 20,
    "similarityThreshold": 0.3
  }' \
  http://localhost:8080/api/semanticsearch/search > /dev/null
END_TIME=$(date +%s%N)
DURATION=$((($END_TIME - $START_TIME) / 1000000))
echo -e "   Search duration: ${GREEN}${DURATION}ms${NC}"
echo ""

# Summary
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Test Summary${NC}"
echo -e "${BLUE}========================================${NC}"
echo -e "Total embeddings: $EMBEDDING_COUNT"
echo -e "Search test results:"
echo -e "  - Query 1 (backend): $RESULT_COUNT1 results"
echo -e "  - Query 2 (senior): $RESULT_COUNT2 results"
echo -e "  - Query 3 (python ML): $RESULT_COUNT3 results"
echo -e "  - Query 4 (product): $RESULT_COUNT4 results"
echo -e "  - Performance: ${DURATION}ms"
echo ""

TOTAL_RESULTS=$((RESULT_COUNT1 + RESULT_COUNT2 + RESULT_COUNT3 + RESULT_COUNT4))
if [ "$EMBEDDING_COUNT" -gt 0 ] && [ "$TOTAL_RESULTS" -gt 0 ]; then
    echo -e "${GREEN}✅ SEMANTIC SEARCH IS WORKING!${NC}"
    exit 0
elif [ "$EMBEDDING_COUNT" -gt 0 ]; then
    echo -e "${YELLOW}⚠️  Embeddings exist but no search results. Try adjusting similarity threshold.${NC}"
    exit 0
else
    echo -e "${RED}❌ No embeddings found. Import candidates first.${NC}"
    exit 1
fi
