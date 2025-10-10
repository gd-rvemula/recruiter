-- Add Candidate Status Tracking
-- Date: 2025-09-28
-- Description: Add global candidate status and status history tracking

-- Add status columns to candidates table
ALTER TABLE candidates 
ADD COLUMN current_status VARCHAR(50) DEFAULT 'New',
ADD COLUMN status_updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
ADD COLUMN status_updated_by VARCHAR(100);

-- Create index for status queries
CREATE INDEX idx_candidates_current_status ON candidates(current_status);
CREATE INDEX idx_candidates_status_updated_at ON candidates(status_updated_at);

-- Create candidate status history table
CREATE TABLE candidate_status_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    candidate_id UUID NOT NULL REFERENCES candidates(id) ON DELETE CASCADE,
    previous_status VARCHAR(50),
    new_status VARCHAR(50) NOT NULL,
    changed_by VARCHAR(100),
    change_reason TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create indexes for status history
CREATE INDEX idx_status_history_candidate_id ON candidate_status_history(candidate_id);
CREATE INDEX idx_status_history_created_at ON candidate_status_history(created_at);
CREATE INDEX idx_status_history_new_status ON candidate_status_history(new_status);

-- Update existing candidates to have 'New' status if null
UPDATE candidates 
SET current_status = 'New' 
WHERE current_status IS NULL;

-- Create trigger to automatically log status changes
CREATE OR REPLACE FUNCTION log_candidate_status_change()
RETURNS TRIGGER AS $$
BEGIN
    -- Only log if status actually changed
    IF OLD.current_status IS DISTINCT FROM NEW.current_status THEN
        INSERT INTO candidate_status_history (
            candidate_id,
            previous_status,
            new_status,
            changed_by,
            created_at
        ) VALUES (
            NEW.id,
            OLD.current_status,
            NEW.current_status,
            NEW.status_updated_by,
            NEW.status_updated_at
        );
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger for status change logging
CREATE TRIGGER trig_candidate_status_change
    AFTER UPDATE ON candidates
    FOR EACH ROW
    EXECUTE FUNCTION log_candidate_status_change();

-- Add status field to full-text search trigger
CREATE OR REPLACE FUNCTION update_candidate_search_vector()
RETURNS TRIGGER AS $$
BEGIN
    NEW.search_vector := 
        setweight(to_tsvector('english', COALESCE(NEW.first_name, '')), 'A') ||
        setweight(to_tsvector('english', COALESCE(NEW.last_name, '')), 'A') ||
        setweight(to_tsvector('english', COALESCE(NEW.full_name, '')), 'A') ||
        setweight(to_tsvector('english', COALESCE(NEW.email, '')), 'B') ||
        setweight(to_tsvector('english', COALESCE(NEW.current_title, '')), 'B') ||
        setweight(to_tsvector('english', COALESCE(NEW.current_status, '')), 'C');
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

COMMENT ON TABLE candidate_status_history IS 'Tracks all status changes for candidates with full audit trail';
COMMENT ON COLUMN candidates.current_status IS 'Current status: New, Review, Screening, Interviewing, Accepted, Rejected, Withdrawn, OnHold';
COMMENT ON COLUMN candidates.status_updated_at IS 'Timestamp when status was last updated';
COMMENT ON COLUMN candidates.status_updated_by IS 'User who last updated the status';