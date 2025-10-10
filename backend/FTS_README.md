# Full-Text Search Infrastructure - Permanent Setup

This document outlines the permanent Full-Text Search (FTS) infrastructure for the Recruiter system.

## Overview

The FTS system is now a **permanent part** of the database schema and will be automatically initialized when the application starts.

## Architecture

### Database Components

1. **Search Vector Columns**
   - `candidates.search_vector` - Indexes names, emails, titles
   - `resumes.search_vector` - Indexes resume content and filenames  
   - `skills.search_vector` - Indexes skill names, descriptions, categories

2. **Automatic Triggers**
   - `trig_candidates_search_vector` - Updates candidate search vectors on INSERT/UPDATE
   - `trig_resumes_search_vector` - Updates resume search vectors on INSERT/UPDATE
   - `trig_skills_search_vector` - Updates skill search vectors on INSERT/UPDATE

3. **Materialized View**
   - `candidate_search_view` - Combines candidate, skills, and resume data for optimized search
   - Includes combined search vectors for cross-table searching

4. **Search Functions**
   - `search_candidates_fts()` - Main search function with pagination and ranking
   - `get_search_suggestions()` - Provides autocomplete suggestions
   - `refresh_candidate_search_view()` - Refreshes materialized view

### Entity Framework Models

All models now permanently include search vector properties:

```csharp
// In Candidate.cs
[Column("search_vector")]
public string? SearchVector { get; set; }

// In Resume.cs  
[Column("search_vector")]
public string? SearchVector { get; set; }

// In Skill.cs
[Column("search_vector")] 
public string? SearchVector { get; set; }
```

## Automatic Initialization

The FTS infrastructure is automatically initialized when the application starts:

1. **Program.cs** checks if FTS columns exist
2. If not found, automatically creates:
   - Search vector columns
   - Required extensions (pg_trgm)
   - Basic infrastructure

3. Full setup can be completed by running `PermanentFTS.sql`

## API Endpoints

### Primary Search Endpoint
```
POST /api/candidates/search
```
- Automatically uses FTS when `searchTerm` is provided
- Falls back to basic search for filters only
- Returns paginated results with relevance ranking

### Advanced FTS Endpoint
```
POST /api/search/fts
```
- Pure full-text search with PostgreSQL ranking
- Optimized for complex queries
- Uses materialized view for best performance

## Performance Features

1. **GIN Indexes** on all search vector columns
2. **Materialized View** with combined search vectors
3. **Automatic Updates** via database triggers
4. **Relevance Ranking** using PostgreSQL's ts_rank

## Maintenance

### Refresh Search Data
```sql
-- Refresh materialized view after bulk data changes
REFRESH MATERIALIZED VIEW candidate_search_view;

-- Or use the function
SELECT refresh_candidate_search_view();
```

### Update Search Vectors
Search vectors are automatically updated by triggers, but can be manually refreshed:

```sql
-- Update all candidate search vectors
UPDATE candidates SET search_vector = 
    setweight(to_tsvector('english', COALESCE(first_name, '')), 'A') ||
    setweight(to_tsvector('english', COALESCE(last_name, '')), 'A') ||
    setweight(to_tsvector('english', COALESCE(email, '')), 'B') ||
    setweight(to_tsvector('english', COALESCE(current_title, '')), 'A');
```

## Migration Scripts

### Setup Scripts (in order)
1. `PermanentFTS.sql` - Complete FTS infrastructure setup
2. Run once after database creation or data import

### Utility Scripts  
- `ClearDatabase.sql` - Clears all data (removes FTS temporarily)
- `RestoreFTS.sql` - Restores FTS after clearing (legacy)

## Benefits of Permanent Setup

✅ **No more manual FTS setup** after data imports
✅ **Consistent search experience** across all environments  
✅ **Automatic search vector maintenance** via triggers
✅ **Entity Framework models always match** database schema
✅ **Production-ready** with proper indexing and optimization

## Troubleshooting

### If Search Results Are Empty
1. Check if materialized view needs refresh:
   ```sql
   REFRESH MATERIALIZED VIEW candidate_search_view;
   ```

2. Verify search vectors are populated:
   ```sql
   SELECT COUNT(*) FROM candidates WHERE search_vector IS NOT NULL;
   ```

### Performance Issues  
1. Check if GIN indexes exist:
   ```sql
   \di *search_vector*
   ```

2. Analyze query performance:
   ```sql
   EXPLAIN ANALYZE SELECT * FROM candidate_search_view 
   WHERE combined_search_vector @@ plainto_tsquery('english', 'search_term');
   ```

## Search Capabilities

The permanent FTS setup provides:

- **Multi-field search** across names, titles, skills, resume content
- **Relevance ranking** with PostgreSQL's advanced scoring
- **Fuzzy matching** with trigram similarity  
- **Instant autocomplete** suggestions
- **Sub-second performance** on large datasets
- **Automatic index maintenance**

This infrastructure ensures that search functionality is always available and performs optimally without manual intervention.