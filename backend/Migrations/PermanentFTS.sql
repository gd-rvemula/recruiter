-- Full-Text Search Migration - Permanent Infrastructure
-- This migration ensures FTS is always available as part of the standard database setup
-- Run this after the initial database setup to make FTS a permanent feature

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Add search vector columns to all relevant tables
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

CREATE OR REPLACE FUNCTION update_resume_search_vector()
RETURNS TRIGGER AS $$
BEGIN
    NEW.search_vector := 
        setweight(to_tsvector('english', COALESCE(NEW.file_name, '')), 'A') ||
        setweight(to_tsvector('english', COALESCE(NEW.extracted_text, '')), 'C');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION update_skill_search_vector()
RETURNS TRIGGER AS $$
BEGIN
    NEW.search_vector := 
        setweight(to_tsvector('english', COALESCE(NEW.skill_name, '')), 'A') ||
        setweight(to_tsvector('english', COALESCE(NEW.description, '')), 'B') ||
        setweight(to_tsvector('english', COALESCE(NEW.category, '')), 'B');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create triggers for automatic search vector updates
DROP TRIGGER IF EXISTS trig_candidates_search_vector ON candidates;
CREATE TRIGGER trig_candidates_search_vector
    BEFORE INSERT OR UPDATE ON candidates
    FOR EACH ROW EXECUTE FUNCTION update_candidate_search_vector();

DROP TRIGGER IF EXISTS trig_resumes_search_vector ON resumes;
CREATE TRIGGER trig_resumes_search_vector
    BEFORE INSERT OR UPDATE ON resumes
    FOR EACH ROW EXECUTE FUNCTION update_resume_search_vector();

DROP TRIGGER IF EXISTS trig_skills_search_vector ON skills;
CREATE TRIGGER trig_skills_search_vector
    BEFORE INSERT OR UPDATE ON skills
    FOR EACH ROW EXECUTE FUNCTION update_skill_search_vector();

-- Update existing data with search vectors
UPDATE candidates SET search_vector = 
    setweight(to_tsvector('english', COALESCE(first_name, '')), 'A') ||
    setweight(to_tsvector('english', COALESCE(last_name, '')), 'A') ||
    setweight(to_tsvector('english', COALESCE(email, '')), 'B') ||
    setweight(to_tsvector('english', COALESCE(current_title, '')), 'A')
WHERE search_vector IS NULL;

UPDATE resumes SET search_vector = 
    setweight(to_tsvector('english', COALESCE(file_name, '')), 'A') ||
    setweight(to_tsvector('english', COALESCE(extracted_text, '')), 'C')
WHERE search_vector IS NULL;

UPDATE skills SET search_vector = 
    setweight(to_tsvector('english', COALESCE(skill_name, '')), 'A') ||
    setweight(to_tsvector('english', COALESCE(description, '')), 'B') ||
    setweight(to_tsvector('english', COALESCE(category, '')), 'B')
WHERE search_vector IS NULL;

-- Create search indexes for performance
CREATE INDEX IF NOT EXISTS idx_candidates_search_vector 
ON candidates USING GIN(search_vector);

CREATE INDEX IF NOT EXISTS idx_resumes_search_vector 
ON resumes USING GIN(search_vector);

CREATE INDEX IF NOT EXISTS idx_skills_search_vector 
ON skills USING GIN(search_vector);

-- Create materialized view for optimized search combining all candidate data
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
    setweight(to_tsvector('english', COALESCE(STRING_AGG(DISTINCT r.extracted_text, ' '), '')), 'C') as combined_search_vector
FROM candidates c
LEFT JOIN candidate_skills cs ON c.id = cs.candidate_id
LEFT JOIN skills s ON cs.skill_id = s.id
LEFT JOIN resumes r ON c.id = r.candidate_id
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

-- Function to get search suggestions
CREATE OR REPLACE FUNCTION get_search_suggestions(
    search_term text,
    suggestion_type text DEFAULT 'skills',
    max_suggestions integer DEFAULT 10
)
RETURNS TABLE(suggestion text) AS $$
BEGIN
    IF suggestion_type = 'skills' THEN
        RETURN QUERY
        SELECT DISTINCT s.skill_name
        FROM skills s
        WHERE s.skill_name ILIKE '%' || search_term || '%'
        ORDER BY s.skill_name
        LIMIT max_suggestions;
    ELSIF suggestion_type = 'titles' THEN
        RETURN QUERY
        SELECT DISTINCT c.current_title
        FROM candidates c
        WHERE c.current_title IS NOT NULL 
          AND c.current_title ILIKE '%' || search_term || '%'
        ORDER BY c.current_title
        LIMIT max_suggestions;
    ELSIF suggestion_type = 'names' THEN
        RETURN QUERY
        SELECT DISTINCT (c.first_name || ' ' || c.last_name) as full_name
        FROM candidates c
        WHERE (c.first_name ILIKE '%' || search_term || '%' 
               OR c.last_name ILIKE '%' || search_term || '%')
        ORDER BY full_name
        LIMIT max_suggestions;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Show completion status
SELECT 
    'FTS infrastructure permanently installed!' as status,
    COUNT(*) as total_candidates_with_search_vectors
FROM candidates 
WHERE search_vector IS NOT NULL;