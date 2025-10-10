-- Migration: Add requisition_name column and clear all data
-- Date: 2025-10-06
-- Purpose: Track which job posting candidates applied to

-- Connect to the database
\c recruitingdb;

-- Step 1: Clear all data from related tables (in correct order due to foreign keys)
TRUNCATE TABLE ai_summaries CASCADE;
TRUNCATE TABLE candidate_status_history CASCADE;
TRUNCATE TABLE candidate_skills CASCADE;
TRUNCATE TABLE job_applications CASCADE;
TRUNCATE TABLE resumes CASCADE;
TRUNCATE TABLE work_experience CASCADE;
TRUNCATE TABLE education CASCADE;
TRUNCATE TABLE candidates CASCADE;
TRUNCATE TABLE skills CASCADE;

-- Step 2: Reset sequences if any
-- (PostgreSQL will auto-reset UUID sequences, but good to be explicit if we had serial IDs)

-- Step 3: Add requisition_name column to candidates table
ALTER TABLE candidates 
ADD COLUMN IF NOT EXISTS requisition_name VARCHAR(300);

-- Step 4: Create index for faster searches by requisition
CREATE INDEX IF NOT EXISTS idx_candidates_requisition_name 
ON candidates(requisition_name) 
WHERE requisition_name IS NOT NULL;

-- Step 5: Add comment for documentation
COMMENT ON COLUMN candidates.requisition_name IS 'The job posting/requisition name this candidate applied to';

-- Step 6: Verify column was added
\d candidates;

-- Step 7: Display summary
SELECT 
    'Migration completed successfully' AS status,
    COUNT(*) AS remaining_candidates,
    (SELECT COUNT(*) FROM skills) AS remaining_skills,
    (SELECT COUNT(*) FROM resumes) AS remaining_resumes,
    (SELECT COUNT(*) FROM ai_summaries) AS remaining_ai_summaries
FROM candidates;

-- Step 8: Show the new column
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_name = 'candidates' 
AND column_name = 'requisition_name';
