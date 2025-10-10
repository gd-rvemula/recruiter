-- Migration: Create AI Summaries Table
-- Purpose: Cache AI-generated resume summaries to avoid repeated Azure OpenAI API calls
-- Date: 2025-10-06

-- Create ai_summaries table
CREATE TABLE IF NOT EXISTS ai_summaries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    candidate_id UUID NOT NULL REFERENCES candidates(id) ON DELETE CASCADE,
    resume_id UUID NOT NULL REFERENCES resumes(id) ON DELETE CASCADE,
    summary_text TEXT NOT NULL,
    resume_text_hash VARCHAR(64) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for efficient lookup
CREATE INDEX IF NOT EXISTS idx_ai_summaries_candidate_id ON ai_summaries(candidate_id);
CREATE INDEX IF NOT EXISTS idx_ai_summaries_resume_id ON ai_summaries(resume_id);
CREATE INDEX IF NOT EXISTS idx_ai_summaries_resume_text_hash ON ai_summaries(resume_text_hash);

-- Create composite index for fast cache lookup
CREATE INDEX IF NOT EXISTS idx_ai_summaries_resume_hash_lookup 
    ON ai_summaries(resume_id, resume_text_hash);

-- Add comment for documentation
COMMENT ON TABLE ai_summaries IS 'Caches AI-generated resume summaries to reduce Azure OpenAI API calls';
COMMENT ON COLUMN ai_summaries.resume_text_hash IS 'SHA256 hash of resume text for cache invalidation when resume changes';

-- Verify table creation
SELECT 
    'Table created successfully' AS status,
    COUNT(*) AS initial_count 
FROM ai_summaries;
