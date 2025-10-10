#!/bin/bash
# Phase 1: Semantic Search Setup Script
# Date: October 5, 2025
# This script initializes Ollama and database for semantic search

set -e

echo "========================================="
echo "Phase 1: Semantic Search Setup"
echo "========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Step 1: Build and start services
echo -e "${YELLOW}Step 1: Building and starting Docker services...${NC}"
cd /Users/rvemula/projects/Recruiter/backend
docker compose down
docker compose up -d --build

echo -e "${GREEN}✓ Services started${NC}"
echo ""

# Step 2: Wait for services to be healthy
echo -e "${YELLOW}Step 2: Waiting for services to be healthy...${NC}"
echo "Waiting for Ollama..."
for i in {1..30}; do
    if docker compose exec -T ollama curl -f http://localhost:11434/api/tags >/dev/null 2>&1; then
        echo -e "${GREEN}✓ Ollama is ready${NC}"
        break
    fi
    echo -n "."
    sleep 2
done
echo ""

# Step 3: Pull Ollama embedding model
echo -e "${YELLOW}Step 3: Pulling Ollama embedding model (nomic-embed-text)...${NC}"
docker compose exec -T ollama ollama pull nomic-embed-text
echo -e "${GREEN}✓ Embedding model pulled${NC}"
echo ""

# Step 4: Install pgvector extension
echo -e "${YELLOW}Step 4: Installing pgvector extension...${NC}"

# Find the database container
DB_CONTAINER=$(docker ps --filter "ancestor=postgres:15" --format "{{.ID}}" | head -1)

if [ -z "$DB_CONTAINER" ]; then
    echo -e "${RED}✗ PostgreSQL container not found${NC}"
    exit 1
fi

# Copy migration file
docker cp Migrations/Phase1_01_InstallPgVector.sql $DB_CONTAINER:/tmp/

# Execute migration
docker exec -it $DB_CONTAINER bash -c "PAGER=cat psql -U postgres -d recruitingdb -f /tmp/Phase1_01_InstallPgVector.sql"

echo -e "${GREEN}✓ pgvector installed${NC}"
echo ""

# Step 5: Add embedding columns
echo -e "${YELLOW}Step 5: Adding embedding columns and indexes...${NC}"

# Copy migration file
docker cp Migrations/Phase1_02_AddEmbeddingColumns.sql $DB_CONTAINER:/tmp/

# Execute migration
docker exec -it $DB_CONTAINER bash -c "PAGER=cat psql -U postgres -d recruitingdb -f /tmp/Phase1_02_AddEmbeddingColumns.sql"

echo -e "${GREEN}✓ Embedding columns added${NC}"
echo ""

# Step 6: Verify setup
echo -e "${YELLOW}Step 6: Verifying setup...${NC}"

# Check API health
echo "Checking API..."
sleep 5
API_HEALTH=$(curl -s http://localhost:8080/health || echo "FAILED")
if [[ $API_HEALTH == *"Healthy"* ]]; then
    echo -e "${GREEN}✓ API is healthy${NC}"
else
    echo -e "${RED}✗ API health check failed${NC}"
fi

# Check embedding service
echo "Checking embedding service..."
EMBEDDING_HEALTH=$(curl -s http://localhost:8080/api/semanticsearch/health || echo "FAILED")
if [[ $EMBEDDING_HEALTH == *"available"* ]]; then
    echo -e "${GREEN}✓ Embedding service is healthy${NC}"
else
    echo -e "${YELLOW}⚠ Embedding service may not be ready yet (check logs)${NC}"
fi

echo ""
echo "========================================="
echo -e "${GREEN}Phase 1 Setup Complete!${NC}"
echo "========================================="
echo ""
echo "Services:"
echo "  - API: http://localhost:8080"
echo "  - Swagger: http://localhost:8080/swagger"
echo "  - Ollama: http://localhost:11434"
echo ""
echo "Test semantic search:"
echo "  curl -X POST http://localhost:8080/api/semanticsearch/search \\"
echo "    -H 'Content-Type: application/json' \\"
echo "    -d '{\"query\": \"experienced React developer\", \"page\": 1, \"pageSize\": 10}'"
echo ""
echo "Check logs:"
echo "  docker compose logs -f recruiter-api"
echo "  docker compose logs -f ollama"
echo ""
