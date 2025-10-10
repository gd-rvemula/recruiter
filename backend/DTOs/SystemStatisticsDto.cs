namespace RecruiterApi.DTOs;

public class SystemStatisticsDto
{
    public int TotalCandidates { get; set; }
    public int WithEmbeddings { get; set; }
    public double CoveragePercent { get; set; }
}
