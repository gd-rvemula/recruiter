-- Phase 1: Install pgvector Extension for Semantic Search
-- Date: October 5, 2025
-- Description: Enable vector similarity search for AI embeddings
-- Requirement: PostgreSQL 11+ with pgvector extension available

-- Install the pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Verify installation
SELECT extname, extversion 
FROM pg_extension 
WHERE extname = 'vector';

-- Test vector capabilities
SELECT 'pgvector installed successfully! Ready for semantic search.' as status;

COMMENT ON EXTENSION vector IS 'Vector similarity search for semantic embeddings';
