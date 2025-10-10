-- Test the Enhanced Excel Import Service Skill Extraction
-- Date: September 29, 2025
-- Description: Demonstrates comprehensive skill extraction with word count algorithm

-- Show current skills in database
SELECT 'Current skills in database:' as status;
SELECT COUNT(*) as total_skills FROM skills;

-- Show a sample of all available skills that can be matched
SELECT 'Sample of available skills for matching:' as status;
SELECT skill_name 
FROM skills 
ORDER BY skill_name 
LIMIT 25;

-- Show current candidate-skill assignments
SELECT 'Current skill assignments:' as status;
SELECT COUNT(*) as total_assignments FROM candidate_skills;
SELECT COUNT(DISTINCT candidate_id) as candidates_with_skills FROM candidate_skills;
SELECT COUNT(DISTINCT skill_id) as unique_skills_assigned FROM candidate_skills;

-- Show top 10 most common skills currently assigned
SELECT 'Top 10 most assigned skills:' as status;
SELECT s.skill_name, COUNT(*) as frequency
FROM candidate_skills cs
JOIN skills s ON cs.skill_id = s.id
JOIN candidates c ON cs.candidate_id = c.id
WHERE c.is_active = true
GROUP BY s.skill_name
ORDER BY frequency DESC
LIMIT 10;