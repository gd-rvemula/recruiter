-- Final Test of Full Text Search API Functions
-- This tests the exact functions that the C# API will use

-- Test the main search function
SELECT 'Testing main search function:' as test;
SELECT candidate_id, first_name, last_name, current_title, search_rank, highlight_snippet
FROM search_candidates_fts('software engineer', 5, 0);

SELECT 'Testing resume text search:' as test;
SELECT candidate_id, first_name, last_name, current_title, search_rank
FROM search_candidates_fts('developer experience', 3, 0);

-- Test basic search queries that the API might use
SELECT 'Testing skill-based search:' as test;
SELECT candidate_id, first_name, last_name, current_title, skills_text, search_rank
FROM search_candidates_fts('C# .NET', 3, 0);

-- Verify materialized view data
SELECT 'Sample data from materialized view:' as test;
SELECT candidate_id, first_name, last_name, current_title, skills_text
FROM candidate_search_view 
LIMIT 3;