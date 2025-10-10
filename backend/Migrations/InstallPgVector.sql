-- Install pgvector Extension for Semantic Search
-- Date: October 4, 2025
-- Description: Enable vector similarity search for AI embeddings

-- Install the pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Verify installation
SELECT * FROM pg_available_extensions WHERE name = 'vector';

-- Show vector capabilities
SELECT 'pgvector installed successfully!' as status;