-- Test Full Text Search Setup
-- This script tests the FTS functionality

-- Check if we have candidates with search vectors
SELECT 'Checking candidates with search vectors:' as test;
SELECT COUNT(*) as total_candidates, 
       COUNT(search_vector) as candidates_with_search_vector,
       COUNT(CASE WHEN search_vector IS NOT NULL THEN 1 END) as non_null_search_vectors
FROM candidates;

-- Check if we have resumes with search vectors
SELECT 'Checking resumes with search vectors:' as test;
SELECT COUNT(*) as total_resumes,
       COUNT(resume_search_vector) as resumes_with_search_vector,
       COUNT(CASE WHEN resume_text IS NOT NULL THEN 1 END) as resumes_with_text
FROM resumes;

-- Check the materialized view
SELECT 'Checking materialized view:' as test;
SELECT COUNT(*) as total_in_view FROM candidate_search_view;

-- Test search with common terms
SELECT 'Testing search with common skills:' as test;
SELECT candidate_id, first_name, last_name, current_title, skills_text, search_rank
FROM search_candidates_fts('java', 3, 0);

SELECT 'Testing search with technology terms:' as test;
SELECT candidate_id, first_name, last_name, current_title, skills_text, search_rank
FROM search_candidates_fts('software', 3, 0);

-- Test suggestions
SELECT 'Testing search suggestions:' as test;
SELECT suggestion, similarity_score 
FROM get_search_suggestions('java', 5);