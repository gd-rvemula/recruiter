namespace RecruiterApi.DTOs;

public class ExcelImportResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ProcessedRows { get; set; }
    public int ErrorCount { get; set; }
    public int ImportedCandidates { get; set; }
    public List<string> Errors { get; set; } = new();
}
