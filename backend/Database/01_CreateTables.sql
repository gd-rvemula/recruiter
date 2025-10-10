-- =============================================
-- RECRUITER DATABASE SCHEMA - PostgreSQL
-- =============================================

-- Enable UUID extension for PostgreSQL
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =============================================
-- 1. CANDIDATES TABLE (Main Entity)
-- =============================================
CREATE TABLE candidates (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
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
    is_authorized_to_work BOOLEAN NOT NULL DEFAULT false,
    needs_sponsorship BOOLEAN NOT NULL DEFAULT false,
    
    -- Metadata
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT true,
    
    -- Indexes
    CONSTRAINT uk_candidates_email UNIQUE (email),
    CONSTRAINT uk_candidates_candidate_code UNIQUE (candidate_code)
);

-- Create indexes
CREATE INDEX idx_candidates_email ON candidates (email);
CREATE INDEX idx_candidates_candidate_code ON candidates (candidate_code);
CREATE INDEX idx_candidates_full_name ON candidates (full_name);
CREATE INDEX idx_candidates_current_title ON candidates (current_title);

-- =============================================
-- 2. APPLICATION SOURCES TABLE (Lookup)
-- =============================================
CREATE TABLE application_sources (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    source_name VARCHAR(200) NOT NULL UNIQUE,
    source_type VARCHAR(100), -- 'Job Board', 'Agency', 'Referral'
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Insert common sources
INSERT INTO application_sources (source_name, source_type) VALUES
('Job Site Posting -> Indeed', 'Job Board'),
('Job Site Posting -> LinkedIn', 'Job Board'),
('Agency Sourced -> Other', 'Agency'),
('Agency Sourced -> Robert Half', 'Agency'),
('Referral', 'Referral'),
('Direct Application', 'Direct');

-- =============================================
-- 3. APPLICATION STAGES TABLE (Lookup)
-- =============================================
CREATE TABLE application_stages (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    stage_name VARCHAR(100) NOT NULL UNIQUE,
    stage_order INTEGER NOT NULL,
    description VARCHAR(500),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Insert common stages
INSERT INTO application_stages (stage_name, stage_order, description) VALUES
('Applied', 1, 'Initial application received'),
('Review', 2, 'Application under review'),
('Phone Screen', 3, 'Initial phone screening'),
('Technical Interview', 4, 'Technical assessment'),
('Final Interview', 5, 'Final round interview'),
('Offer', 6, 'Offer extended'),
('Hired', 7, 'Candidate hired'),
('Rejected', 8, 'Application rejected');

-- =============================================
-- 4. JOB APPLICATIONS TABLE
-- =============================================
CREATE TABLE job_applications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    candidate_id UUID NOT NULL,
    job_position_id UUID, -- Future: Link to job postings
    
    -- Application Details
    application_date TIMESTAMP WITH TIME ZONE NOT NULL,
    source_id UUID,
    source_text VARCHAR(200), -- Raw source text from Excel
    current_stage_id UUID,
    current_stage_text VARCHAR(100), -- Raw stage text from Excel
    current_status VARCHAR(100),
    candidate_type VARCHAR(100),
    
    -- Referral Info
    referred_by VARCHAR(200),
    
    -- Tracking
    number_of_jobs_applied_to INTEGER DEFAULT 1,
    action_awaiting_me TEXT,
    action_awaiting_others TEXT,
    
    -- Metadata
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE,
    FOREIGN KEY (source_id) REFERENCES application_sources(id),
    FOREIGN KEY (current_stage_id) REFERENCES application_stages(id)
);

-- Create indexes
CREATE INDEX idx_job_applications_candidate_id ON job_applications (candidate_id);
CREATE INDEX idx_job_applications_application_date ON job_applications (application_date);
CREATE INDEX idx_job_applications_current_stage_id ON job_applications (current_stage_id);
CREATE INDEX idx_job_applications_source_id ON job_applications (source_id);

-- =============================================
-- 5. RESUMES TABLE
-- =============================================
CREATE TABLE resumes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    candidate_id UUID NOT NULL,
    
    -- Resume Files
    file_name VARCHAR(255),
    file_size BIGINT,
    file_type VARCHAR(50), -- 'pdf', 'docx'
    file_path VARCHAR(500), -- Storage path/URL
    
    -- Parsed Content
    resume_text TEXT, -- Full extracted text
    resume_text_processed TEXT, -- Cleaned/processed text
    
    -- Metadata
    uploaded_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMP WITH TIME ZONE,
    is_processed BOOLEAN NOT NULL DEFAULT false,
    processing_status VARCHAR(50) DEFAULT 'Pending', -- 'Pending', 'Processed', 'Failed'
    
    FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE
);

CREATE INDEX idx_resumes_candidate_id ON resumes (candidate_id);
CREATE INDEX idx_resumes_processing_status ON resumes (processing_status);

-- =============================================
-- 6. WORK EXPERIENCE TABLE
-- =============================================
CREATE TABLE work_experience (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    candidate_id UUID NOT NULL,
    
    company_name VARCHAR(200) NOT NULL,
    job_title VARCHAR(200) NOT NULL,
    start_date DATE,
    end_date DATE,
    is_current BOOLEAN NOT NULL DEFAULT false,
    location VARCHAR(200),
    description TEXT,
    
    -- Extracted from parsing
    extracted_order INTEGER, -- Order in resume
    
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE
);

CREATE INDEX idx_work_experience_candidate_id ON work_experience (candidate_id);
CREATE INDEX idx_work_experience_company_name ON work_experience (company_name);

-- =============================================
-- 7. EDUCATION TABLE
-- =============================================
CREATE TABLE education (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    candidate_id UUID NOT NULL,
    
    institution_name VARCHAR(200),
    degree_name VARCHAR(200),
    degree_type VARCHAR(100), -- 'Bachelor of Science', 'Master of Science'
    field_of_study VARCHAR(200),
    start_date DATE,
    end_date DATE,
    gpa DECIMAL(3,2),
    location VARCHAR(200),
    
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE
);

CREATE INDEX idx_education_candidate_id ON education (candidate_id);
CREATE INDEX idx_education_institution_name ON education (institution_name);

-- =============================================
-- 8. SKILLS TABLE
-- =============================================
CREATE TABLE skills (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    skill_name VARCHAR(100) NOT NULL UNIQUE,
    category VARCHAR(100), -- 'Programming', 'Framework', 'Database', 'Cloud'
    
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_skills_skill_name ON skills (skill_name);
CREATE INDEX idx_skills_category ON skills (category);

-- =============================================
-- 9. CANDIDATE SKILLS TABLE (Many-to-Many)
-- =============================================
CREATE TABLE candidate_skills (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    candidate_id UUID NOT NULL,
    skill_id UUID NOT NULL,
    
    proficiency_level VARCHAR(50), -- 'Beginner', 'Intermediate', 'Expert'
    years_of_experience INTEGER,
    is_extracted BOOLEAN NOT NULL DEFAULT true, -- Extracted from resume vs manually added
    
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE,
    FOREIGN KEY (skill_id) REFERENCES skills(id) ON DELETE CASCADE,
    UNIQUE (candidate_id, skill_id)
);

CREATE INDEX idx_candidate_skills_candidate_id ON candidate_skills (candidate_id);
CREATE INDEX idx_candidate_skills_skill_id ON candidate_skills (skill_id);

-- =============================================
-- 10. CANDIDATE NOTES TABLE
-- =============================================
CREATE TABLE candidate_notes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    candidate_id UUID NOT NULL,
    
    note_text TEXT NOT NULL,
    note_type VARCHAR(50), -- 'Interview', 'Phone Screen', 'General'
    is_private BOOLEAN NOT NULL DEFAULT false,
    
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100) NOT NULL,
    
    FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE CASCADE
);

CREATE INDEX idx_candidate_notes_candidate_id ON candidate_notes (candidate_id);

-- =============================================
-- 11. FILE UPLOADS TABLE
-- =============================================
CREATE TABLE file_uploads (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    candidate_id UUID,
    
    file_name VARCHAR(255) NOT NULL,
    original_file_name VARCHAR(255) NOT NULL,
    file_size BIGINT NOT NULL,
    content_type VARCHAR(100),
    file_path VARCHAR(500) NOT NULL,
    file_type VARCHAR(50), -- 'Resume', 'CoverLetter', 'Portfolio'
    
    uploaded_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    uploaded_by VARCHAR(100),
    
    FOREIGN KEY (candidate_id) REFERENCES candidates(id) ON DELETE SET NULL
);

CREATE INDEX idx_file_uploads_candidate_id ON file_uploads (candidate_id);

-- =============================================
-- FULL TEXT SEARCH SETUP
-- =============================================

-- Add full-text search indexes for PostgreSQL
CREATE INDEX idx_candidates_full_name_fts ON candidates USING gin(to_tsvector('english', full_name));
CREATE INDEX idx_candidates_current_title_fts ON candidates USING gin(to_tsvector('english', current_title));
CREATE INDEX idx_resumes_text_fts ON resumes USING gin(to_tsvector('english', resume_text));

-- =============================================
-- HELPER FUNCTIONS
-- =============================================

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers for updated_at
CREATE TRIGGER update_candidates_updated_at BEFORE UPDATE ON candidates FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_job_applications_updated_at BEFORE UPDATE ON job_applications FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================
-- INITIAL DATA POPULATION
-- =============================================

-- Insert common skills
INSERT INTO skills (skill_name, category) VALUES
-- Programming Languages
('C#', 'Programming'),
('Java', 'Programming'),
('Python', 'Programming'),
('JavaScript', 'Programming'),
('TypeScript', 'Programming'),
('SQL', 'Programming'),
('T-SQL', 'Programming'),
('HTML', 'Programming'),
('CSS', 'Programming'),
('Golang', 'Programming'),

-- Frameworks
('.NET Core', 'Framework'),
('ASP.NET Core', 'Framework'),
('Entity Framework', 'Framework'),
('React', 'Framework'),
('Angular', 'Framework'),
('Node.js', 'Framework'),
('Spring Boot', 'Framework'),
('Blazor', 'Framework'),

-- Databases
('SQL Server', 'Database'),
('PostgreSQL', 'Database'),
('MongoDB', 'Database'),
('Oracle', 'Database'),
('MySQL', 'Database'),
('Redis', 'Database'),

-- Cloud Technologies
('AWS', 'Cloud'),
('Azure', 'Cloud'),
('Google Cloud', 'Cloud'),
('Docker', 'Cloud'),
('Kubernetes', 'Cloud'),

-- Tools
('Git', 'Tool'),
('Jenkins', 'Tool'),
('Azure DevOps', 'Tool'),
('Jira', 'Tool'),
('Visual Studio', 'Tool');

-- Create summary view for reporting
CREATE VIEW candidate_summary AS
SELECT 
    c.id,
    c.candidate_code,
    c.full_name,
    c.email,
    c.current_title,
    c.total_years_experience,
    c.salary_expectation,
    c.is_authorized_to_work,
    c.needs_sponsorship,
    ja.application_date,
    ja.current_stage_text,
    ja.current_status,
    aps.source_name,
    COUNT(DISTINCT cs.skill_id) as total_skills,
    COUNT(DISTINCT we.id) as total_work_experience,
    COUNT(DISTINCT e.id) as total_education
FROM candidates c
LEFT JOIN job_applications ja ON c.id = ja.candidate_id
LEFT JOIN application_sources aps ON ja.source_id = aps.id
LEFT JOIN candidate_skills cs ON c.id = cs.candidate_id
LEFT JOIN work_experience we ON c.id = we.candidate_id
LEFT JOIN education e ON c.id = e.candidate_id
GROUP BY c.id, c.candidate_code, c.full_name, c.email, c.current_title, 
         c.total_years_experience, c.salary_expectation, c.is_authorized_to_work,
         c.needs_sponsorship, ja.application_date, ja.current_stage_text, 
         ja.current_status, aps.source_name;