-- Fix Script for Full Text Search Migration
-- This script fixes any remaining issues from the main migration

-- Drop and recreate the search function with correct signature to avoid conflicts
DROP FUNCTION IF EXISTS search_candidates_fts(text, integer, integer);

-- Recreate the search function
CREATE OR REPLACE FUNCTION search_candidates_fts(
    search_query text,
    limit_count int DEFAULT 50,
    offset_count int DEFAULT 0
)
RETURNS TABLE (
    candidate_id uuid,
    first_name varchar,
    last_name varchar,
    email varchar,
    phone varchar,
    city varchar,
    state varchar,
    country varchar,
    current_title varchar,
    years_of_experience int,
    skills_text text,
    search_rank real,
    highlight_snippet text
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        csv.candidate_id,
        csv.first_name,
        csv.last_name,
        csv.email,
        csv.phone,
        csv.city,
        csv.state,
        csv.country,
        csv.current_title,
        csv.years_of_experience,
        csv.skills_text,
        ts_rank(csv.combined_search_vector, plainto_tsquery('english', search_query))::real as search_rank,
        ts_headline('english', 
            COALESCE(csv.first_name, '') || ' ' || COALESCE(csv.last_name, '') || ' ' || 
            COALESCE(csv.current_title, '') || ' ' || COALESCE(csv.skills_text, ''),
            plainto_tsquery('english', search_query),
            'MaxWords=10, MinWords=5'
        ) as highlight_snippet
    FROM candidate_search_view csv
    WHERE csv.combined_search_vector @@ plainto_tsquery('english', search_query)
    ORDER BY search_rank DESC, csv.last_name, csv.first_name
    LIMIT limit_count OFFSET offset_count;
END;
$$ LANGUAGE plpgsql;

-- Test the search function
SELECT 'Full-text search fixes applied successfully!' as status;
SELECT 'Testing search function...' as test;

-- Test with a simple search to verify everything works
SELECT candidate_id, first_name, last_name, current_title, search_rank
FROM search_candidates_fts('developer', 5, 0);