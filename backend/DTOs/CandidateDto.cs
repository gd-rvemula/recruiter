namespace RecruiterApi.DTOs;

public class CandidateDto
{
    public Guid Id { get; set; }
    public string CandidateCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? CurrentTitle { get; set; }
    public int? TotalYearsExperience { get; set; }
    public decimal? SalaryExpectation { get; set; }
    public bool IsAuthorizedToWork { get; set; }
    public bool NeedsSponsorship { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsActive { get; set; }
    public string CurrentStatus { get; set; } = "New";
    public DateTime StatusUpdatedAt { get; set; }
    public string? StatusUpdatedBy { get; set; }
    
    // Related data
    public List<WorkExperienceDto> WorkExperiences { get; set; } = new();
    public List<EducationDto> Education { get; set; } = new();
    public List<CandidateSkillDto> Skills { get; set; } = new();
    public List<ResumeDto> Resumes { get; set; } = new();
    public List<CandidateStatusHistoryDto> StatusHistory { get; set; } = new();
}

public class CandidateSearchDto
{
    public Guid Id { get; set; }
    public string CandidateCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? CurrentTitle { get; set; }
    public string? RequisitionName { get; set; }
    public int? TotalYearsExperience { get; set; }
    public decimal? SalaryExpectation { get; set; }
    public bool IsAuthorizedToWork { get; set; }
    public bool NeedsSponsorship { get; set; }
    public bool IsActive { get; set; }
    public string CurrentStatus { get; set; } = "New";
    public List<string> PrimarySkills { get; set; } = new();
    
    // Semantic search properties
    public double? SimilarityScore { get; set; }  // For semantic/hybrid search ranking
    public string? EmbeddingModel { get; set; }   // Model used for embedding
}

public class CandidateCreateDto
{
    public string CandidateCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? CurrentTitle { get; set; }
    public int? TotalYearsExperience { get; set; }
    public decimal? SalaryExpectation { get; set; }
    public bool IsAuthorizedToWork { get; set; }
    public bool NeedsSponsorship { get; set; }
    public string? CreatedBy { get; set; }
}

public class CandidateUpdateDto : CandidateCreateDto
{
    public bool IsActive { get; set; } = true;
}