-- Add Embedding Columns for Semantic Search
-- Date: October 4, 2025
-- Description: Add vector columns to store AI embeddings for candidates and resumes

-- Add embedding column to candidates table
-- Using 1536 dimensions for OpenAI text-embedding-3-small
-- Or 768 for Ollama nomic-embed-text
ALTER TABLE candidates 
ADD COLUMN IF NOT EXISTS profile_embedding vector(1536);

-- Add embedding column to resumes table
ALTER TABLE resumes 
ADD COLUMN IF NOT EXISTS resume_embedding vector(1536);

-- Add embedding column to skills table (for skill-based search)
ALTER TABLE skills 
ADD COLUMN IF NOT EXISTS skill_embedding vector(1536);

-- Create indexes for fast vector similarity search
-- Using HNSW (Hierarchical Navigable Small World) for best performance
CREATE INDEX IF NOT EXISTS idx_candidates_profile_embedding 
ON candidates USING hnsw (profile_embedding vector_cosine_ops);

CREATE INDEX IF NOT EXISTS idx_resumes_resume_embedding 
ON resumes USING hnsw (resume_embedding vector_cosine_ops);

CREATE INDEX IF NOT EXISTS idx_skills_skill_embedding 
ON skills USING hnsw (skill_embedding vector_cosine_ops);

-- Alternative: IVFFlat index (faster build, slower query)
-- CREATE INDEX idx_candidates_profile_embedding ON candidates 
-- USING ivfflat (profile_embedding vector_cosine_ops) WITH (lists = 100);

-- Add metadata columns for tracking embeddings
ALTER TABLE candidates 
ADD COLUMN IF NOT EXISTS embedding_generated_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS embedding_model VARCHAR(100);

ALTER TABLE resumes 
ADD COLUMN IF NOT EXISTS embedding_generated_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS embedding_model VARCHAR(100);

-- Show the new schema
SELECT 'Embedding columns added successfully!' as status;
\d candidates;
\d resumes;