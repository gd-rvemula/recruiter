namespace RecruiterApi.DTOs;

public class CandidateStatusDto
{
    public Guid CandidateId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public DateTime StatusUpdatedAt { get; set; }
    public string? StatusUpdatedBy { get; set; }
}

public class CandidateStatusHistoryDto
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? ChangedBy { get; set; }
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; }
}

public class CandidateStatusUpdateDto
{
    public string NewStatus { get; set; } = string.Empty;
    public string? ChangeReason { get; set; }
    public string? ChangedBy { get; set; }
}

public class CandidateStatusResponseDto
{
    public string CurrentStatus { get; set; } = string.Empty;
    public DateTime StatusUpdatedAt { get; set; }
    public string? StatusUpdatedBy { get; set; }
    public List<string> AvailableStatuses { get; set; } = new();
}