# Recruiter API

A .NET 8 Web API application built with Foundatio library for distributed systems capabilities.

## Features

- Health check endpoints
- PostgreSQL database integration
- Docker containerization
- Foundatio library integration for:
  - Caching
  - Queuing
  - Storage
  - Messaging
  - Locks
  - Metrics
  - Jobs

## Prerequisites

- Docker and Docker Compose
- .NET 8 SDK (for local development)

## Running with Docker

1. Build and run the application with PostgreSQL:
   ```bash
   docker-compose up --build
   ```

2. The API will be available at:
   - HTTP: http://localhost:8080
   - Health Check: http://localhost:8080/health
   - API Health Check: http://localhost:8080/api/health
   - Swagger UI: http://localhost:8080/swagger

## Database

The application automatically creates a PostgreSQL database named `recruitingdb` on startup if it doesn't exist.

## Configuration

The application uses the following configuration:

- **Development**: Connects to PostgreSQL on localhost:5433
- **Docker**: Connects to PostgreSQL container on postgres:5432

## API Endpoints

### Health Check
- `GET /health` - Basic health check
- `GET /api/health` - Detailed health check with database status

## Foundatio Integration

This application leverages [Foundatio](https://github.com/FoundatioFx/Foundatio) - a pluggable foundation library for building distributed applications. Foundatio provides abstractions for common distributed systems patterns.

### Available Foundatio Services

- **Caching**: In-memory caching with `ICache` interface
- **Queuing**: In-memory queuing with `IQueue<T>` interface  
- **Storage**: In-memory file storage with `IFileStorage` interface
- **Messaging**: In-memory messaging with `IMessageBus` interface
- **Metrics**: In-memory metrics collection with `IMetricsClient` interface
- **Jobs**: Job processing capabilities with `IJob` interface

### Using Foundatio Services

The application automatically registers Foundatio services via `builder.Services.AddFoundatio()`. You can inject and use these services in your controllers or services:

#### Caching Example
```csharp
public class ExampleController : ControllerBase
{
    private readonly ICache _cache;
    
    public ExampleController(ICache cache)
    {
        _cache = cache;
    }
    
    [HttpGet("cached-data")]
    public async Task<IActionResult> GetCachedData()
    {
        var cacheKey = "example-data";
        var data = await _cache.GetAsync<string>(cacheKey);
        
        if (data.HasValue)
        {
            return Ok(data.Value);
        }
        
        // Simulate expensive operation
        var result = await ExpensiveOperation();
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
        
        return Ok(result);
    }
}
```

#### Queue Processing Example
```csharp
public class EmailQueueJob : QueueJobBase<EmailMessage>
{
    public EmailQueueJob(IQueue<EmailMessage> queue) : base(queue) { }
    
    protected override async Task<JobResult> ProcessQueueEntryAsync(QueueEntryContext<EmailMessage> context)
    {
        var email = context.QueueEntry.Value;
        
        // Process email sending logic
        await SendEmailAsync(email);
        
        return JobResult.Success;
    }
}
```

#### File Storage Example
```csharp
public class DocumentService
{
    private readonly IFileStorage _fileStorage;
    
    public DocumentService(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage;
    }
    
    public async Task<string> SaveDocumentAsync(byte[] content, string fileName)
    {
        var filePath = $"documents/{Guid.NewGuid()}/{fileName}";
        await _fileStorage.SaveFileAsync(filePath, content);
        return filePath;
    }
}
```

#### Messaging Example
```csharp
public class NotificationService
{
    private readonly IMessageBus _messageBus;
    
    public NotificationService(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }
    
    public async Task PublishNotificationAsync(NotificationMessage message)
    {
        await _messageBus.PublishAsync(message);
    }
}
```

### Configuration

Foundatio services are configured in `appsettings.json`:

```json
{
  "Foundatio": {
    "Cache": {
      "Provider": "InMemory"
    },
    "Queue": {
      "Provider": "InMemory"
    },
    "Storage": {
      "Provider": "InMemory"
    },
    "Messaging": {
      "Provider": "InMemory"
    },
    "Metrics": {
      "Provider": "InMemory"
    }
  }
}
```

### Production Considerations

For production environments, consider replacing in-memory providers with:

- **Redis** for caching, queuing, and messaging
- **Azure Blob Storage** or **AWS S3** for file storage
- **Application Insights** or **StatsD** for metrics
- **Redis** or **SQL Server** for distributed locks

### Foundatio Benefits

- **Abstraction**: Switch between providers without changing business logic
- **Resilience**: Built-in retry policies and circuit breakers
- **Testing**: Easy to mock and test with in-memory providers
- **Scalability**: Designed for distributed systems from the ground up

## Development

For local development without Docker:

1. Ensure PostgreSQL is running on port 5433
2. Run the application:
   ```bash
   dotnet run
   ```

## Environment Variables

- `ASPNETCORE_ENVIRONMENT`: Set to "Development" for local development
- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string
