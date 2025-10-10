-- Add Additional Skills Migration
-- Date: September 29, 2025
-- Description: Adding 8 new skills including cloud platforms, testing methodologies, and AI tools

-- Insert new skills into the skills table
INSERT INTO skills (skill_name, created_at, updated_at) VALUES
('AKS', NOW(), NOW()),
('EKS', NOW(), NOW()),
('TDD', NOW(), NOW()),
('BDD', NOW(), NOW()),
('Copilot', NOW(), NOW()),
('Claude', NOW(), NOW()),
('Agentic AI', NOW(), NOW()),
('AI Agents', NOW(), NOW())
ON CONFLICT (skill_name) DO NOTHING;

-- Verify the insertion
SELECT skill_name FROM skills ORDER BY skill_name;