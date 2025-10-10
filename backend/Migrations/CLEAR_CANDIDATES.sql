-- =============================================================================
-- CLEAR CANDIDATE DATABASE
-- Date: October 5, 2025
-- Purpose: Clean all candidate data for fresh import with PII protection
-- =============================================================================

-- WARNING: This script will DELETE ALL candidate data!
-- Make sure you have backups if needed.

BEGIN;

-- Display current counts before deletion
SELECT 'BEFORE DELETION - Current Counts:' as status;

SELECT 
    'candidates' as table_name, 
    COUNT(*) as record_count 
FROM candidates
UNION ALL
SELECT 
    'resumes' as table_name, 
    COUNT(*) as record_count 
FROM resumes
UNION ALL
SELECT 
    'candidate_skills' as table_name, 
    COUNT(*) as record_count 
FROM candidate_skills
UNION ALL
SELECT 
    'work_experience' as table_name, 
    COUNT(*) as record_count 
FROM work_experience
UNION ALL
SELECT 
    'education' as table_name, 
    COUNT(*) as record_count 
FROM education
UNION ALL
SELECT 
    'job_applications' as table_name, 
    COUNT(*) as record_count 
FROM job_applications
UNION ALL
SELECT 
    'candidate_notes' as table_name, 
    COUNT(*) as record_count 
FROM candidate_notes
UNION ALL
SELECT 
    'file_uploads' as table_name, 
    COUNT(*) as record_count 
FROM file_uploads
ORDER BY table_name;

-- =============================================================================
-- DELETE ALL CANDIDATE-RELATED DATA
-- (CASCADE relationships will handle most deletions automatically)
-- =============================================================================

-- Delete candidate notes (if any)
DELETE FROM candidate_notes;

-- Delete file uploads (if any)
DELETE FROM file_uploads;

-- Delete job applications
DELETE FROM job_applications;

-- Delete education records
DELETE FROM education;

-- Delete work experience records
DELETE FROM work_experience;

-- Delete candidate skills (many-to-many)
DELETE FROM candidate_skills;

-- Delete resumes (contains PII that should be sanitized)
DELETE FROM resumes;

-- Delete candidate status history (if table exists)
-- DELETE FROM candidate_status_history;

-- Delete all candidates (this will CASCADE to related tables)
DELETE FROM candidates;

-- =============================================================================
-- VERIFY DELETION
-- =============================================================================

SELECT 'AFTER DELETION - Remaining Counts:' as status;

SELECT 
    'candidates' as table_name, 
    COUNT(*) as record_count 
FROM candidates
UNION ALL
SELECT 
    'resumes' as table_name, 
    COUNT(*) as record_count 
FROM resumes
UNION ALL
SELECT 
    'candidate_skills' as table_name, 
    COUNT(*) as record_count 
FROM candidate_skills
UNION ALL
SELECT 
    'work_experience' as table_name, 
    COUNT(*) as record_count 
FROM work_experience
UNION ALL
SELECT 
    'education' as table_name, 
    COUNT(*) as record_count 
FROM education
UNION ALL
SELECT 
    'job_applications' as table_name, 
    COUNT(*) as record_count 
FROM job_applications
UNION ALL
SELECT 
    'candidate_notes' as table_name, 
    COUNT(*) as record_count 
FROM candidate_notes
UNION ALL
SELECT 
    'file_uploads' as table_name, 
    COUNT(*) as record_count 
FROM file_uploads
ORDER BY table_name;

-- =============================================================================
-- RESET SEQUENCES (Optional - for clean candidate codes)
-- =============================================================================

-- Note: Candidate codes use GUID + timestamp, not sequences
-- No sequence reset needed

-- =============================================================================
-- REFRESH MATERIALIZED VIEWS
-- =============================================================================

-- Refresh the candidate search view (if exists)
-- Note: Not using CONCURRENTLY since we just cleared all data
REFRESH MATERIALIZED VIEW candidate_search_view;

SELECT 'Database cleared successfully!' as status;
SELECT 'Ready for fresh Excel imports with PII protection.' as next_step;

COMMIT;

-- =============================================================================
-- VERIFICATION QUERIES
-- =============================================================================

-- Check that embeddings are also cleared
SELECT 
    COUNT(*) as candidates_with_embeddings,
    COUNT(*) FILTER (WHERE profile_embedding IS NOT NULL) as with_embeddings,
    COUNT(*) FILTER (WHERE profile_embedding IS NULL) as without_embeddings
FROM candidates;

-- Expected: All counts should be 0

-- =============================================================================
-- END OF SCRIPT
-- =============================================================================
