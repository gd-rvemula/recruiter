-- Create a PostgreSQL function for skill extraction that can be called from .NET
-- This allows the Excel import service to extract skills from resume text during import

CREATE OR REPLACE FUNCTION extract_skills_from_text(resume_text TEXT)
RETURNS TABLE(skill_id UUID, skill_name VARCHAR(100), match_confidence INTEGER) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        s.id as skill_id,
        s.skill_name,
        CASE 
            -- Direct skill name matches (highest confidence)
            WHEN LOWER(resume_text) LIKE '%' || LOWER(s.skill_name) || '%' THEN 10
            -- Handle specific variations (high confidence)
            WHEN (s.skill_name = 'C#' AND (LOWER(resume_text) LIKE '%c sharp%' OR LOWER(resume_text) LIKE '%csharp%')) THEN 9
            WHEN (s.skill_name = '.NET' AND (LOWER(resume_text) LIKE '%dotnet%' OR LOWER(resume_text) LIKE '%dot net%')) THEN 9
            WHEN (s.skill_name = 'JavaScript' AND LOWER(resume_text) LIKE '%js %') THEN 8
            WHEN (s.skill_name = 'TypeScript' AND LOWER(resume_text) LIKE '%ts %') THEN 8
            WHEN (s.skill_name = 'React' AND (LOWER(resume_text) LIKE '%reactjs%' OR LOWER(resume_text) LIKE '%react.js%')) THEN 9
            WHEN (s.skill_name = 'Node.js' AND (LOWER(resume_text) LIKE '%nodejs%' OR LOWER(resume_text) LIKE '%node js%')) THEN 9
            WHEN (s.skill_name = 'Vue.js' AND (LOWER(resume_text) LIKE '%vuejs%' OR LOWER(resume_text) LIKE '%vue %')) THEN 8
            WHEN (s.skill_name = 'Angular' AND LOWER(resume_text) LIKE '%angularjs%') THEN 8
            WHEN (s.skill_name = 'PostgreSQL' AND (LOWER(resume_text) LIKE '%postgres%' OR LOWER(resume_text) LIKE '%psql%')) THEN 9
            WHEN (s.skill_name = 'MongoDB' AND LOWER(resume_text) LIKE '%mongo%') THEN 8
            WHEN (s.skill_name = 'Kubernetes' AND LOWER(resume_text) LIKE '%k8s%') THEN 8
            WHEN (s.skill_name = 'Machine Learning' AND LOWER(resume_text) LIKE '%ml %') THEN 7
            WHEN (s.skill_name = 'AI/ML' AND (LOWER(resume_text) LIKE '%artificial intelligence%' OR LOWER(resume_text) LIKE '% ai %')) THEN 8
            WHEN (s.skill_name = 'AWS' AND LOWER(resume_text) LIKE '%amazon web services%') THEN 9
            WHEN (s.skill_name = 'Microsoft Azure' AND LOWER(resume_text) LIKE '%azure%') THEN 9
            WHEN (s.skill_name = 'Google Cloud Platform' AND (LOWER(resume_text) LIKE '%google cloud%' OR LOWER(resume_text) LIKE '%gcp%')) THEN 9
            WHEN (s.skill_name = 'Agile/Scrum' AND (LOWER(resume_text) LIKE '%agile%' OR LOWER(resume_text) LIKE '%scrum%')) THEN 8
            WHEN (s.skill_name = 'HTML5' AND LOWER(resume_text) LIKE '%html%') THEN 8
            WHEN (s.skill_name = 'CSS3' AND LOWER(resume_text) LIKE '%css%') THEN 8
            ELSE 0
        END as match_confidence
    FROM skills s
    WHERE (
        -- Direct matches
        LOWER(resume_text) LIKE '%' || LOWER(s.skill_name) || '%'
        -- Or specific variations
        OR (s.skill_name = 'C#' AND (LOWER(resume_text) LIKE '%c sharp%' OR LOWER(resume_text) LIKE '%csharp%'))
        OR (s.skill_name = '.NET' AND (LOWER(resume_text) LIKE '%dotnet%' OR LOWER(resume_text) LIKE '%dot net%'))
        OR (s.skill_name = 'JavaScript' AND LOWER(resume_text) LIKE '%js %')
        OR (s.skill_name = 'TypeScript' AND LOWER(resume_text) LIKE '%ts %')
        OR (s.skill_name = 'React' AND (LOWER(resume_text) LIKE '%reactjs%' OR LOWER(resume_text) LIKE '%react.js%'))
        OR (s.skill_name = 'Node.js' AND (LOWER(resume_text) LIKE '%nodejs%' OR LOWER(resume_text) LIKE '%node js%'))
        OR (s.skill_name = 'Vue.js' AND (LOWER(resume_text) LIKE '%vuejs%' OR LOWER(resume_text) LIKE '%vue %'))
        OR (s.skill_name = 'Angular' AND LOWER(resume_text) LIKE '%angularjs%')
        OR (s.skill_name = 'PostgreSQL' AND (LOWER(resume_text) LIKE '%postgres%' OR LOWER(resume_text) LIKE '%psql%'))
        OR (s.skill_name = 'MongoDB' AND LOWER(resume_text) LIKE '%mongo%')
        OR (s.skill_name = 'Kubernetes' AND LOWER(resume_text) LIKE '%k8s%')
        OR (s.skill_name = 'Machine Learning' AND LOWER(resume_text) LIKE '%ml %')
        OR (s.skill_name = 'AI/ML' AND (LOWER(resume_text) LIKE '%artificial intelligence%' OR LOWER(resume_text) LIKE '% ai %'))
        OR (s.skill_name = 'AWS' AND LOWER(resume_text) LIKE '%amazon web services%')
        OR (s.skill_name = 'Microsoft Azure' AND LOWER(resume_text) LIKE '%azure%')
        OR (s.skill_name = 'Google Cloud Platform' AND (LOWER(resume_text) LIKE '%google cloud%' OR LOWER(resume_text) LIKE '%gcp%'))
        OR (s.skill_name = 'Agile/Scrum' AND (LOWER(resume_text) LIKE '%agile%' OR LOWER(resume_text) LIKE '%scrum%'))
        OR (s.skill_name = 'HTML5' AND LOWER(resume_text) LIKE '%html%')
        OR (s.skill_name = 'CSS3' AND LOWER(resume_text) LIKE '%css%')
    )
    AND LENGTH(resume_text) > 50  -- Ensure substantial resume content
    ORDER BY match_confidence DESC, s.skill_name;
END;
$$ LANGUAGE plpgsql;

-- Test the function
SELECT 'Testing skill extraction function for Excel import:' as status;
SELECT skill_name, match_confidence 
FROM extract_skills_from_text('Senior Software Engineer with C#, .NET, JavaScript, React, PostgreSQL, AWS experience') 
LIMIT 10;