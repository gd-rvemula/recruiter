-- Recreate Full-Text Search Infrastructure After XLSX Import
-- Run this script AFTER importing new XLSX data to restore search functionality

-- Add search vector columns back to tables
ALTER TABLE candidates ADD COLUMN IF NOT EXISTS search_vector tsvector;
ALTER TABLE resumes ADD COLUMN IF NOT EXISTS search_vector tsvector;
ALTER TABLE skills ADD COLUMN IF NOT EXISTS search_vector tsvector;

-- Create update functions for search vectors
CREATE OR REPLACE FUNCTION update_candidate_search_vector()
RETURNS TRIGGER AS $$
BEGIN
    NEW.search_vector := 
        setweight(to_tsvector('english', COALESCE(NEW.first_name, '')), 'A') ||
        setweight(to_tsvector('english', COALESCE(NEW.last_name, '')), 'A') ||
        setweight(to_tsvector('english', COALESCE(NEW.email, '')), 'B') ||
        setweight(to_tsvector('english', COALESCE(NEW.current_title, '')), 'A');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create triggers for automatic search vector updates
DROP TRIGGER IF EXISTS trig_candidates_search_vector ON candidates;
CREATE TRIGGER trig_candidates_search_vector
    BEFORE INSERT OR UPDATE ON candidates
    FOR EACH ROW EXECUTE FUNCTION update_candidate_search_vector();

-- Update existing candidate search vectors
UPDATE candidates SET search_vector = 
    setweight(to_tsvector('english', COALESCE(first_name, '')), 'A') ||
    setweight(to_tsvector('english', COALESCE(last_name, '')), 'A') ||
    setweight(to_tsvector('english', COALESCE(email, '')), 'B') ||
    setweight(to_tsvector('english', COALESCE(current_title, '')), 'A')
WHERE search_vector IS NULL;

-- Create search indexes
CREATE INDEX IF NOT EXISTS idx_candidates_search_vector 
ON candidates USING GIN(search_vector);

-- Create materialized view for optimized search
CREATE MATERIALIZED VIEW candidate_search_view AS
SELECT 
    c.id as candidate_id,
    c.first_name,
    c.last_name,
    c.email,
    c.current_title,
    c.total_years_experience as years_of_experience,
    STRING_AGG(s.skill_name, ', ' ORDER BY s.skill_name) as skills_text,
    c.search_vector ||
    setweight(to_tsvector('english', COALESCE(STRING_AGG(s.skill_name, ' '), '')), 'B') as combined_search_vector
FROM candidates c
LEFT JOIN candidate_skills cs ON c.id = cs.candidate_id
LEFT JOIN skills s ON cs.skill_id = s.id
WHERE c.is_active = true
GROUP BY c.id, c.first_name, c.last_name, c.email, c.current_title, 
         c.total_years_experience, c.search_vector;

-- Create index on materialized view
CREATE INDEX IF NOT EXISTS idx_candidate_search_view_vector 
ON candidate_search_view USING GIN(combined_search_vector);

-- Create function to refresh the materialized view
CREATE OR REPLACE FUNCTION refresh_candidate_search_view()
RETURNS void AS $$
BEGIN
    REFRESH MATERIALIZED VIEW CONCURRENTLY candidate_search_view;
END;
$$ LANGUAGE plpgsql;

-- Create the main search function
CREATE OR REPLACE FUNCTION search_candidates_fts(
    search_query text,
    page_size integer DEFAULT 20,
    page_offset integer DEFAULT 0
)
RETURNS TABLE(
    candidate_id uuid,
    candidate_code varchar,
    first_name varchar,
    last_name varchar,
    email varchar,
    current_title varchar,
    years_of_experience integer,
    skills_text text,
    search_rank real
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        csv.candidate_id,
        c.candidate_code,
        csv.first_name,
        csv.last_name,
        csv.email,
        csv.current_title,
        csv.years_of_experience,
        csv.skills_text,
        ts_rank(csv.combined_search_vector, plainto_tsquery('english', search_query)) as search_rank
    FROM candidate_search_view csv
    JOIN candidates c ON csv.candidate_id = c.id
    WHERE csv.combined_search_vector @@ plainto_tsquery('english', search_query)
      AND c.is_active = true
    ORDER BY search_rank DESC, csv.last_name, csv.first_name
    LIMIT page_size OFFSET page_offset;
END;
$$ LANGUAGE plpgsql;

-- Show final status
SELECT 
    'FTS infrastructure restored!' as status,
    COUNT(*) as total_candidates
FROM candidates;