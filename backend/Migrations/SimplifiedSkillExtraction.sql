-- Simplified Resume-Based Skill Extraction Script
-- Date: September 29, 2025
-- Description: Simple and robust skill extraction from resume text using direct text matching

-- Clear previous attempt
DELETE FROM candidate_skills;

-- Show current state
SELECT 'Starting simplified skill extraction:' as status;
SELECT COUNT(*) as resumes_with_text FROM resumes WHERE resume_text IS NOT NULL;

-- Direct skill extraction using simple text matching
-- This approach is more reliable than complex regex
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
    GREATEST(1, LEAST(c.total_years_experience, 
        CASE 
            WHEN s.skill_name IN ('Leadership', 'Team Management', 'Project Management') 
            THEN c.total_years_experience / 2
            ELSE c.total_years_experience * 0.8
        END
    )) as years_of_experience,
    true as is_extracted
FROM candidates c
JOIN resumes r ON c.id = r.candidate_id
CROSS JOIN skills s
WHERE c.is_active = true 
    AND r.resume_text IS NOT NULL
    AND LENGTH(TRIM(r.resume_text)) > 50
    AND (
        -- Direct skill name matches
        LOWER(r.resume_text) LIKE '%' || LOWER(s.skill_name) || '%'
        
        -- Handle specific variations
        OR (s.skill_name = 'C#' AND (LOWER(r.resume_text) LIKE '%c sharp%' OR LOWER(r.resume_text) LIKE '%csharp%'))
        OR (s.skill_name = '.NET' AND (LOWER(r.resume_text) LIKE '%dotnet%' OR LOWER(r.resume_text) LIKE '%dot net%'))
        OR (s.skill_name = 'JavaScript' AND LOWER(r.resume_text) LIKE '%js %')
        OR (s.skill_name = 'TypeScript' AND LOWER(r.resume_text) LIKE '%ts %')
        OR (s.skill_name = 'React' AND (LOWER(r.resume_text) LIKE '%reactjs%' OR LOWER(r.resume_text) LIKE '%react.js%'))
        OR (s.skill_name = 'Node.js' AND (LOWER(r.resume_text) LIKE '%nodejs%' OR LOWER(r.resume_text) LIKE '%node js%'))
        OR (s.skill_name = 'Vue.js' AND (LOWER(r.resume_text) LIKE '%vuejs%' OR LOWER(r.resume_text) LIKE '%vue %'))
        OR (s.skill_name = 'Angular' AND LOWER(r.resume_text) LIKE '%angularjs%')
        OR (s.skill_name = 'PostgreSQL' AND (LOWER(r.resume_text) LIKE '%postgres%' OR LOWER(r.resume_text) LIKE '%psql%'))
        OR (s.skill_name = 'MongoDB' AND LOWER(r.resume_text) LIKE '%mongo%')
        OR (s.skill_name = 'Kubernetes' AND LOWER(r.resume_text) LIKE '%k8s%')
        OR (s.skill_name = 'Machine Learning' AND LOWER(r.resume_text) LIKE '%ml %')
        OR (s.skill_name = 'AI/ML' AND (LOWER(r.resume_text) LIKE '%artificial intelligence%' OR LOWER(r.resume_text) LIKE '% ai %'))
        OR (s.skill_name = 'Spring Boot' AND LOWER(r.resume_text) LIKE '%spring framework%')
        OR (s.skill_name = 'Express.js' AND LOWER(r.resume_text) LIKE '%express %')
        OR (s.skill_name = 'Microsoft SQL Server' AND (LOWER(r.resume_text) LIKE '%sql server%' OR LOWER(r.resume_text) LIKE '%mssql%'))
        OR (s.skill_name = 'HTML5' AND LOWER(r.resume_text) LIKE '%html%')
        OR (s.skill_name = 'CSS3' AND LOWER(r.resume_text) LIKE '%css%')
        OR (s.skill_name = 'SASS/SCSS' AND (LOWER(r.resume_text) LIKE '%sass%' OR LOWER(r.resume_text) LIKE '%scss%'))
        OR (s.skill_name = 'iOS Development' AND LOWER(r.resume_text) LIKE '%ios%')
        OR (s.skill_name = 'Android Development' AND LOWER(r.resume_text) LIKE '%android%')
        OR (s.skill_name = 'AWS' AND LOWER(r.resume_text) LIKE '%amazon web services%')
        OR (s.skill_name = 'Microsoft Azure' AND LOWER(r.resume_text) LIKE '%azure%')
        OR (s.skill_name = 'Google Cloud Platform' AND (LOWER(r.resume_text) LIKE '%google cloud%' OR LOWER(r.resume_text) LIKE '%gcp%'))
        OR (s.skill_name = 'Agile/Scrum' AND (LOWER(r.resume_text) LIKE '%agile%' OR LOWER(r.resume_text) LIKE '%scrum%'))
        OR (s.skill_name = 'TDD' AND LOWER(r.resume_text) LIKE '%test driven development%')
        OR (s.skill_name = 'BDD' AND LOWER(r.resume_text) LIKE '%behavior driven development%')
        OR (s.skill_name = 'Copilot' AND LOWER(r.resume_text) LIKE '%github copilot%')
        OR (s.skill_name = 'Claude' AND LOWER(r.resume_text) LIKE '%claude ai%')
    )
ON CONFLICT (candidate_id, skill_id) DO NOTHING;

-- Add implied skills based on job roles and technologies
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
    AND (
        -- Git for software engineers/developers
        (s.skill_name = 'Git' AND (
            LOWER(r.resume_text) LIKE '%software engineer%' OR 
            LOWER(r.resume_text) LIKE '%developer%' OR 
            LOWER(r.resume_text) LIKE '%programmer%'
        ))
        -- Problem Solving for technical roles
        OR (s.skill_name = 'Problem Solving' AND (
            LOWER(r.resume_text) LIKE '%engineer%' OR 
            LOWER(r.resume_text) LIKE '%developer%' OR 
            LOWER(r.resume_text) LIKE '%analyst%' OR
            LOWER(r.resume_text) LIKE '%architect%'
        ))
        -- Communication for senior/lead roles
        OR (s.skill_name = 'Communication' AND (
            LOWER(r.resume_text) LIKE '%senior%' OR 
            LOWER(r.resume_text) LIKE '%lead%' OR 
            LOWER(r.resume_text) LIKE '%manager%' OR
            LOWER(r.resume_text) LIKE '%director%'
        ))
        -- Leadership for management roles
        OR (s.skill_name = 'Leadership' AND (
            LOWER(r.resume_text) LIKE '%lead%' OR 
            LOWER(r.resume_text) LIKE '%manager%' OR 
            LOWER(r.resume_text) LIKE '%director%' OR
            LOWER(r.resume_text) LIKE '%head of%'
        ))
        -- Unit Testing for software engineers
        OR (s.skill_name = 'Unit Testing' AND (
            LOWER(r.resume_text) LIKE '%software engineer%' OR 
            LOWER(r.resume_text) LIKE '%developer%' OR
            LOWER(r.resume_text) LIKE '%qa%' OR
            LOWER(r.resume_text) LIKE '%testing%'
        ))
        -- Project Management for leads/seniors
        OR (s.skill_name = 'Project Management' AND (
            LOWER(r.resume_text) LIKE '%project manager%' OR
            LOWER(r.resume_text) LIKE '%lead%' OR 
            LOWER(r.resume_text) LIKE '%senior%' OR
            LOWER(r.resume_text) LIKE '%manager%'
        ))
        -- Agile/Scrum is very common in software development
        OR (s.skill_name = 'Agile/Scrum' AND (
            LOWER(r.resume_text) LIKE '%software%' OR 
            LOWER(r.resume_text) LIKE '%developer%' OR 
            LOWER(r.resume_text) LIKE '%engineer%'
        ))
    )
ON CONFLICT (candidate_id, skill_id) DO NOTHING;

-- Show final results
SELECT 'Simplified skill extraction completed!' as status;
SELECT COUNT(*) as total_skill_assignments FROM candidate_skills;
SELECT COUNT(DISTINCT candidate_id) as candidates_with_skills FROM candidate_skills;
SELECT COUNT(DISTINCT skill_id) as unique_skills_found FROM candidate_skills;

-- Show top skills found
SELECT 'Top 25 skills extracted from resumes:' as status;
SELECT s.skill_name, COUNT(*) as frequency 
FROM candidate_skills cs 
JOIN skills s ON cs.skill_id = s.id 
JOIN candidates c ON cs.candidate_id = c.id 
WHERE c.is_active = true 
GROUP BY s.skill_name 
ORDER BY frequency DESC 
LIMIT 25;

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