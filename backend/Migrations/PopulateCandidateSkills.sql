-- Intelligent Candidate Skills Population Script
-- Date: September 29, 2025
-- Description: Assigns skills to candidates based on job titles and creates realistic skill distributions

-- First, let's see what we're working with
SELECT 'Current state before population:' as status;
SELECT COUNT(*) as total_candidates FROM candidates WHERE is_active = true;
SELECT COUNT(*) as total_skills FROM skills;
SELECT COUNT(*) as current_skill_assignments FROM candidate_skills;

-- Create temporary mapping table for job title to skill assignments
CREATE TEMP TABLE temp_job_title_skills AS
WITH job_title_mappings AS (
    SELECT 
        LOWER(TRIM(current_title)) as job_title_lower,
        current_title,
        COUNT(*) as candidate_count
    FROM candidates 
    WHERE is_active = true AND current_title IS NOT NULL AND TRIM(current_title) != ''
    GROUP BY LOWER(TRIM(current_title)), current_title
    ORDER BY candidate_count DESC
),
skill_mappings AS (
    SELECT 
        'senior software engineer' as job_title_pattern,
        ARRAY['C#', '.NET', 'JavaScript', 'TypeScript', 'React', 'Angular', 'Node.js', 'Python', 'Java', 'SQL Server', 'PostgreSQL', 'Docker', 'Kubernetes', 'AWS', 'Azure', 'Agile/Scrum', 'Unit Testing', 'Git'] as skills
    UNION ALL SELECT 'software engineer', ARRAY['C#', '.NET', 'JavaScript', 'Python', 'Java', 'React', 'Node.js', 'PostgreSQL', 'MySQL', 'Docker', 'Git', 'Unit Testing', 'Agile/Scrum']
    UNION ALL SELECT 'senior full stack engineer', ARRAY['JavaScript', 'TypeScript', 'React', 'Angular', 'Vue.js', 'Node.js', 'Express.js', 'Python', 'Django', 'Flask', 'PostgreSQL', 'MongoDB', 'Docker', 'AWS', 'Git']
    UNION ALL SELECT 'lead software engineer', ARRAY['C#', '.NET', 'JavaScript', 'TypeScript', 'Python', 'Java', 'React', 'Angular', 'Docker', 'Kubernetes', 'AWS', 'Azure', 'Leadership', 'Team Management', 'Project Management', 'Agile/Scrum']
    UNION ALL SELECT 'software developer', ARRAY['C#', '.NET', 'JavaScript', 'Python', 'Java', 'React', 'PostgreSQL', 'MySQL', 'Git', 'Unit Testing']
    UNION ALL SELECT 'java developer', ARRAY['Java', 'Spring Boot', 'JavaScript', 'React', 'Angular', 'PostgreSQL', 'MySQL', 'Oracle Database', 'Docker', 'Jenkins', 'Git', 'Unit Testing', 'Maven']
    UNION ALL SELECT 'full stack developer', ARRAY['JavaScript', 'TypeScript', 'React', 'Angular', 'Node.js', 'Express.js', 'Python', 'PostgreSQL', 'MongoDB', 'HTML5', 'CSS3', 'Git']
    UNION ALL SELECT 'sql/bi developer', ARRAY['Microsoft SQL Server', 'PostgreSQL', 'MySQL', 'Oracle Database', 'Python', 'R', 'Data Analysis', 'Business Analysis', 'Tableau', 'Power BI']
    UNION ALL SELECT 'frontend developer', ARRAY['JavaScript', 'TypeScript', 'React', 'Angular', 'Vue.js', 'HTML5', 'CSS3', 'SASS/SCSS', 'Bootstrap', 'Tailwind CSS', 'Webpack', 'Git']
    UNION ALL SELECT 'backend developer', ARRAY['C#', '.NET', 'Python', 'Java', 'Node.js', 'Express.js', 'PostgreSQL', 'MySQL', 'MongoDB', 'Redis', 'Docker', 'AWS', 'Git']
    UNION ALL SELECT 'devops engineer', ARRAY['Docker', 'Kubernetes', 'AWS', 'Azure', 'Google Cloud Platform', 'Jenkins', 'GitLab CI/CD', 'Terraform', 'Ansible', 'Python', 'Bash', 'Git']
    UNION ALL SELECT 'data scientist', ARRAY['Python', 'R', 'Machine Learning', 'Deep Learning', 'TensorFlow', 'PyTorch', 'Pandas', 'NumPy', 'Data Analysis', 'SQL', 'PostgreSQL', 'Jupyter']
    UNION ALL SELECT 'product manager', ARRAY['Product Development', 'Project Management', 'Agile/Scrum', 'Strategic Planning', 'Business Analysis', 'Requirements Gathering', 'Stakeholder Management', 'Communication']
    UNION ALL SELECT 'mobile developer', ARRAY['iOS Development', 'Android Development', 'React Native', 'Flutter', 'Swift', 'Kotlin', 'Java', 'JavaScript', 'Git']
    UNION ALL SELECT 'default', ARRAY['JavaScript', 'HTML5', 'CSS3', 'Git', 'Agile/Scrum'] -- Default for unmatched titles
)
SELECT 
    jtm.job_title_lower,
    jtm.current_title,
    jtm.candidate_count,
    COALESCE(sm.skills, default_skills.skills) as skills
FROM job_title_mappings jtm
LEFT JOIN skill_mappings sm ON jtm.job_title_lower LIKE '%' || sm.job_title_pattern || '%'
CROSS JOIN (SELECT skills FROM skill_mappings WHERE job_title_pattern = 'default') default_skills
WHERE COALESCE(sm.skills, default_skills.skills) IS NOT NULL;

-- Show the mapping we created
SELECT 'Job title to skills mapping:' as status;
SELECT current_title, candidate_count, array_length(skills, 1) as skill_count 
FROM temp_job_title_skills 
ORDER BY candidate_count DESC;

-- Now insert candidate skills based on job titles
-- We'll assign 70-90% of the mapped skills to each candidate to create variety
INSERT INTO candidate_skills (candidate_id, skill_id, proficiency_level, years_of_experience, is_extracted)
SELECT DISTINCT
    c.id as candidate_id,
    s.id as skill_id,
    CASE 
        WHEN c.total_years_experience >= 8 THEN 'Expert'
        WHEN c.total_years_experience >= 5 THEN 'Advanced'
        WHEN c.total_years_experience >= 2 THEN 'Intermediate'
        ELSE 'Beginner'
    END as proficiency_level,
    LEAST(c.total_years_experience, 
          CASE 
              WHEN s.skill_name IN ('Leadership', 'Team Management', 'Project Management') THEN c.total_years_experience / 2
              ELSE c.total_years_experience
          END
    ) as years_of_experience,
    true as is_extracted
FROM candidates c
CROSS JOIN temp_job_title_skills tjts
CROSS JOIN LATERAL unnest(tjts.skills) AS skill_name_unnest
JOIN skills s ON s.skill_name = skill_name_unnest
WHERE c.is_active = true
    AND (
        LOWER(TRIM(COALESCE(c.current_title, ''))) LIKE '%' || tjts.job_title_lower || '%'
        OR tjts.job_title_lower = 'default'
    )
    -- Add some randomness: assign 70-90% of skills to each candidate
    AND (ABS(HASHTEXT(c.id::text || s.skill_name)) % 100) < (70 + (ABS(HASHTEXT(c.candidate_code)) % 21))
ON CONFLICT (candidate_id, skill_id) DO NOTHING;

-- Add some additional cross-training skills (popular skills get assigned to more people)
INSERT INTO candidate_skills (candidate_id, skill_id, proficiency_level, years_of_experience, is_extracted)
SELECT DISTINCT
    c.id as candidate_id,
    s.id as skill_id,
    'Intermediate' as proficiency_level,
    GREATEST(1, c.total_years_experience / 3) as years_of_experience,
    true as is_extracted
FROM candidates c
CROSS JOIN skills s
WHERE c.is_active = true
    AND s.skill_name IN ('Git', 'Agile/Scrum', 'Communication', 'Problem Solving', 'Unit Testing', 'JavaScript', 'HTML5', 'CSS3')
    -- 40-60% of candidates get these popular skills
    AND (ABS(HASHTEXT(c.id::text || s.skill_name || 'popular')) % 100) < (40 + (ABS(HASHTEXT(c.candidate_code || 'pop')) % 21))
ON CONFLICT (candidate_id, skill_id) DO NOTHING;

-- Show final results
SELECT 'Final population results:' as status;
SELECT COUNT(*) as total_skill_assignments FROM candidate_skills;
SELECT COUNT(DISTINCT candidate_id) as candidates_with_skills FROM candidate_skills;
SELECT COUNT(DISTINCT skill_id) as skills_assigned FROM candidate_skills;

-- Show top skills by frequency
SELECT 'Top 15 skills by frequency:' as status;
SELECT s.skill_name, COUNT(*) as frequency 
FROM candidate_skills cs 
JOIN skills s ON cs.skill_id = s.id 
JOIN candidates c ON cs.candidate_id = c.id 
WHERE c.is_active = true 
GROUP BY s.skill_name 
ORDER BY frequency DESC 
LIMIT 15;

-- Cleanup
DROP TABLE temp_job_title_skills;