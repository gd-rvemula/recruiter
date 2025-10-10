-- Simple Test Query to Verify FTS is Working
-- This will help us debug the C# service issues

-- Test the actual data in the database
SELECT 'Testing basic candidate data:' as test;
SELECT COUNT(*) as total_candidates FROM candidates WHERE is_active = true;

SELECT 'Testing search vectors:' as test;
SELECT COUNT(*) as candidates_with_vectors FROM candidates WHERE search_vector IS NOT NULL;

-- Test direct search without function
SELECT 'Testing direct search:' as test;
SELECT id, candidate_code, first_name, last_name, current_title 
FROM candidates 
WHERE search_vector @@ plainto_tsquery('english', 'software')
  AND is_active = true 
LIMIT 3;

-- Test our function
SELECT 'Testing our function:' as test;
SELECT candidate_id, candidate_code, first_name, last_name, current_title, search_rank
FROM search_candidates_fts('software', 3, 0);