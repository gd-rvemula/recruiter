namespace RecruiterApi.DTOs;

public class CandidateSearchRequest
{
    public string? SearchTerm { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? CurrentTitle { get; set; }
    public int? MinTotalYearsExperience { get; set; }
    public int? MaxTotalYearsExperience { get; set; }
    public bool? IsAuthorizedToWork { get; set; }
    public bool? NeedsSponsorship { get; set; }
    public string? SponsorshipFilter { get; set; } // "all", "yes", "no"
    public decimal? MinSalaryExpectation { get; set; }
    public decimal? MaxSalaryExpectation { get; set; }
    public bool? IsActive { get; set; }
    public List<string> Skills { get; set; } = new();
    public List<string> Companies { get; set; } = new();
    public List<string> JobTitles { get; set; } = new();
    
    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    
    // Sorting
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc";
}

public class CandidateSearchResponse
{
    public List<CandidateSearchDto> Candidates { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class ExcelImportRequest
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public bool OverwriteExisting { get; set; } = false;
    public Dictionary<string, string> ColumnMappings { get; set; } = new();
}

public class ExcelImportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int SkippedRecords { get; set; }
    public int ErrorRecords { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}