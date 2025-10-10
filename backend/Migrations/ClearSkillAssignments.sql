-- Clear Current Skill Assignments Migration
-- Date: September 29, 2025
-- Description: Remove all current candidate-skill assignments to prepare for proper skill assignment

-- Show current state before clearing
SELECT 'Before clearing:' as status;
SELECT COUNT(*) as total_candidate_skills FROM candidate_skills;
SELECT skill_id, COUNT(*) as count FROM candidate_skills GROUP BY skill_id ORDER BY skill_id;

-- Clear all candidate-skill assignments
DELETE FROM candidate_skills;

-- Show state after clearing
SELECT 'After clearing:' as status;
SELECT COUNT(*) as total_candidate_skills FROM candidate_skills;

-- Verify skills table is intact
SELECT 'Skills table status:' as status;
SELECT COUNT(*) as total_skills FROM skills;

-- Test the skills frequency API result (should show all 0s now)
SELECT 'Skills frequency after clearing:' as status;
SELECT s.skill_name, COALESCE(skill_counts.frequency, 0) as frequency 
FROM skills s 
LEFT JOIN (
    SELECT skill_id, COUNT(*) as frequency 
    FROM candidate_skills cs 
    JOIN candidates c ON cs.candidate_id = c.id 
    WHERE c.is_active = true 
    GROUP BY skill_id
) skill_counts ON s.id = skill_counts.skill_id 
ORDER BY frequency DESC, s.skill_name 
LIMIT 10;