-- Full Text Search Migration for Recruiter Database
-- This script adds PostgreSQL full-text search capabilities

-- Enable the pg_trgm extension for fuzzy matching and suggestions
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Add search vector columns to existing tables
ALTER TABLE candidates 
ADD COLUMN IF NOT EXISTS search_vector tsvector;

ALTER TABLE resumes 
ADD COLUMN IF NOT EXISTS resume_search_vector tsvector;

ALTER TABLE skills 
ADD COLUMN IF NOT EXISTS skill_search_vector tsvector;

-- Create GIN indexes for fast full-text search
CREATE INDEX IF NOT EXISTS idx_candidates_search_vector 
ON candidates USING gin(search_vector);

CREATE INDEX IF NOT EXISTS idx_resumes_search_vector 
ON resumes USING gin(resume_search_vector);

CREATE INDEX IF NOT EXISTS idx_skills_search_vector 
ON skills USING gin(skill_search_vector);

-- Create trigram indexes for fuzzy matching and suggestions
CREATE INDEX IF NOT EXISTS idx_candidates_names_trigram 
ON candidates USING gin((first_name || ' ' || last_name) gin_trgm_ops);

CREATE INDEX IF NOT EXISTS idx_skills_name_trigram 
ON skills USING gin(skill_name gin_trgm_ops);

-- Function to update candidate search vector with correct column names
CREATE OR REPLACE FUNCTION update_candidate_search_vector(candidate_id UUID)
RETURNS void AS $$
BEGIN
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
    )
    WHERE candidates.id = candidate_id;
END;
$$ LANGUAGE plpgsql;

-- Function to update resume search vector
CREATE OR REPLACE FUNCTION update_resume_search_vector(resume_id UUID)
RETURNS void AS $$
BEGIN
    UPDATE resumes 
    SET resume_search_vector = to_tsvector('english', 
        COALESCE(file_name, '') || ' ' ||
        COALESCE(resume_text, '')
    )
    WHERE resumes.id = resume_id;
END;
$$ LANGUAGE plpgsql;

-- Function to update skill search vector
CREATE OR REPLACE FUNCTION update_skill_search_vector(skill_id UUID)
RETURNS void AS $$
BEGIN
    UPDATE skills 
    SET skill_search_vector = to_tsvector('english', 
        COALESCE(skill_name, '') || ' ' ||
        COALESCE(category, '')
    )
    WHERE skills.id = skill_id;
END;
$$ LANGUAGE plpgsql;

-- Populate initial search vectors for all existing data
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

UPDATE resumes 
SET resume_search_vector = to_tsvector('english', 
    COALESCE(file_name, '') || ' ' ||
    COALESCE(resume_text, '')
)
WHERE resume_text IS NOT NULL;

UPDATE skills 
SET skill_search_vector = to_tsvector('english', 
    COALESCE(skill_name, '') || ' ' ||
    COALESCE(category, '')
);

-- Create triggers to automatically update search vectors when data changes
CREATE OR REPLACE FUNCTION trigger_update_candidate_search_vector()
RETURNS trigger AS $$
BEGIN
    NEW.search_vector := to_tsvector('english', 
        COALESCE(NEW.first_name, '') || ' ' ||
        COALESCE(NEW.last_name, '') || ' ' ||
        COALESCE(NEW.email, '') || ' ' ||
        COALESCE(NEW.phone, '') || ' ' ||
        COALESCE(NEW.city, '') || ' ' ||
        COALESCE(NEW.state, '') || ' ' ||
        COALESCE(NEW.country, '') || ' ' ||
        COALESCE(NEW.current_title, '')
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION trigger_update_resume_search_vector()
RETURNS trigger AS $$
BEGIN
    NEW.resume_search_vector := to_tsvector('english', 
        COALESCE(NEW.file_name, '') || ' ' ||
        COALESCE(NEW.resume_text, '')
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION trigger_update_skill_search_vector()
RETURNS trigger AS $$
BEGIN
    NEW.skill_search_vector := to_tsvector('english', 
        COALESCE(NEW.skill_name, '') || ' ' ||
        COALESCE(NEW.category, '')
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the triggers
DROP TRIGGER IF EXISTS trig_candidates_search_vector ON candidates;
CREATE TRIGGER trig_candidates_search_vector
    BEFORE INSERT OR UPDATE ON candidates
    FOR EACH ROW EXECUTE FUNCTION trigger_update_candidate_search_vector();

DROP TRIGGER IF EXISTS trig_resumes_search_vector ON resumes;
CREATE TRIGGER trig_resumes_search_vector
    BEFORE INSERT OR UPDATE ON resumes
    FOR EACH ROW EXECUTE FUNCTION trigger_update_resume_search_vector();

DROP TRIGGER IF EXISTS trig_skills_search_vector ON skills;
CREATE TRIGGER trig_skills_search_vector
    BEFORE INSERT OR UPDATE ON skills
    FOR EACH ROW EXECUTE FUNCTION trigger_update_skill_search_vector();

-- Create materialized view for fast candidate search
DROP MATERIALIZED VIEW IF EXISTS candidate_search_view;
CREATE MATERIALIZED VIEW candidate_search_view AS
SELECT 
    c.id as candidate_id,
    c.first_name,
    c.last_name,
    c.email,
    c.phone,
    c.city,
    c.state,
    c.country,
    c.current_title,
    c.total_years_experience as years_of_experience,
    c.search_vector as candidate_search_vector,
    r.resume_search_vector,
    string_agg(DISTINCT s.skill_name, ', ') as skills_text,
    to_tsvector('english', string_agg(DISTINCT s.skill_name, ' ')) as skills_search_vector,
    (
        setweight(c.search_vector, 'A') ||
        setweight(COALESCE(r.resume_search_vector, to_tsvector('english', '')), 'B') ||
        setweight(to_tsvector('english', COALESCE(string_agg(DISTINCT s.skill_name, ' '), '')), 'C')
    ) as combined_search_vector
FROM candidates c
LEFT JOIN resumes r ON c.id = r.candidate_id
LEFT JOIN candidate_skills cs ON c.id = cs.candidate_id
LEFT JOIN skills s ON cs.skill_id = s.id
GROUP BY c.id, c.first_name, c.last_name, c.email, c.phone, 
         c.city, c.state, c.country, c.current_title, 
         c.total_years_experience, c.search_vector, r.resume_search_vector;

-- Create indexes on the materialized view
CREATE UNIQUE INDEX idx_candidate_search_view_id ON candidate_search_view(candidate_id);
CREATE INDEX idx_candidate_search_view_combined ON candidate_search_view USING gin(combined_search_vector);

-- Function to refresh the materialized view
CREATE OR REPLACE FUNCTION refresh_candidate_search_view()
RETURNS void AS $$
BEGIN
    REFRESH MATERIALIZED VIEW CONCURRENTLY candidate_search_view;
END;
$$ LANGUAGE plpgsql;

-- Drop existing search function if it exists to avoid conflicts
DROP FUNCTION IF EXISTS search_candidates_fts(text, integer, integer);

-- Advanced search function with ranking
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

-- Function to get search suggestions
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
        s.skill_name as suggestion,
        similarity(s.skill_name, search_term) as similarity_score
    FROM skills s
    WHERE s.skill_name % search_term
    ORDER BY similarity_score DESC
    LIMIT suggestion_limit;
END;
$$ LANGUAGE plpgsql;

-- Initial refresh of the materialized view
SELECT refresh_candidate_search_view();

-- Verify the setup
SELECT 'Full-text search setup completed successfully!' as status,
       'Created search vectors, indexes, triggers, and materialized view' as details;