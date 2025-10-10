-- Add missing description column to skills table
-- This is needed for Excel import functionality

ALTER TABLE skills ADD COLUMN IF NOT EXISTS description text;

-- Update any existing skills to have empty description if needed
UPDATE skills SET description = '' WHERE description IS NULL;

-- Show updated table structure
\d skills;