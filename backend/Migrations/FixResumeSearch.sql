-- Fix Resume Search Vectors - Critical Update
-- This script fixes the resume search vector population issue

-- First, let's create the correct resume search vector update function
CREATE OR REPLACE FUNCTION update_resume_search_vector()
RETURNS TRIGGER AS $$
BEGIN
    NEW.search_vector := 
        setweight(to_tsvector('english', COALESCE(NEW.file_name, '')), 'A') ||
        setweight(to_tsvector('english', COALESCE(NEW.resume_text, '')), 'C');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger for automatic resume search vector updates
DROP TRIGGER IF EXISTS trig_resumes_search_vector ON resumes;
CREATE TRIGGER trig_resumes_search_vector
    BEFORE INSERT OR UPDATE ON resumes
    FOR EACH ROW EXECUTE FUNCTION update_resume_search_vector();

-- Update all existing resumes with search vectors
UPDATE resumes SET search_vector = 
    setweight(to_tsvector('english', COALESCE(file_name, '')), 'A') ||
    setweight(to_tsvector('english', COALESCE(resume_text, '')), 'C')
WHERE search_vector IS NULL;

-- Create/recreate the resume search index
DROP INDEX IF EXISTS idx_resumes_search_vector;
CREATE INDEX idx_resumes_search_vector 
ON resumes USING GIN(search_vector);

-- Now let's recreate the materialized view with the correct field names
DROP MATERIALIZED VIEW IF EXISTS candidate_search_view CASCADE;
CREATE MATERIALIZED VIEW candidate_search_view AS
SELECT 
    c.id as candidate_id,
    c.first_name,
    c.last_name,
    c.email,
    c.current_title,
    c.total_years_experience as years_of_experience,
    STRING_AGG(DISTINCT s.skill_name, ', ' ORDER BY s.skill_name) as skills_text,
    c.search_vector ||
    setweight(to_tsvector('english', COALESCE(STRING_AGG(DISTINCT s.skill_name, ' '), '')), 'B') ||
    setweight(to_tsvector('english', COALESCE(STRING_AGG(DISTINCT r.resume_text, ' '), '')), 'C') as combined_search_vector
FROM candidates c
LEFT JOIN candidate_skills cs ON c.id = cs.candidate_id
LEFT JOIN skills s ON cs.skill_id = s.id
LEFT JOIN resumes r ON c.id = r.candidate_id
WHERE c.is_active = true
GROUP BY c.id, c.first_name, c.last_name, c.email, c.current_title, 
         c.total_years_experience, c.search_vector;

-- Recreate the index on materialized view
CREATE INDEX idx_candidate_search_view_vector 
ON candidate_search_view USING GIN(combined_search_vector);

-- Refresh the materialized view
REFRESH MATERIALIZED VIEW candidate_search_view;

-- Show the results
SELECT 
    'Resume search vectors fixed!' as status,
    COUNT(*) as total_resumes,
    COUNT(search_vector) as resumes_with_search_vectors
FROM resumes;

-- Test search for Azure in resume content
SELECT COUNT(*) as azure_candidates_found
FROM candidate_search_view 
WHERE combined_search_vector @@ plainto_tsquery('english', 'Azure');