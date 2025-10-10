-- Populate Search Vectors Script
-- This script populates the search vectors for all existing data

-- Update all candidate search vectors
UPDATE candidates 
SET search_vector = to_tsvector('english', 
    COALESCE(first_name, '') || ' ' ||
    COALESCE(last_name, '') || ' ' ||
    COALESCE(email, '') || ' ' ||
    COALESCE(phone, '') || ' ' ||
    COALESCE(city, '') || ' ' ||
    COALESCE(state, '') || ' ' ||
    COALESCE(country, '') || ' ' ||
    COALESCE(current_title, '') || ' ' ||
    COALESCE(
        (SELECT string_agg(skill_name, ' ') 
         FROM candidate_skills cs 
         JOIN skills s ON cs.skill_id = s.id 
         WHERE cs.candidate_id = candidates.id), 
        ''
    )
);

-- Update all resume search vectors
UPDATE resumes 
SET resume_search_vector = to_tsvector('english', 
    COALESCE(file_name, '') || ' ' ||
    COALESCE(resume_text, '')
)
WHERE resume_text IS NOT NULL;

-- Update all skill search vectors
UPDATE skills 
SET skill_search_vector = to_tsvector('english', 
    COALESCE(skill_name, '') || ' ' ||
    COALESCE(category, '')
);

-- Refresh the materialized view
REFRESH MATERIALIZED VIEW candidate_search_view;

-- Fix the search suggestions function return type
DROP FUNCTION IF EXISTS get_search_suggestions(text, integer);
CREATE OR REPLACE FUNCTION get_search_suggestions(
    search_term text,
    suggestion_limit int DEFAULT 10
)
RETURNS TABLE (
    suggestion text,
    similarity_score real
) AS $$
BEGIN
    RETURN QUERY
    SELECT DISTINCT
        s.skill_name::text as suggestion,
        similarity(s.skill_name, search_term) as similarity_score
    FROM skills s
    WHERE s.skill_name % search_term
    ORDER BY similarity_score DESC
    LIMIT suggestion_limit;
END;
$$ LANGUAGE plpgsql;

-- Check results
SELECT 'Population completed!' as status;
SELECT 'Updated candidates:', COUNT(*) as count FROM candidates WHERE search_vector IS NOT NULL;
SELECT 'Updated resumes:', COUNT(*) as count FROM resumes WHERE resume_search_vector IS NOT NULL;
SELECT 'Updated skills:', COUNT(*) as count FROM skills WHERE skill_search_vector IS NOT NULL;