-- =============================================
-- RECRUITER DATABASE SCHEMA
-- PostgreSQL Version
-- =============================================

-- Create database (run this separately if needed)
-- CREATE DATABASE recruitingdb;

-- =============================================
-- CANDIDATES TABLE (Main Entity)
-- =============================================
CREATE TABLE candidates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    candidate_code VARCHAR(50) NOT NULL UNIQUE,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    full_name VARCHAR(200) NOT NULL,
    email VARCHAR(200) NOT NULL,
    phone VARCHAR(50),
    address TEXT,
    city VARCHAR(100),
    state VARCHAR(100),
    country VARCHAR(100),
    
    -- Professional Info
    current_title VARCHAR(200),
    total_years_experience INTEGER,
    salary_expectation DECIMAL(10,2),
    
    -- Legal Status
    is_authorized_to_work BOOLEAN NOT NULL DEFAULT FALSE,
    needs_sponsorship BOOLEAN NOT NULL DEFAULT FALSE,
    
    -- Metadata
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- Create indexes for candidates
CREATE INDEX idx_candidates_email ON candidates(email);
CREATE INDEX idx_candidates_candidate_code ON candidates(candidate_code);
CREATE INDEX idx_candidates_full_name ON candidates(full_name);
CREATE INDEX idx_candidates_current_title ON candidates(current_title);

-- =============================================
-- JOB APPLICATIONS TABLE
-- =============================================
CREATE TABLE job_applications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    candidate_id UUID NOT NULL,
    job_position_id UUID, -- Future: Link to job postings
    
    -- Application Details
    application_date TIMESTAMP WITH TIME ZONE NOT NULL,
    source VARCHAR(200), -- "Job Site Posting -> Indeed", "Agency Sourced"
    current_stage VARCHAR(100), -- "Review", "Interview", "Offer"
    current_status VARCHAR(100), -- "Applied", "In Review", "Rejected"
    candidate_type VARCHAR(100), -- "Applied", "External", "Referral" 
    
    -- Referral Info
    referred_by VARCHAR(200),
    
    -- Tracking
    number_of_jobs_applied_to INTEGER DEFAULT 1,
    action_awaiting_me VARCHAR(200),
    action_awaiting_others VARCHAR(200),
    
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_job_applications_candidate FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE
);

-- Create indexes for job applications
CREATE INDEX idx_job_applications_candidate_id ON job_applications(candidate_id);
CREATE INDEX idx_job_applications_application_date ON job_applications(application_date);
CREATE INDEX idx_job_applications_current_stage ON job_applications(current_stage);
CREATE INDEX idx_job_applications_current_status ON job_applications(current_status);

-- =============================================
-- RESUMES TABLE
-- =============================================
CREATE TABLE resumes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    candidate_id UUID NOT NULL,
    
    -- Resume Files
    file_name VARCHAR(255),
    file_size BIGINT,
    file_type VARCHAR(50), -- "pdf", "docx"
    file_path VARCHAR(500), -- Storage path/URL
    
    -- Parsed Content
    resume_text TEXT, -- Full extracted text
    resume_text_processed TEXT, -- Cleaned/processed text
    
    -- Metadata
    uploaded_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMP WITH TIME ZONE,
    is_processed BOOLEAN NOT NULL DEFAULT FALSE,
    processing_status VARCHAR(50) DEFAULT 'Pending', -- "Pending", "Processed", "Failed"
    
    CONSTRAINT fk_resumes_candidate FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE
);

-- Create indexes for resumes
CREATE INDEX idx_resumes_candidate_id ON resumes(candidate_id);
CREATE INDEX idx_resumes_processing_status ON resumes(processing_status);

-- =============================================
-- WORK EXPERIENCE TABLE
-- =============================================
CREATE TABLE work_experience (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    candidate_id UUID NOT NULL,
    
    company_name VARCHAR(200) NOT NULL,
    job_title VARCHAR(200) NOT NULL,
    start_date DATE,
    end_date DATE,
    is_current BOOLEAN NOT NULL DEFAULT FALSE,
    location VARCHAR(200),
    description TEXT,
    
    -- Extracted from parsing
    extracted_order INTEGER, -- Order in resume
    
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_work_experience_candidate FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE
);

-- Create indexes for work experience
CREATE INDEX idx_work_experience_candidate_id ON work_experience(candidate_id);
CREATE INDEX idx_work_experience_company_name ON work_experience(company_name);
CREATE INDEX idx_work_experience_job_title ON work_experience(job_title);

-- =============================================
-- EDUCATION TABLE
-- =============================================
CREATE TABLE education (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    candidate_id UUID NOT NULL,
    
    institution_name VARCHAR(200),
    degree_name VARCHAR(200),
    degree_type VARCHAR(100), -- "Bachelor of Science", "Master of Science"
    field_of_study VARCHAR(200),
    start_date DATE,
    end_date DATE,
    gpa DECIMAL(3,2),
    location VARCHAR(200),
    
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_education_candidate FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE
);

-- Create indexes for education
CREATE INDEX idx_education_candidate_id ON education(candidate_id);
CREATE INDEX idx_education_institution_name ON education(institution_name);
CREATE INDEX idx_education_degree_type ON education(degree_type);

-- =============================================
-- SKILLS TABLE
-- =============================================
CREATE TABLE skills (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    skill_name VARCHAR(100) NOT NULL UNIQUE,
    category VARCHAR(100), -- "Programming", "Framework", "Database", "Cloud"
    
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Create indexes for skills
CREATE INDEX idx_skills_skill_name ON skills(skill_name);
CREATE INDEX idx_skills_category ON skills(category);

-- =============================================
-- CANDIDATE SKILLS TABLE (Many-to-Many Junction)
-- =============================================
CREATE TABLE candidate_skills (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    candidate_id UUID NOT NULL,
    skill_id UUID NOT NULL,
    
    proficiency_level VARCHAR(50), -- "Beginner", "Intermediate", "Expert"
    years_of_experience INTEGER,
    is_extracted BOOLEAN NOT NULL DEFAULT TRUE, -- Extracted from resume vs manually added
    
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_candidate_skills_candidate FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE,
    CONSTRAINT fk_candidate_skills_skill FOREIGN KEY (skill_id) REFERENCES skills(id) ON DELETE CASCADE,
    CONSTRAINT uk_candidate_skills UNIQUE (candidate_id, skill_id)
);

-- Create indexes for candidate skills
CREATE INDEX idx_candidate_skills_candidate_id ON candidate_skills(candidate_id);
CREATE INDEX idx_candidate_skills_skill_id ON candidate_skills(skill_id);

-- =============================================
-- LOOKUP TABLES
-- =============================================
CREATE TABLE application_sources (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    source_name VARCHAR(200) NOT NULL UNIQUE,
    source_type VARCHAR(100), -- "Job Board", "Agency", "Referral"
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE application_stages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    stage_name VARCHAR(100) NOT NULL UNIQUE,
    stage_order INTEGER NOT NULL,
    description VARCHAR(500),
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- =============================================
-- AUDIT/TRACKING TABLES
-- =============================================
CREATE TABLE candidate_notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    candidate_id UUID NOT NULL,
    
    note_text TEXT NOT NULL,
    note_type VARCHAR(50), -- "Interview", "Phone Screen", "General"
    is_private BOOLEAN NOT NULL DEFAULT FALSE,
    
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100) NOT NULL,
    
    CONSTRAINT fk_candidate_notes_candidate FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE
);

CREATE TABLE file_uploads (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    candidate_id UUID,
    
    file_name VARCHAR(255) NOT NULL,
    original_file_name VARCHAR(255) NOT NULL,
    file_size BIGINT NOT NULL,
    content_type VARCHAR(100),
    file_path VARCHAR(500) NOT NULL,
    file_type VARCHAR(50), -- "Resume", "CoverLetter", "Portfolio"
    
    uploaded_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    uploaded_by VARCHAR(100),
    
    CONSTRAINT fk_file_uploads_candidate FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE SET NULL
);

-- =============================================
-- SEED DATA FOR LOOKUP TABLES
-- =============================================
INSERT INTO application_sources (source_name, source_type) VALUES
('Job Site Posting -> Indeed', 'Job Board'),
('Job Site Posting -> LinkedIn', 'Job Board'),
('Agency Sourced -> Other', 'Agency'),
('Direct Application', 'Direct'),
('Employee Referral', 'Referral'),
('Campus Recruitment', 'Campus'),
('Social Media', 'Social');

INSERT INTO application_stages (stage_name, stage_order, description) VALUES
('Applied', 1, 'Application received'),
('Review', 2, 'Application under review'),
('Phone Screen', 3, 'Initial phone screening'),
('Technical Interview', 4, 'Technical assessment'),
('Final Interview', 5, 'Final round interview'),
('Offer', 6, 'Job offer extended'),
('Hired', 7, 'Candidate hired'),
('Rejected', 8, 'Application rejected');

-- =============================================
-- FUNCTIONS AND TRIGGERS
-- =============================================

-- Function to update the updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers for updated_at
CREATE TRIGGER update_candidates_updated_at BEFORE UPDATE ON candidates 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_job_applications_updated_at BEFORE UPDATE ON job_applications 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();