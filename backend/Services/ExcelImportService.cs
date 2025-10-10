using RecruiterApi.Data;
using RecruiterApi.DTOs;
using RecruiterApi.Models;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Foundatio.Queues;

namespace RecruiterApi.Services;

public class ExcelImportService : IExcelImportService
{
    private readonly RecruiterDbContext _context;
    private readonly ILogger<ExcelImportService> _logger;
    private readonly ISkillExtractionService _skillExtractionService;
    private readonly IQueue<EmbeddingGenerationJob> _embeddingQueue;
    private readonly IPiiSanitizationService _piiSanitizationService;

    public ExcelImportService(
        RecruiterDbContext context, 
        ILogger<ExcelImportService> logger, 
        ISkillExtractionService skillExtractionService,
        IQueue<EmbeddingGenerationJob> embeddingQueue,
        IPiiSanitizationService piiSanitizationService)
    {
        _context = context;
        _logger = logger;
        _skillExtractionService = skillExtractionService;
        _embeddingQueue = embeddingQueue;
        _piiSanitizationService = piiSanitizationService;
    }

    public async Task<ExcelImportResultDto> ImportCandidatesFromExcelAsync(string filePath, bool overwriteExisting = false)
    {
        try
        {
            using var fileStream = File.OpenRead(filePath);
            return await ProcessExcelFileAsync(fileStream, Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing Excel file from path: {FilePath}", filePath);
            return new ExcelImportResultDto
            {
                Success = false,
                Message = $"Error importing Excel file: {ex.Message}",
                ProcessedRows = 0,
                ErrorCount = 1,
                ImportedCandidates = 0,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ExcelImportResultDto> ProcessExcelFileAsync(Stream fileStream, string fileName)
    {
        var result = new ExcelImportResultDto
        {
            Success = false,
            Message = "Processing started",
            ProcessedRows = 0,
            ErrorCount = 0,
            ImportedCandidates = 0,
            Errors = new List<string>()
        };

        try
        {
            _logger.LogInformation("Starting Excel import for file: {FileName}", fileName);

            // Extract requisition name from filename (remove extension and clean up)
            var requisitionName = ExtractRequisitionNameFromFileName(fileName);
            _logger.LogInformation("Using RequisitionName from filename: {RequisitionName}", requisitionName);

            // Create workbook based on file extension
            IWorkbook workbook;
            if (fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                workbook = new XSSFWorkbook(fileStream);
            }
            else if (fileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                workbook = new HSSFWorkbook(fileStream);
            }
            else
            {
                result.Message = "Unsupported file format. Only .xlsx and .xls files are supported.";
                result.Errors.Add("Unsupported file format");
                return result;
            }

            using (workbook)
            {
                var worksheet = workbook.GetSheetAt(0);
                
                if (worksheet == null)
                {
                    result.Message = "No worksheet found in Excel file";
                    result.Errors.Add("Excel file contains no worksheets");
                    return result;
                }

                var rowCount = worksheet.LastRowNum + 1; // NPOI uses 0-based indexing
                if (rowCount <= 1)
                {
                    result.Message = "No data rows found in Excel file";
                    result.Errors.Add("Excel file contains no data rows (only header or empty)");
                    return result;
                }

                _logger.LogInformation("Found {RowCount} rows in Excel file", rowCount);

            // The actual column headers are in row 2 (0-based index 1)
            var headerRow = 2; // Row 2 contains the actual column headers
            var columnMappings = GetColumnMappings(worksheet, headerRow);
            
            _logger.LogInformation("Using header row {HeaderRow} with {MappingCount} mappings", headerRow, columnMappings.Count);
            
            if (!columnMappings.Any())
            {
                result.Message = "No recognizable columns found in Excel file";
                result.Errors.Add("Could not identify any standard candidate columns in the Excel file. Available columns in first few rows logged above.");
                return result;
            }

                // Process data rows
                var candidatesCreated = 0;
                var skillsCreated = new HashSet<string>();

                // NPOI uses 0-based indexing
                // headerRow = 2 means Excel row 2 (0-based index 1) contains headers
                // Data starts at Excel row 3 (0-based index 2)
                // Loop from headerRow to rowCount-1 (inclusive) to process all data rows
                for (int row = headerRow; row < rowCount; row++)
                {
                    try
                    {
                        var candidate = ProcessCandidateRow(worksheet, row + 1, columnMappings, requisitionName); // row+1 is the 1-based Excel row number for display
                        if (candidate != null)
                        {
                            // Check if candidate code already exists (skip duplicates)
                            var existingCandidate = await _context.Candidates
                                .FirstOrDefaultAsync(c => c.CandidateCode == candidate.CandidateCode);
                            
                            if (existingCandidate != null)
                            {
                                _logger.LogInformation("Skipping duplicate candidate: {CandidateCode}", candidate.CandidateCode);
                                result.ProcessedRows++;
                                continue; // Skip this candidate
                            }
                            
                            // Extract resume text from the same row
                            string? resumeText = null;
                            if (columnMappings.ContainsKey("ResumeText"))
                            {
                                resumeText = GetCellValue(worksheet, row + 1, columnMappings["ResumeText"]);
                            }
                            
                            // Extract and create skills using resume text
                            var candidateSkills = await ProcessCandidateSkills(candidate, resumeText, skillsCreated);
                            
                            _context.Candidates.Add(candidate);
                            
                            // Add candidate skills
                            foreach (var skill in candidateSkills)
                            {
                                _context.CandidateSkills.Add(skill);
                            }
                            
                            // Add resumes (only if candidate is not a duplicate)
                            if (candidate.Resumes != null && candidate.Resumes.Any())
                            {
                                foreach (var resume in candidate.Resumes)
                                {
                                    _context.Resumes.Add(resume);
                                }
                            }
                            
                            // Add job applications (only if candidate is not a duplicate)
                            if (candidate.JobApplications != null && candidate.JobApplications.Any())
                            {
                                foreach (var jobApp in candidate.JobApplications)
                                {
                                    _context.JobApplications.Add(jobApp);
                                }
                            }
                            
                            // Save this candidate immediately to catch duplicate key violations early
                            try
                            {
                                await _context.SaveChangesAsync();
                                candidatesCreated++;
                            }
                            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("duplicate key") == true)
                            {
                                // If duplicate key error, log and skip
                                _logger.LogInformation("Skipping duplicate candidate during save: {CandidateCode}", candidate.CandidateCode);
                                // Remove from context to avoid issues
                                _context.Entry(candidate).State = EntityState.Detached;
                                foreach (var skill in candidateSkills)
                                {
                                    _context.Entry(skill).State = EntityState.Detached;
                                }
                                // Also detach resumes if any
                                if (candidate.Resumes != null)
                                {
                                    foreach (var resume in candidate.Resumes)
                                    {
                                        _context.Entry(resume).State = EntityState.Detached;
                                    }
                                }
                                // Also detach job applications if any
                                if (candidate.JobApplications != null)
                                {
                                    foreach (var jobApp in candidate.JobApplications)
                                    {
                                        _context.Entry(jobApp).State = EntityState.Detached;
                                    }
                                }
                            }
                            
                            result.ProcessedRows++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing row {Row}: {Message}", row + 1, ex.Message);
                        result.Errors.Add($"Row {row + 1}: {ex.Message}");
                        result.ErrorCount++;
                    }
                }

                // Queue embedding generation jobs for imported candidates (background task)
                var embeddingJobsQueued = await QueueEmbeddingGenerationJobsAsync(worksheet, headerRow + 1, rowCount, columnMappings);

                result.Success = true;
                result.ImportedCandidates = candidatesCreated;
                result.Message = $"Successfully imported {candidatesCreated} candidates from {result.ProcessedRows} rows. {embeddingJobsQueued} embedding jobs queued.";
                
                _logger.LogInformation(
                    "Excel import completed: {CandidatesCreated} candidates, {SkillsCreated} skills, {EmbeddingJobs} embedding jobs queued", 
                    candidatesCreated, skillsCreated.Count, embeddingJobsQueued);

                return result;
            } // Close the using block for workbook
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Excel file: {FileName}", fileName);
            result.Success = false;
            result.Message = $"Error processing Excel file: {ex.Message}";
            result.Errors.Add(ex.Message);
            return result;
        }
    }

    private Dictionary<string, int> GetColumnMappings(ISheet worksheet, int headerRow)
    {
        var mappings = new Dictionary<string, int>();
        var headerRowObj = worksheet.GetRow(headerRow - 1); // NPOI uses 0-based indexing
        
        if (headerRowObj == null) 
        {
            _logger.LogWarning("Header row is null at row {HeaderRow}", headerRow - 1);
            return mappings;
        }
        
        var columnCount = headerRowObj.LastCellNum;
        _logger.LogInformation("Found {ColumnCount} columns in header row", columnCount);

        for (int col = 0; col < columnCount; col++)
        {
            var cell = headerRowObj.GetCell(col);
            var headerValue = cell?.ToString()?.Trim().ToLower();
            _logger.LogInformation("Column {ColIndex}: '{HeaderValue}'", col, headerValue ?? "NULL");
            
            if (string.IsNullOrEmpty(headerValue)) continue;

            // Map actual column names from the Excel file to our fields
            switch (headerValue)
            {
                // Email mapping
                case "email":
                    mappings["Email"] = col;
                    break;
                    
                // Job titles and current position
                case "current title":
                case "current job title":
                    mappings["CurrentTitle"] = col;
                    break;
                    
                // Address information  
                case "address":
                    mappings["Address"] = col;
                    break;
                    
                // Experience
                case "total years experience":
                    mappings["Experience"] = col;
                    break;
                    
                // Authorization and sponsorship
                case "legally authorized to work in the us?":
                    mappings["IsAuthorizedToWork"] = col;
                    break;
                case "need sponsorship to obtain/maintain work authorization?":
                    mappings["NeedsSponsorship"] = col;
                    break;
                    
                // Salary
                case "salary expectation":
                    mappings["SalaryExpectation"] = col;
                    break;
                    
                // Resume information
                case "resume":
                    mappings["Resume"] = col;
                    break;
                case "resume text":
                    mappings["ResumeText"] = col;
                    break;
                    
                // Job application details
                case "job application":
                    mappings["JobApplication"] = col;
                    break;
                // Note: RequisitionName is now extracted from the filename, not from Excel column
                case "stage":
                    mappings["Stage"] = col;
                    break;
                case "date applied":
                    mappings["DateApplied"] = col;
                    break;
                case "source":
                    mappings["Source"] = col;
                    break;
                    
                // Additional job information
                case "all job titles":
                    mappings["AllJobTitles"] = col;
                    break;
                case "all degrees":
                    mappings["AllDegrees"] = col;
                    break;
                    
                // Reference information
                case "referred by":
                    mappings["ReferredBy"] = col;
                    break;
                    
                // Number of applications
                case "# jobs applied to":
                    mappings["JobsAppliedTo"] = col;
                    break;
            }
        }

        _logger.LogInformation("Column mappings found: {MappingCount} mappings", mappings.Count);
        foreach (var mapping in mappings)
        {
            _logger.LogInformation("Mapped '{Key}' to column {Value}", mapping.Key, mapping.Value);
        }

        return mappings;
    }

    private Candidate? ProcessCandidateRow(ISheet worksheet, int row, Dictionary<string, int> columnMappings, string requisitionName)
    {
        // STEP 1: Extract candidate code from Job Application column FIRST
        // This is the PRIMARY source of candidate identification
        string candidateCode = string.Empty;
        string extractedName = ""; // Only used for resume sanitization
        
        if (columnMappings.ContainsKey("JobApplication"))
        {
            var jobApplicationText = GetCellValue(worksheet, row, columnMappings["JobApplication"]);
            if (!string.IsNullOrWhiteSpace(jobApplicationText))
            {
                // Extract candidate code: "Alex Carter (C123456)" -> "C123456"
                var codeFromJobApp = _piiSanitizationService.ExtractCandidateCodeFromJobApplication(jobApplicationText);
                if (!string.IsNullOrEmpty(codeFromJobApp))
                {
                    candidateCode = codeFromJobApp;
                    _logger.LogInformation("Extracted candidate code from Job Application: {Code}", candidateCode);
                }
                
                // Extract name for PII removal: "Alex Carter (C123456)" -> "Alex Carter"
                var nameFromJobApp = _piiSanitizationService.ExtractCandidateNameFromJobApplication(jobApplicationText);
                if (!string.IsNullOrEmpty(nameFromJobApp))
                {
                    extractedName = nameFromJobApp;
                    _logger.LogInformation("Extracted name for PII sanitization (candidate: {CandidateCode})", candidateCode);
                }
            }
        }
        
        // STEP 2: If no candidate code found, generate one (fallback)
        if (string.IsNullOrEmpty(candidateCode))
        {
            var candidateId = Guid.NewGuid();
            candidateCode = $"C{DateTime.Now:yyyyMMdd}{candidateId.ToString()[0..6]}";
            _logger.LogWarning("No candidate code in Job Application column, generated: {Code}", candidateCode);
        }
        
        // STEP 3: Create candidate with the extracted/generated code
        var candidate = new Candidate
        {
            Id = Guid.NewGuid(),
            CandidateCode = candidateCode,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Extract data based on column mappings
        // PII PROTECTION: We extract email ONLY for sanitization purposes, NOT for storage
        var email = "";
        
        if (columnMappings.ContainsKey("Email"))
        {
            email = GetCellValue(worksheet, row, columnMappings["Email"]);
            // DO NOT STORE EMAIL - PII Protection
            // candidate.Email = email; // REMOVED
        }
        
        // PII PROTECTION: Use anonymized placeholder names based on candidate code
        // Format: "Candidate XXXXXX" where XXXXXX is last 6 chars (or full code if shorter)
        // Example: C123456 -> "Candidate 123456", C20251005abc123 -> "Candidate bc123"
        var lastSixChars = candidate.CandidateCode.Length >= 6 
            ? candidate.CandidateCode.Substring(candidate.CandidateCode.Length - 6) 
            : candidate.CandidateCode.TrimStart('C');
        candidate.FirstName = "Candidate";
        candidate.LastName = lastSixChars;
        candidate.FullName = $"{candidate.FirstName} {lastSixChars}";

        // Map additional fields from the Excel structure
        if (columnMappings.ContainsKey("CurrentTitle"))
        {
            candidate.CurrentTitle = GetCellValue(worksheet, row, columnMappings["CurrentTitle"]);
        }
        
        // Set RequisitionName from filename
        candidate.RequisitionName = requisitionName;
        _logger.LogInformation("Set RequisitionName for candidate {CandidateCode}: {RequisitionName}", 
            candidate.CandidateCode, candidate.RequisitionName);

        // PII PROTECTION: Do not store address information
        // var extractedAddress = ""; // Track for resume sanitization if needed
        // if (columnMappings.ContainsKey("Address"))
        // {
        //     extractedAddress = GetCellValue(worksheet, row, columnMappings["Address"]);
        //     // DO NOT STORE ADDRESS
        // }

        if (columnMappings.ContainsKey("Experience"))
        {
            var experienceText = GetCellValue(worksheet, row, columnMappings["Experience"]);
            var experience = ParseExperience(experienceText);
            candidate.TotalYearsExperience = experience.HasValue ? (int?)Math.Round(experience.Value) : null;
        }

        if (columnMappings.ContainsKey("SalaryExpectation"))
        {
            var salaryText = GetCellValue(worksheet, row, columnMappings["SalaryExpectation"]);
            if (decimal.TryParse(salaryText.Replace("$", "").Replace(",", ""), out var salary))
            {
                candidate.SalaryExpectation = salary;
            }
        }

        if (columnMappings.ContainsKey("IsAuthorizedToWork"))
        {
            var authText = GetCellValue(worksheet, row, columnMappings["IsAuthorizedToWork"]).ToLower();
            candidate.IsAuthorizedToWork = authText.Contains("yes") || authText.Contains("true");
        }

        if (columnMappings.ContainsKey("NeedsSponsorship"))
        {
            var sponsorText = GetCellValue(worksheet, row, columnMappings["NeedsSponsorship"]).ToLower();
            candidate.NeedsSponsorship = sponsorText.Contains("yes") || sponsorText.Contains("true");
        }

        // PII PROTECTION: No name extraction from Excel
        // All candidates now use anonymized "Candidate XXXXXX" format
        // Names are already set above based on candidate code

        // Process resume text if available - SANITIZE PII BEFORE STORING
        if (columnMappings.ContainsKey("ResumeText"))
        {
            var resumeText = GetCellValue(worksheet, row, columnMappings["ResumeText"]);
            if (!string.IsNullOrWhiteSpace(resumeText))
            {
                // PII PROTECTION: Sanitize resume text to remove personal information
                var sanitizedResumeText = _piiSanitizationService.SanitizeResumeText(
                    resumeText,
                    candidateName: extractedName,
                    email: email,
                    address: null // Address not extracted for sanitization
                );

                _logger.LogInformation("Sanitized resume for candidate {CandidateCode}: Original={OriginalLength}, Sanitized={SanitizedLength}",
                    candidate.CandidateCode, resumeText.Length, sanitizedResumeText.Length);

                // Create Resume entity with SANITIZED text
                // NOTE: Resume will be added to context by the caller AFTER confirming candidate isn't duplicate
                var resume = new Resume
                {
                    Id = Guid.NewGuid(),
                    CandidateId = candidate.Id,
                    FileName = "resume_sanitized.txt", // DO NOT store original filename (PII)
                    FileType = "text",
                    ResumeText = sanitizedResumeText, // SANITIZED TEXT ONLY
                    UploadedAt = DateTime.UtcNow,
                    IsProcessed = true, // Mark as processed since we sanitized it
                    ProcessingStatus = "Sanitized"
                };
                
                // Store resume on candidate for caller to add to context later
                candidate.Resumes = new List<Resume> { resume };
            }
        }

        // Process Job Application details (candidate code already extracted at start)
        // Create JobApplication entity if we have stage/source information
        // NOTE: JobApplication will be added to context by the caller AFTER confirming candidate isn't duplicate
        if (columnMappings.ContainsKey("Stage") || columnMappings.ContainsKey("Source") || columnMappings.ContainsKey("DateApplied"))
        {
            var jobApplication = new JobApplication
            {
                Id = Guid.NewGuid(),
                CandidateId = candidate.Id,
                ApplicationDate = DateTime.UtcNow,
                CurrentStage = columnMappings.ContainsKey("Stage") 
                    ? GetCellValue(worksheet, row, columnMappings["Stage"]) 
                    : null,
                Source = columnMappings.ContainsKey("Source") 
                    ? GetCellValue(worksheet, row, columnMappings["Source"]) 
                    : null,
                ReferredBy = candidate.CandidateCode // Reference the candidate's own code
            };

                    // Parse date applied if available
                    if (columnMappings.ContainsKey("DateApplied"))
                    {
                        var dateAppliedText = GetCellValue(worksheet, row, columnMappings["DateApplied"]);
                        if (DateTime.TryParse(dateAppliedText, out var dateApplied))
                        {
                            // Ensure UTC for PostgreSQL compatibility
                            jobApplication.ApplicationDate = DateTime.SpecifyKind(dateApplied, DateTimeKind.Utc);
                        }
                    }

                    // Parse number of jobs applied to if available
                    if (columnMappings.ContainsKey("JobsAppliedTo"))
                    {
                        var jobsAppliedToText = GetCellValue(worksheet, row, columnMappings["JobsAppliedTo"]);
                        if (int.TryParse(jobsAppliedToText, out var jobsAppliedTo))
                        {
                            jobApplication.NumberOfJobsAppliedTo = jobsAppliedTo;
                        }
                    }
                    
                    _logger.LogInformation("Created job application for candidate {CandidateCode}",
                        candidate.CandidateCode);
                    
                    // Store job application on candidate for caller to add to context later
                    candidate.JobApplications = new List<JobApplication> { jobApplication };
        }

        // Set default values - properties already have defaults in the model
        // IsAuthorizedToWork defaults to false, NeedsSponsorship defaults to false

        return candidate;
    }

    private string? TryExtractNameFromRow(ISheet worksheet, int row, HashSet<int> usedColumns)
    {
        var rowObj = worksheet.GetRow(row - 1); // Convert to 0-based indexing
        if (rowObj == null) return null;
        
        var columnCount = rowObj.LastCellNum;
        
        // Look through all columns for something that looks like a name
        for (int col = 0; col < columnCount; col++)
        {
            // Skip columns we've already mapped
            if (usedColumns.Contains(col)) continue;
            
            var cell = rowObj.GetCell(col);
            var cellValue = cell?.ToString()?.Trim();
            
            if (string.IsNullOrEmpty(cellValue)) continue;
            
            // Simple heuristic: if it contains at least one letter and looks like a name
            if (CouldBeName(cellValue))
            {
                return cellValue;
            }
        }
        
        return null;
    }
    
    private bool CouldBeName(string value)
    {
        // Simple name detection heuristics
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2) return false;
        
        // Should contain at least one letter
        if (!value.Any(char.IsLetter)) return false;
        
        // Should not be mostly numbers
        var digitCount = value.Count(char.IsDigit);
        if (digitCount > value.Length / 2) return false;
        
        // Should not contain common non-name patterns
        var lowerValue = value.ToLower();
        var nonNamePatterns = new[] { "@", "http", "www", ".com", "yes", "no", "true", "false" };
        if (nonNamePatterns.Any(pattern => lowerValue.Contains(pattern))) return false;
        
        return true;
    }

    private string CapitalizeFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpper(input[0]) + (input.Length > 1 ? input.Substring(1).ToLower() : "");
    }

    private async Task<List<CandidateSkill>> ProcessCandidateSkills(Candidate candidate, string? resumeText, HashSet<string> skillsCreated)
    {
        try
        {
            // Use the comprehensive skill extraction service if resume text is available
            if (!string.IsNullOrWhiteSpace(resumeText))
            {
                var extractedSkills = await _skillExtractionService.ExtractSkillsFromResumeTextAsync(
                    candidate.Id, 
                    resumeText, 
                    candidate.TotalYearsExperience ?? 0);
                
                _logger.LogInformation("Extracted {SkillCount} skills from resume for candidate {CandidateId}", 
                    extractedSkills.Count, candidate.Id);
                
                return extractedSkills;
            }
            
            // Fallback: Extract skills from job title and basic candidate data
            var skillsFromTitle = await ExtractSkillsFromJobTitle(candidate);
            
            _logger.LogInformation("Extracted {SkillCount} skills from job title for candidate {CandidateId}", 
                skillsFromTitle.Count, candidate.Id);
            
            return skillsFromTitle;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing skills for candidate {CandidateId}", candidate.Id);
            
            // Return basic skills as fallback
            return await GetDefaultSkills(candidate);
        }
    }

    private async Task<List<CandidateSkill>> ExtractSkillsFromJobTitle(Candidate candidate)
    {
        var candidateSkills = new List<CandidateSkill>();
        var jobTitle = candidate.CurrentTitle?.ToLower() ?? "";
        
        // Map job titles to likely skills
        var jobTitleSkillsMap = new Dictionary<string, string[]>
        {
            ["software engineer"] = new[] { "C#", ".NET", "JavaScript", "Python", "Git", "Agile/Scrum", "Unit Testing" },
            ["senior software engineer"] = new[] { "C#", ".NET", "JavaScript", "TypeScript", "React", "Angular", "AWS", "Docker", "Leadership", "Git" },
            ["full stack engineer"] = new[] { "JavaScript", "TypeScript", "React", "Node.js", "PostgreSQL", "HTML5", "CSS3", "Git" },
            ["lead engineer"] = new[] { "Leadership", "Team Management", "Project Management", "C#", ".NET", "JavaScript", "AWS", "Docker" },
            ["java developer"] = new[] { "Java", "Spring Boot", "PostgreSQL", "Git", "Unit Testing", "Agile/Scrum" },
            ["frontend developer"] = new[] { "JavaScript", "TypeScript", "React", "Angular", "HTML5", "CSS3", "Git" },
            ["backend developer"] = new[] { "C#", ".NET", "Python", "PostgreSQL", "Docker", "AWS", "Git" },
            ["devops engineer"] = new[] { "Docker", "Kubernetes", "AWS", "Jenkins", "Terraform", "Git" },
            ["data scientist"] = new[] { "Python", "R", "Machine Learning", "TensorFlow", "Data Analysis", "PostgreSQL" },
            ["product manager"] = new[] { "Product Development", "Project Management", "Agile/Scrum", "Communication", "Strategic Planning" }
        };

        // Find matching skills based on job title
        var matchingSkills = new List<string>();
        foreach (var titleMapping in jobTitleSkillsMap)
        {
            if (jobTitle.Contains(titleMapping.Key))
            {
                matchingSkills.AddRange(titleMapping.Value);
                break; // Use first match
            }
        }

        // If no match, use generic software skills
        if (!matchingSkills.Any())
        {
            matchingSkills = new List<string> { "Problem Solving", "Communication", "Git", "Agile/Scrum" };
        }

        // Create candidate skills from matched skills
        foreach (var skillName in matchingSkills.Distinct())
        {
            var skill = await _context.Skills.FirstOrDefaultAsync(s => s.SkillName == skillName);
            if (skill != null)
            {
                var proficiencyLevel = DetermineProficiencyLevel(candidate.TotalYearsExperience ?? 0, skillName);
                var yearsOfExperience = CalculateYearsOfExperience(candidate.TotalYearsExperience ?? 0, skillName);

                candidateSkills.Add(new CandidateSkill
                {
                    Id = Guid.NewGuid(),
                    CandidateId = candidate.Id,
                    SkillId = skill.Id,
                    ProficiencyLevel = proficiencyLevel,
                    YearsOfExperience = yearsOfExperience,
                    IsExtracted = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        return candidateSkills;
    }

    private async Task<List<CandidateSkill>> GetDefaultSkills(Candidate candidate)
    {
        var candidateSkills = new List<CandidateSkill>();
        var defaultSkills = new[] { "Problem Solving", "Communication", "Git" };
        
        foreach (var skillName in defaultSkills)
        {
            var skill = await _context.Skills.FirstOrDefaultAsync(s => s.SkillName == skillName);
            if (skill != null)
            {
                candidateSkills.Add(new CandidateSkill
                {
                    Id = Guid.NewGuid(),
                    CandidateId = candidate.Id,
                    SkillId = skill.Id,
                    ProficiencyLevel = "Intermediate",
                    YearsOfExperience = Math.Max(1, candidate.TotalYearsExperience ?? 1),
                    IsExtracted = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        return candidateSkills;
    }

    private string DetermineProficiencyLevel(int totalYearsExperience, string skillName)
    {
        var leadershipSkills = new[] { "Leadership", "Team Management", "Project Management", "Strategic Planning" };
        
        if (leadershipSkills.Contains(skillName))
        {
            return totalYearsExperience switch
            {
                >= 12 => "Expert",
                >= 8 => "Advanced",
                >= 5 => "Intermediate",
                _ => "Beginner"
            };
        }

        return totalYearsExperience switch
        {
            >= 10 => "Expert",
            >= 6 => "Advanced",
            >= 3 => "Intermediate",
            _ => "Beginner"
        };
    }

    private int CalculateYearsOfExperience(int totalYearsExperience, string skillName)
    {
        var leadershipSkills = new[] { "Leadership", "Team Management", "Project Management", "Strategic Planning" };
        
        if (leadershipSkills.Contains(skillName))
        {
            return Math.Max(1, Math.Min(totalYearsExperience - 2, totalYearsExperience / 2));
        }

        return Math.Max(1, (int)(totalYearsExperience * 0.8));
    }

    private string GetCellValue(ISheet worksheet, int row, int col)
    {
        var rowObj = worksheet.GetRow(row - 1); // Convert to 0-based indexing
        if (rowObj == null) return "";
        
        var cell = rowObj.GetCell(col);
        if (cell == null) return "";
        
        return cell.ToString()?.Trim() ?? "";
    }

    private decimal? ParseExperience(string experienceText)
    {
        if (string.IsNullOrEmpty(experienceText)) return null;

        // Try to extract numeric value from experience text
        var match = Regex.Match(experienceText, @"(\d+(?:\.\d+)?)");
        if (match.Success && decimal.TryParse(match.Groups[1].Value, out var experience))
        {
            return experience;
        }

        return null;
    }

    public Task<SearchIndexResponse> RefreshSearchIndexAfterImportAsync()
    {
        // This method would refresh the search index after importing data
        // For now, we'll return a simple response
        // In the future, this could call the database function to refresh materialized views
        return Task.FromResult(new SearchIndexResponse
        {
            Success = true,
            Message = "Search index refresh completed",
            ProcessedCandidates = 0
        });
    }

    /// <summary>
    /// Queue embedding generation jobs for recently imported candidates
    /// This is a background task to avoid blocking the import process
    /// </summary>
    private async Task<int> QueueEmbeddingGenerationJobsAsync(
        ISheet worksheet, 
        int startRow, 
        int endRow, 
        Dictionary<string, int> columnMappings)
    {
        int jobsQueued = 0;

        try
        {
            // Get all candidates created in the last minute (just imported)
            var recentCandidates = await _context.Candidates
                .Include(c => c.Resumes)
                .Where(c => c.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
                .ToListAsync();

            _logger.LogInformation("Found {Count} recently imported candidates for embedding generation", recentCandidates.Count);

            foreach (var candidate in recentCandidates)
            {
                try
                {
                    // Get resume text from the most recent resume - THIS IS THE PRIMARY DATA FOR EMBEDDINGS
                    string? resumeText = candidate.Resumes?
                        .OrderByDescending(r => r.UploadedAt)
                        .FirstOrDefault()?.ResumeText;

                    // Skip candidates without resume text - no point in generating embeddings for just names
                    if (string.IsNullOrWhiteSpace(resumeText))
                    {
                        _logger.LogDebug("Skipping candidate {CandidateId} - no resume text available", candidate.Id);
                        continue;
                    }

                    // Use resume text as the primary content for embeddings
                    // Optionally prepend basic candidate info for context
                    var profileText = resumeText;

                    // Add minimal metadata prefix (current title if available for better context)
                    if (!string.IsNullOrWhiteSpace(candidate.CurrentTitle))
                    {
                        profileText = $"{candidate.CurrentTitle}. {resumeText}";
                    }

                    // Limit text to avoid token limits
                    // nomic-embed-text has 8192 token context, ~4 chars per token = ~30k chars safe limit
                    if (profileText.Length > 30000)
                    {
                        _logger.LogDebug("Truncating resume text for candidate {CandidateId} from {Original} to 30000 chars", 
                            candidate.Id, profileText.Length);
                        profileText = profileText.Substring(0, 30000);
                    }

                    // Queue embedding generation job
                    await _embeddingQueue.EnqueueAsync(new EmbeddingGenerationJob
                    {
                        CandidateId = candidate.Id,
                        ProfileText = profileText,
                        ResumeText = resumeText,
                        Source = "ExcelImport"
                    });

                    jobsQueued++;
                    _logger.LogDebug("Queued embedding job for candidate {CandidateId}: {Name}", candidate.Id, candidate.FullName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to queue embedding job for candidate {CandidateId}", candidate.Id);
                }
            }

            _logger.LogInformation("Queued {JobCount} embedding generation jobs", jobsQueued);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing embedding generation jobs");
        }

        return jobsQueued;
    }

    /// <summary>
    /// Extracts a clean requisition name from the Excel filename.
    /// Removes file extension and cleans up special characters.
    /// </summary>
    /// <param name="fileName">The Excel filename (e.g., "Senior-NET-Developer-Remote.xlsx")</param>
    /// <returns>Clean requisition name (e.g., "Senior NET Developer Remote")</returns>
    private string ExtractRequisitionNameFromFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "Unknown Requisition";
        }

        // Remove file extension (.xlsx, .xls)
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        // Replace common separators with spaces (hyphens, underscores, dots)
        var cleanName = nameWithoutExtension
            .Replace('-', ' ')
            .Replace('_', ' ')
            .Replace('.', ' ');

        // Remove multiple spaces and trim
        cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"\s+", " ").Trim();

        // If the result is empty or too short, return the original filename
        if (string.IsNullOrWhiteSpace(cleanName) || cleanName.Length < 3)
        {
            return Path.GetFileNameWithoutExtension(fileName);
        }

        return cleanName;
    }
}
