-- =============================================================================
-- PII PROTECTION IMPLEMENTATION
-- Date: October 5, 2025
-- Purpose: Document PII protection measures implemented in the import process
-- =============================================================================

-- SUMMARY OF PII PROTECTION MEASURES:
-- ====================================

-- 1. EMAIL ADDRESSES
--    - NO LONGER STORED in candidates.email column
--    - Extracted during import ONLY for sanitization purposes
--    - Removed from resume text using regex pattern matching

-- 2. ADDRESSES
--    - NO LONGER STORED in candidates.address, city, state columns
--    - Removed from resume text including:
--      * Full addresses
--      * Street addresses (e.g., "123 Main St")
--      * City names
--      * State names
--      * Zip codes (5-digit and ZIP+4 formats)

-- 3. PHONE NUMBERS
--    - Removed from resume text using comprehensive regex patterns
--    - Matches various formats: (555) 123-4567, 555-123-4567, 555.123.4567

-- 4. CANDIDATE NAMES
--    - Names extracted for sanitization purposes only
--    - All occurrences removed from resume text
--    - First name, last name, and full name variations removed

-- 5. RESUME FILENAMES
--    - Original filenames NOT STORED (may contain PII)
--    - Generic filename used: "resume_sanitized.txt"

-- 6. JOB APPLICATION REFERENCES
--    - Format: "Dwight Shrute (C20250928edb5a8)"
--    - ONLY candidate code stored: "C20250928edb5a8"
--    - Name portion removed for PII protection
--    - Stored in job_applications.referred_by column

-- 7. RESUME TEXT SANITIZATION
--    - All PII removed before database storage
--    - Replacements:
--      * Emails       → [EMAIL_REMOVED]
--      * Phones       → [PHONE_REMOVED]
--      * Addresses    → [ADDRESS_REMOVED]
--      * Zip Codes    → [ZIP_REMOVED]
--      * Names        → [NAME_REMOVED]
--    - Processing status marked as "Sanitized"

-- =============================================================================
-- VERIFICATION QUERIES
-- =============================================================================

-- Check that no emails are stored
SELECT COUNT(*) as emails_found
FROM candidates
WHERE email IS NOT NULL AND email != '';

-- Check that no addresses are stored  
SELECT COUNT(*) as addresses_found
FROM candidates
WHERE address IS NOT NULL AND address != '';

-- Check that resumes are marked as sanitized
SELECT 
    processing_status,
    COUNT(*) as count
FROM resumes
GROUP BY processing_status
ORDER BY count DESC;

-- Check job applications store only candidate codes (should match pattern C[0-9a-f]{6,14})
SELECT 
    referred_by,
    COUNT(*) as count
FROM job_applications
WHERE referred_by IS NOT NULL
GROUP BY referred_by
ORDER BY count DESC
LIMIT 20;

-- Verify resume text doesn't contain obvious PII patterns
-- (This is a sample check - manual review recommended for comprehensive validation)
SELECT 
    candidate_id,
    LENGTH(resume_text) as text_length,
    CASE 
        WHEN resume_text ~ '[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}' THEN 'CONTAINS_EMAIL'
        WHEN resume_text ~ '\d{3}[-.\s]?\d{3}[-.\s]?\d{4}' THEN 'CONTAINS_PHONE'
        WHEN resume_text ~ '\b\d{5}(?:-\d{4})?\b' THEN 'CONTAINS_ZIP'
        ELSE 'CLEAN'
    END as pii_check
FROM resumes
WHERE resume_text IS NOT NULL
LIMIT 100;

-- =============================================================================
-- IMPLEMENTATION DETAILS
-- =============================================================================

-- SERVICE: PiiSanitizationService.cs
-- - IPiiSanitizationService interface
-- - Regex-based pattern matching
-- - Comprehensive PII detection and removal

-- UPDATED SERVICE: ExcelImportService.cs
-- - Integrated PiiSanitizationService dependency
-- - Email and address extracted but NOT stored
-- - Resume text sanitized before database insertion
-- - Job application candidate code extraction
-- - Generic filename for all resumes

-- DEPENDENCY INJECTION: Program.cs
-- - Registered IPiiSanitizationService as scoped service

-- =============================================================================
-- REGEX PATTERNS USED
-- =============================================================================

-- Email: \b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b
-- Phone: Multiple patterns for US phone numbers
-- Zip Code: \b\d{5}(?:-\d{4})?\b
-- Street Address: \b\d+\s+[A-Za-z\s]+(Street|St|Avenue|Ave|Road|...)...
-- Candidate Code: \(C[0-9a-fA-F]{6,14}\)

-- =============================================================================
-- TESTING RECOMMENDATIONS
-- =============================================================================

-- 1. Import test data with known PII
-- 2. Verify email column is NULL/empty for all new imports
-- 3. Verify address columns are NULL/empty for all new imports
-- 4. Check resume_text for [EMAIL_REMOVED], [PHONE_REMOVED] markers
-- 5. Verify job_applications.referred_by contains only candidate codes
-- 6. Confirm processing_status = 'Sanitized' for all new resumes
-- 7. Manual review of sample resume text for any missed PII

-- =============================================================================
-- COMPLIANCE NOTES
-- =============================================================================

-- This implementation supports:
-- - GDPR compliance (minimize PII storage)
-- - CCPA compliance (data minimization)
-- - General data protection best practices
-- - Privacy-by-design principles

-- Limitations:
-- - Name sanitization is heuristic-based (may miss variations)
-- - Address sanitization may miss non-standard formats
-- - Manual review recommended for critical use cases
-- - Consider additional scanning tools for comprehensive PII detection

-- =============================================================================
-- END OF DOCUMENTATION
-- =============================================================================
