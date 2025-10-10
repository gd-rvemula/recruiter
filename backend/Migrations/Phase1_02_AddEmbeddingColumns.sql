-- Phase 1: Add Embedding Columns for Semantic Search
-- Date: October 5, 2025
-- Description: Add vector columns to store AI embeddings
-- Using 768 dimensions for nomic-embed-text (Ollama)
-- Note: Change to 1536 dimensions if using Azure OpenAI text-embedding-3-small

-- Add embedding column to candidates table (profile summary)
ALTER TABLE candidates 
ADD COLUMN IF NOT EXISTS profile_embedding vector(768),
ADD COLUMN IF NOT EXISTS embedding_generated_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS embedding_model VARCHAR(100),
ADD COLUMN IF NOT EXISTS embedding_tokens INTEGER;

-- Add embedding column to resumes table (full resume text)
ALTER TABLE resumes 
ADD COLUMN IF NOT EXISTS resume_embedding vector(768),
ADD COLUMN IF NOT EXISTS embedding_generated_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS embedding_model VARCHAR(100),
ADD COLUMN IF NOT EXISTS embedding_tokens INTEGER;

-- Create HNSW indexes for fast vector similarity search
-- Using cosine distance (most common for embeddings)
-- m=16: number of connections per layer (higher = more accurate but slower build)
-- ef_construction=64: size of dynamic candidate list (higher = more accurate but slower build)
CREATE INDEX IF NOT EXISTS idx_candidates_profile_embedding 
ON candidates USING hnsw (profile_embedding vector_cosine_ops)
WITH (m = 16, ef_construction = 64);

CREATE INDEX IF NOT EXISTS idx_resumes_resume_embedding 
ON resumes USING hnsw (resume_embedding vector_cosine_ops)
WITH (m = 16, ef_construction = 64);

-- Create covering indexes for embedding queries
CREATE INDEX IF NOT EXISTS idx_candidates_embedding_status 
ON candidates (is_active, embedding_generated_at) 
WHERE profile_embedding IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_resumes_embedding_status 
ON resumes (embedding_generated_at) 
WHERE resume_embedding IS NOT NULL;

-- Add comments for documentation
COMMENT ON COLUMN candidates.profile_embedding IS 'Vector embedding of candidate profile (768 dims from nomic-embed-text)';
COMMENT ON COLUMN candidates.embedding_generated_at IS 'Timestamp when embedding was last generated';
COMMENT ON COLUMN candidates.embedding_model IS 'Model used for embedding generation (e.g., ollama/nomic-embed-text)';
COMMENT ON COLUMN candidates.embedding_tokens IS 'Number of tokens used for embedding generation (for cost tracking)';

COMMENT ON COLUMN resumes.resume_embedding IS 'Vector embedding of full resume text (768 dims from nomic-embed-text)';
COMMENT ON COLUMN resumes.embedding_generated_at IS 'Timestamp when embedding was last generated';
COMMENT ON COLUMN resumes.embedding_model IS 'Model used for embedding generation';
COMMENT ON COLUMN resumes.embedding_tokens IS 'Number of tokens used for embedding generation';

-- Show updated schema
SELECT 'Embedding columns and indexes created successfully!' as status;

-- Verify columns
SELECT 
    column_name, 
    data_type,
    CASE 
        WHEN data_type = 'USER-DEFINED' THEN udt_name
        ELSE data_type
    END as type_detail
FROM information_schema.columns
WHERE table_name = 'candidates' 
    AND column_name LIKE '%embedding%'
ORDER BY ordinal_position;

-- Verify indexes
SELECT 
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename IN ('candidates', 'resumes')
    AND indexname LIKE '%embedding%';
