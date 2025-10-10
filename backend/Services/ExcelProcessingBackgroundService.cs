namespace RecruiterApi.Services;

public class ExcelProcessingBackgroundService : BackgroundService
{
    private readonly ILogger<ExcelProcessingBackgroundService> _logger;

    public ExcelProcessingBackgroundService(ILogger<ExcelProcessingBackgroundService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Excel Processing Background Service started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            // Background service temporarily disabled for model updates
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
        
        _logger.LogInformation("Excel Processing Background Service stopped");
    }
}
