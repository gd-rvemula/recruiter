-- Resume-Based Skill Extraction Script
-- Date: September 29, 2025
-- Description: Extracts skills from candidate resume text by matching against the 113 skills catalog

-- First, let's see what we're working with
SELECT 'Current state before resume-based extraction:' as status;
SELECT COUNT(*) as total_candidates FROM candidates WHERE is_active = true;
SELECT COUNT(*) as total_skills FROM skills;
SELECT COUNT(*) as resumes_with_text FROM resumes WHERE resume_text IS NOT NULL;
SELECT COUNT(*) as current_skill_assignments FROM candidate_skills;

-- Create a comprehensive skill matching function
-- This will handle various forms of skill names in resumes
CREATE OR REPLACE FUNCTION extract_skills_from_resume_text(resume_text TEXT)
RETURNS TABLE(skill_name VARCHAR(100), match_score INTEGER) AS $$
BEGIN
    RETURN QUERY
    WITH skill_variations AS (
        SELECT 
            s.skill_name,
            s.skill_name as search_term,
            10 as base_score
        FROM skills s
        
        UNION ALL
        
        -- Handle common variations and aliases
        SELECT s.skill_name, 'C Sharp', 8 FROM skills s WHERE s.skill_name = 'C#'
        UNION ALL SELECT s.skill_name, 'CSharp', 8 FROM skills s WHERE s.skill_name = 'C#'
        UNION ALL SELECT s.skill_name, 'DotNet', 8 FROM skills s WHERE s.skill_name = '.NET'
        UNION ALL SELECT s.skill_name, 'Dot Net', 8 FROM skills s WHERE s.skill_name = '.NET'
        UNION ALL SELECT s.skill_name, 'JS', 6 FROM skills s WHERE s.skill_name = 'JavaScript'
        UNION ALL SELECT s.skill_name, 'TS', 6 FROM skills s WHERE s.skill_name = 'TypeScript'
        UNION ALL SELECT s.skill_name, 'ReactJS', 9 FROM skills s WHERE s.skill_name = 'React'
        UNION ALL SELECT s.skill_name, 'React.js', 9 FROM skills s WHERE s.skill_name = 'React'
        UNION ALL SELECT s.skill_name, 'NodeJS', 9 FROM skills s WHERE s.skill_name = 'Node.js'
        UNION ALL SELECT s.skill_name, 'VueJS', 9 FROM skills s WHERE s.skill_name = 'Vue.js'
        UNION ALL SELECT s.skill_name, 'Vue', 8 FROM skills s WHERE s.skill_name = 'Vue.js'
        UNION ALL SELECT s.skill_name, 'AngularJS', 8 FROM skills s WHERE s.skill_name = 'Angular'
        UNION ALL SELECT s.skill_name, 'Postgres', 9 FROM skills s WHERE s.skill_name = 'PostgreSQL'
        UNION ALL SELECT s.skill_name, 'Mongo', 8 FROM skills s WHERE s.skill_name = 'MongoDB'
        UNION ALL SELECT s.skill_name, 'K8s', 8 FROM skills s WHERE s.skill_name = 'Kubernetes'
        UNION ALL SELECT s.skill_name, 'ML', 6 FROM skills s WHERE s.skill_name = 'Machine Learning'
        UNION ALL SELECT s.skill_name, 'AI', 6 FROM skills s WHERE s.skill_name = 'AI/ML'
        UNION ALL SELECT s.skill_name, 'Artificial Intelligence', 9 FROM skills s WHERE s.skill_name = 'AI/ML'
        UNION ALL SELECT s.skill_name, 'TensorFlow.js', 8 FROM skills s WHERE s.skill_name = 'TensorFlow'
        UNION ALL SELECT s.skill_name, 'PyTorch Lightning', 8 FROM skills s WHERE s.skill_name = 'PyTorch'
        UNION ALL SELECT s.skill_name, 'Spring Framework', 9 FROM skills s WHERE s.skill_name = 'Spring Boot'
        UNION ALL SELECT s.skill_name, 'Express', 8 FROM skills s WHERE s.skill_name = 'Express.js'
        UNION ALL SELECT s.skill_name, 'SQL Server', 9 FROM skills s WHERE s.skill_name = 'Microsoft SQL Server'
        UNION ALL SELECT s.skill_name, 'MSSQL', 8 FROM skills s WHERE s.skill_name = 'Microsoft SQL Server'
        UNION ALL SELECT s.skill_name, 'CSS', 8 FROM skills s WHERE s.skill_name = 'CSS3'
        UNION ALL SELECT s.skill_name, 'HTML', 8 FROM skills s WHERE s.skill_name = 'HTML5'
        UNION ALL SELECT s.skill_name, 'Sass', 9 FROM skills s WHERE s.skill_name = 'SASS/SCSS'
        UNION ALL SELECT s.skill_name, 'SCSS', 9 FROM skills s WHERE s.skill_name = 'SASS/SCSS'
        UNION ALL SELECT s.skill_name, 'React Native', 10 FROM skills s WHERE s.skill_name = 'React Native'
        UNION ALL SELECT s.skill_name, 'iOS', 8 FROM skills s WHERE s.skill_name = 'iOS Development'
        UNION ALL SELECT s.skill_name, 'Android', 8 FROM skills s WHERE s.skill_name = 'Android Development'
        UNION ALL SELECT s.skill_name, 'AWS', 10 FROM skills s WHERE s.skill_name = 'AWS'
        UNION ALL SELECT s.skill_name, 'Amazon Web Services', 10 FROM skills s WHERE s.skill_name = 'AWS'
        UNION ALL SELECT s.skill_name, 'Azure', 10 FROM skills s WHERE s.skill_name = 'Microsoft Azure'
        UNION ALL SELECT s.skill_name, 'GCP', 8 FROM skills s WHERE s.skill_name = 'Google Cloud Platform'
        UNION ALL SELECT s.skill_name, 'Google Cloud', 9 FROM skills s WHERE s.skill_name = 'Google Cloud Platform'
        UNION ALL SELECT s.skill_name, 'Agile', 8 FROM skills s WHERE s.skill_name = 'Agile/Scrum'
        UNION ALL SELECT s.skill_name, 'Scrum', 8 FROM skills s WHERE s.skill_name = 'Agile/Scrum'
        UNION ALL SELECT s.skill_name, 'TDD', 10 FROM skills s WHERE s.skill_name = 'TDD'
        UNION ALL SELECT s.skill_name, 'Test Driven Development', 10 FROM skills s WHERE s.skill_name = 'TDD'
        UNION ALL SELECT s.skill_name, 'BDD', 10 FROM skills s WHERE s.skill_name = 'BDD'
        UNION ALL SELECT s.skill_name, 'Behavior Driven Development', 10 FROM skills s WHERE s.skill_name = 'BDD'
        UNION ALL SELECT s.skill_name, 'GitHub Copilot', 9 FROM skills s WHERE s.skill_name = 'Copilot'
        UNION ALL SELECT s.skill_name, 'Claude AI', 9 FROM skills s WHERE s.skill_name = 'Claude'
        UNION ALL SELECT s.skill_name, 'Agentic AI', 10 FROM skills s WHERE s.skill_name = 'Agentic AI'
        UNION ALL SELECT s.skill_name, 'AI Agents', 10 FROM skills s WHERE s.skill_name = 'AI Agents'
    ),
    matched_skills AS (
        SELECT DISTINCT
            sv.skill_name,
            sv.base_score,
            CASE 
                -- Exact match (case insensitive)
                WHEN LOWER(resume_text) ~ ('\y' || LOWER(sv.search_term) || '\y') THEN sv.base_score + 5
                -- Word boundary match with some flexibility
                WHEN LOWER(resume_text) ~ LOWER(sv.search_term) THEN sv.base_score + 2
                -- Partial match
                WHEN POSITION(LOWER(sv.search_term) IN LOWER(resume_text)) > 0 THEN sv.base_score
                ELSE 0
            END as final_score
        FROM skill_variations sv
        WHERE POSITION(LOWER(sv.search_term) IN LOWER(resume_text)) > 0
    )
    SELECT 
        ms.skill_name::VARCHAR(100),
        ms.final_score as match_score
    FROM matched_skills ms
    WHERE ms.final_score >= 8  -- Only return high-confidence matches
    ORDER BY ms.final_score DESC, ms.skill_name;
END;
$$ LANGUAGE plpgsql;

-- Test the function on a sample resume
SELECT 'Testing skill extraction function:' as status;
SELECT * FROM extract_skills_from_resume_text(
    'Senior Software Engineer with expertise in C#, .NET, JavaScript, React, Node.js, PostgreSQL, Docker, AWS, and Agile/Scrum methodologies. 
    Experience with TensorFlow for machine learning projects and Jenkins for CI/CD.'
) LIMIT 10;

-- Now extract skills for all candidates based on their resume text
INSERT INTO candidate_skills (candidate_id, skill_id, proficiency_level, years_of_experience, is_extracted)
SELECT DISTINCT
    c.id as candidate_id,
    s.id as skill_id,
    CASE 
        WHEN c.total_years_experience >= 10 THEN 'Expert'
        WHEN c.total_years_experience >= 6 THEN 'Advanced'  
        WHEN c.total_years_experience >= 3 THEN 'Intermediate'
        ELSE 'Beginner'
    END as proficiency_level,
    CASE 
        -- Leadership skills are typically acquired later in career
        WHEN s.skill_name IN ('Leadership', 'Team Management', 'Project Management', 'Strategic Planning', 'Stakeholder Management') 
        THEN GREATEST(1, LEAST(c.total_years_experience - 2, c.total_years_experience / 2))
        -- Technical skills can be learned throughout career
        ELSE GREATEST(1, LEAST(c.total_years_experience, 
            CASE 
                WHEN extracted.match_score >= 12 THEN c.total_years_experience  -- High confidence, full experience
                WHEN extracted.match_score >= 10 THEN c.total_years_experience * 0.8  -- Good confidence
                ELSE c.total_years_experience * 0.6  -- Moderate confidence
            END
        ))
    END as years_of_experience,
    true as is_extracted
FROM candidates c
JOIN resumes r ON c.id = r.candidate_id
CROSS JOIN LATERAL extract_skills_from_resume_text(r.resume_text) AS extracted
JOIN skills s ON s.skill_name = extracted.skill_name
WHERE c.is_active = true 
    AND r.resume_text IS NOT NULL
    AND LENGTH(TRIM(r.resume_text)) > 50  -- Ensure we have substantial resume content
ON CONFLICT (candidate_id, skill_id) DO NOTHING;

-- Add some common skills that might be mentioned in different ways or implied
INSERT INTO candidate_skills (candidate_id, skill_id, proficiency_level, years_of_experience, is_extracted)
SELECT DISTINCT
    c.id as candidate_id,
    s.id as skill_id,
    'Intermediate' as proficiency_level,
    GREATEST(1, c.total_years_experience / 2) as years_of_experience,
    true as is_extracted
FROM candidates c
JOIN resumes r ON c.id = r.candidate_id
CROSS JOIN skills s
WHERE c.is_active = true 
    AND r.resume_text IS NOT NULL
    -- Add Git to software engineers (almost universal)
    AND (
        (s.skill_name = 'Git' AND (
            LOWER(r.resume_text) ~ 'software engineer' OR 
            LOWER(r.resume_text) ~ 'developer' OR 
            LOWER(r.resume_text) ~ 'programmer'
        ))
        -- Add Problem Solving to all technical roles
        OR (s.skill_name = 'Problem Solving' AND (
            LOWER(r.resume_text) ~ 'engineer' OR 
            LOWER(r.resume_text) ~ 'developer' OR 
            LOWER(r.resume_text) ~ 'analyst'
        ))
        -- Add Communication to lead/senior roles
        OR (s.skill_name = 'Communication' AND (
            LOWER(r.resume_text) ~ 'senior' OR 
            LOWER(r.resume_text) ~ 'lead' OR 
            LOWER(r.resume_text) ~ 'manager'
        ))
    )
ON CONFLICT (candidate_id, skill_id) DO NOTHING;

-- Show extraction results
SELECT 'Resume-based skill extraction completed!' as status;
SELECT COUNT(*) as total_skill_assignments FROM candidate_skills;
SELECT COUNT(DISTINCT candidate_id) as candidates_with_skills FROM candidate_skills;
SELECT COUNT(DISTINCT skill_id) as unique_skills_found FROM candidate_skills;

-- Show top skills found in resumes
SELECT 'Top 20 skills extracted from resumes:' as status;
SELECT s.skill_name, COUNT(*) as frequency 
FROM candidate_skills cs 
JOIN skills s ON cs.skill_id = s.id 
JOIN candidates c ON cs.candidate_id = c.id 
WHERE c.is_active = true 
GROUP BY s.skill_name 
ORDER BY frequency DESC 
LIMIT 20;

-- Show skill distribution by proficiency level
SELECT 'Skill distribution by proficiency level:' as status;
SELECT proficiency_level, COUNT(*) as count
FROM candidate_skills cs
JOIN candidates c ON cs.candidate_id = c.id
WHERE c.is_active = true
GROUP BY proficiency_level
ORDER BY 
    CASE proficiency_level 
        WHEN 'Expert' THEN 4 
        WHEN 'Advanced' THEN 3 
        WHEN 'Intermediate' THEN 2 
        WHEN 'Beginner' THEN 1 
    END DESC;

-- Show candidates with most skills
SELECT 'Top 10 candidates by skill count:' as status;
SELECT c.full_name, c.current_title, COUNT(*) as skill_count
FROM candidates c
JOIN candidate_skills cs ON c.id = cs.candidate_id
WHERE c.is_active = true
GROUP BY c.id, c.full_name, c.current_title
ORDER BY skill_count DESC
LIMIT 10;

-- Drop the temporary function
DROP FUNCTION extract_skills_from_resume_text(TEXT);