-- Update Search Function to Include Missing Columns
-- This script updates the search function to match the C# DTO expectations

-- Drop and recreate the search function with all required columns
DROP FUNCTION IF EXISTS search_candidates_fts(text, integer, integer);

CREATE OR REPLACE FUNCTION search_candidates_fts(
    search_query text,
    limit_count int DEFAULT 50,
    offset_count int DEFAULT 0
)
RETURNS TABLE (
    candidate_id uuid,
    candidate_code varchar,
    first_name varchar,
    last_name varchar,
    full_name varchar,
    email varchar,
    phone varchar,
    city varchar,
    state varchar,
    country varchar,
    current_title varchar,
    years_of_experience int,
    salary_expectation numeric,
    is_authorized_to_work boolean,
    needs_sponsorship boolean,
    is_active boolean,
    skills_text text,
    search_rank real,
    highlight_snippet text
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        c.id as candidate_id,
        c.candidate_code,
        c.first_name,
        c.last_name,
        c.full_name,
        c.email,
        c.phone,
        c.city,
        c.state,
        c.country,
        c.current_title,
        c.total_years_experience as years_of_experience,
        c.salary_expectation,
        c.is_authorized_to_work,
        c.needs_sponsorship,
        c.is_active,
        csv.skills_text,
        ts_rank(csv.combined_search_vector, plainto_tsquery('english', search_query))::real as search_rank,
        ts_headline('english', 
            COALESCE(c.first_name, '') || ' ' || COALESCE(c.last_name, '') || ' ' || 
            COALESCE(c.current_title, '') || ' ' || COALESCE(csv.skills_text, ''),
            plainto_tsquery('english', search_query),
            'MaxWords=10, MinWords=5'
        ) as highlight_snippet
    FROM candidate_search_view csv
    JOIN candidates c ON csv.candidate_id = c.id
    WHERE csv.combined_search_vector @@ plainto_tsquery('english', search_query)
      AND c.is_active = true
    ORDER BY search_rank DESC, c.last_name, c.first_name
    LIMIT limit_count OFFSET offset_count;
END;
$$ LANGUAGE plpgsql;

-- Test the updated function
SELECT 'Updated search function with all required columns!' as status;
SELECT candidate_id, candidate_code, full_name, current_title, search_rank
FROM search_candidates_fts('software', 2, 0);