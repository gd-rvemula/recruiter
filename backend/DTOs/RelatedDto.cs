namespace RecruiterApi.DTOs;

public class WorkExperienceDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public int? ExtractedOrder { get; set; }
}

public class EducationDto
{
    public Guid Id { get; set; }
    public string? InstitutionName { get; set; }
    public string? DegreeName { get; set; }
    public string? DegreeType { get; set; }
    public string? FieldOfStudy { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal? GPA { get; set; }
    public string? Location { get; set; }
}

public class CandidateSkillDto
{
    public Guid Id { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? ProficiencyLevel { get; set; }
    public int? YearsOfExperience { get; set; }
    public bool IsExtracted { get; set; }
}

public class ResumeDto
{
    public Guid Id { get; set; }
    public string? FileName { get; set; }
    public string? FileType { get; set; }
    public long? FileSize { get; set; }
    public string? FilePath { get; set; }
    public bool IsProcessed { get; set; }
    public string ProcessingStatus { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}