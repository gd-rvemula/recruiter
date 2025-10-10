-- =====================================================
-- Create client_config table with multi-tenancy support
-- =====================================================
-- Purpose: Store system preferences per client (tenant)
-- Multi-Tenancy: Uses client_id field for tenant isolation
-- Default: 'GLOBAL' for system-wide settings
-- =====================================================

CREATE TABLE IF NOT EXISTS client_config (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id VARCHAR(50) NOT NULL DEFAULT 'GLOBAL',
    config_key VARCHAR(100) NOT NULL,
    config_value TEXT NOT NULL,
    config_type VARCHAR(50) NOT NULL DEFAULT 'string', -- 'string', 'integer', 'boolean', 'json'
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Unique constraint: One config_key per client
    CONSTRAINT uk_client_config_client_key UNIQUE (client_id, config_key)
);

-- Create indexes for faster lookups
CREATE INDEX idx_client_config_client_id ON client_config(client_id);
CREATE INDEX idx_client_config_key ON client_config(config_key);
CREATE INDEX idx_client_config_client_key ON client_config(client_id, config_key);

-- =====================================================
-- Insert default GLOBAL configurations
-- =====================================================

-- Search Scoring Strategy (main configuration)
INSERT INTO client_config (client_id, config_key, config_value, config_type, description)
VALUES (
    'GLOBAL',
    'search.scoring_strategy',
    'option1',
    'string',
    'Search scoring algorithm: option1 (All-or-Nothing) or option4 (Tiered Multi-Keyword)'
) ON CONFLICT (client_id, config_key) DO NOTHING;

-- Semantic Search Weight
INSERT INTO client_config (client_id, config_key, config_value, config_type, description)
VALUES (
    'GLOBAL',
    'search.semantic_weight',
    '0.6',
    'string',
    'Weight for semantic score in hybrid search (0.0-1.0)'
) ON CONFLICT (client_id, config_key) DO NOTHING;

-- Keyword Search Weight
INSERT INTO client_config (client_id, config_key, config_value, config_type, description)
VALUES (
    'GLOBAL',
    'search.keyword_weight',
    '0.4',
    'string',
    'Weight for keyword score in hybrid search (0.0-1.0)'
) ON CONFLICT (client_id, config_key) DO NOTHING;

-- Similarity Threshold
INSERT INTO client_config (client_id, config_key, config_value, config_type, description)
VALUES (
    'GLOBAL',
    'search.similarity_threshold',
    '0.3',
    'string',
    'Minimum similarity threshold for results (0.0-1.0)'
) ON CONFLICT (client_id, config_key) DO NOTHING;

-- =====================================================
-- Add comments for documentation
-- =====================================================

COMMENT ON TABLE client_config IS 'Multi-tenant system configuration and user preferences';
COMMENT ON COLUMN client_config.client_id IS 'Tenant identifier (GLOBAL for system-wide settings)';
COMMENT ON COLUMN client_config.config_key IS 'Configuration key (e.g., search.scoring_strategy)';
COMMENT ON COLUMN client_config.config_value IS 'Configuration value stored as text';
COMMENT ON COLUMN client_config.config_type IS 'Data type: string, integer, boolean, json';
COMMENT ON COLUMN client_config.description IS 'Human-readable description of the configuration';

-- =====================================================
-- Verify installation
-- =====================================================

SELECT 
    client_id,
    config_key,
    config_value,
    config_type,
    description
FROM client_config
WHERE client_id = 'GLOBAL'
ORDER BY config_key;
