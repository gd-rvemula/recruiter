-- Clear Database Script for Recruiter System
-- This script will clear all data and FTS structures to prepare for fresh XLSX import

-- Disable triggers to avoid constraint issues during cleanup
SET session_replication_role = replica;

-- Clear all data from tables (in dependency order)
DELETE FROM job_applications;
DELETE FROM candidate_notes;
DELETE FROM candidate_skills;
DELETE FROM work_experience;
DELETE FROM education;
DELETE FROM resumes;
DELETE FROM file_uploads;
DELETE FROM candidates;
DELETE FROM skills;
DELETE FROM application_sources;
DELETE FROM application_stages;

-- Re-enable triggers
SET session_replication_role = DEFAULT;

-- Drop Full-Text Search structures
DROP MATERIALIZED VIEW IF EXISTS candidate_search_view CASCADE;

-- Drop FTS functions
DROP FUNCTION IF EXISTS search_candidates_fts(text, integer, integer) CASCADE;
DROP FUNCTION IF EXISTS get_search_suggestions(text, text, integer) CASCADE;
DROP FUNCTION IF EXISTS refresh_candidate_search_view() CASCADE;

-- Drop trigger functions
DROP FUNCTION IF EXISTS trigger_update_candidate_search_vector() CASCADE;
DROP FUNCTION IF EXISTS trigger_update_resume_search_vector() CASCADE;
DROP FUNCTION IF EXISTS trigger_update_skill_search_vector() CASCADE;

-- Drop update functions
DROP FUNCTION IF EXISTS update_candidate_search_vector() CASCADE;
DROP FUNCTION IF EXISTS update_resume_search_vector() CASCADE;
DROP FUNCTION IF EXISTS update_skill_search_vector() CASCADE;

-- Drop FTS columns from tables
ALTER TABLE candidates DROP COLUMN IF EXISTS search_vector CASCADE;
ALTER TABLE resumes DROP COLUMN IF EXISTS search_vector CASCADE;
ALTER TABLE skills DROP COLUMN IF EXISTS search_vector CASCADE;

-- Drop FTS indexes
DROP INDEX IF EXISTS idx_candidates_search_vector;
DROP INDEX IF EXISTS idx_resumes_search_vector;
DROP INDEX IF EXISTS idx_skills_search_vector;
DROP INDEX IF EXISTS idx_candidate_search_view_vector;

-- Reset sequences to start from 1
SELECT setval(pg_get_serial_sequence('candidates', 'id'), 1, false);
SELECT setval(pg_get_serial_sequence('skills', 'id'), 1, false);
SELECT setval(pg_get_serial_sequence('work_experience', 'id'), 1, false);
SELECT setval(pg_get_serial_sequence('education', 'id'), 1, false);
SELECT setval(pg_get_serial_sequence('resumes', 'id'), 1, false);
SELECT setval(pg_get_serial_sequence('job_applications', 'id'), 1, false);
SELECT setval(pg_get_serial_sequence('candidate_notes', 'id'), 1, false);
SELECT setval(pg_get_serial_sequence('file_uploads', 'id'), 1, false);
SELECT setval(pg_get_serial_sequence('application_sources', 'id'), 1, false);
SELECT setval(pg_get_serial_sequence('application_stages', 'id'), 1, false);

-- Vacuum to reclaim space
VACUUM FULL;

-- Show final table counts
SELECT 
    'candidates' as table_name, COUNT(*) as row_count FROM candidates
UNION ALL SELECT 
    'skills', COUNT(*) FROM skills
UNION ALL SELECT 
    'work_experience', COUNT(*) FROM work_experience
UNION ALL SELECT 
    'education', COUNT(*) FROM education
UNION ALL SELECT 
    'resumes', COUNT(*) FROM resumes
UNION ALL SELECT 
    'candidate_skills', COUNT(*) FROM candidate_skills
UNION ALL SELECT 
    'job_applications', COUNT(*) FROM job_applications
UNION ALL SELECT 
    'candidate_notes', COUNT(*) FROM candidate_notes
UNION ALL SELECT 
    'file_uploads', COUNT(*) FROM file_uploads
UNION ALL SELECT 
    'application_sources', COUNT(*) FROM application_sources
UNION ALL SELECT 
    'application_stages', COUNT(*) FROM application_stages;

-- Database cleared successfully! All tables are empty and ready for new XLSX import.
SELECT 'Database cleared successfully! All tables are empty and ready for new XLSX import.' as status;