using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Npgsql;
using RecruiterApi.Data;
using RecruiterApi.Services;
using RecruiterApi.Models;
using Foundatio.Queues;
using Foundatio.Caching;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework
builder.Services.AddDbContext<RecruiterDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HTTP client factory for embedding services
builder.Services.AddHttpClient();

// Add Foundatio services for async processing
builder.Services.AddSingleton<IQueue<EmbeddingGenerationJob>>(provider => 
    new InMemoryQueue<EmbeddingGenerationJob>(new InMemoryQueueOptions<EmbeddingGenerationJob> 
    { 
        Retries = 3,
        RetryDelay = TimeSpan.FromSeconds(30)
    })
);

builder.Services.AddSingleton<ICacheClient>(provider => 
    new InMemoryCacheClient(new InMemoryCacheClientOptions())
);

// Add embedding service based on configuration
var embeddingProvider = builder.Configuration["Embedding:Provider"] ?? "Ollama";
Log.Information("Configuring embedding service: {Provider}", embeddingProvider);

if (embeddingProvider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IEmbeddingService, AzureOpenAIEmbeddingService>();
    Log.Information("Registered Azure OpenAI Embedding Service");
}
else
{
    builder.Services.AddScoped<IEmbeddingService, OllamaEmbeddingService>();
    Log.Information("Registered Ollama Embedding Service");
}

// Add application services
builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
builder.Services.AddScoped<IFullTextSearchService, FullTextSearchService>();
builder.Services.AddScoped<ISkillExtractionService, SkillExtractionService>();
builder.Services.AddScoped<IPiiSanitizationService, PiiSanitizationService>();
builder.Services.AddScoped<SemanticSearchService>();
builder.Services.AddScoped<IClientConfigService, ClientConfigService>();
builder.Services.AddScoped<IAISummaryService, AISummaryService>();

// Add background services
builder.Services.AddSingleton<ExcelProcessingBackgroundService>();
builder.Services.AddHostedService<ExcelProcessingBackgroundService>(provider => 
    provider.GetRequiredService<ExcelProcessingBackgroundService>());

builder.Services.AddHostedService<EmbeddingGenerationBackgroundService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger in all environments for development
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Root endpoint
app.MapGet("/", () => "Recruiter API is running! Visit /swagger for API documentation.");

// Health check endpoint
app.MapHealthChecks("/health");

// Initialize database
await InitializeDatabaseAsync(app.Services, builder.Configuration);

app.Run();

static async Task InitializeDatabaseAsync(IServiceProvider services, IConfiguration configuration)
{
    try
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            Log.Warning("No database connection string found. Skipping database initialization.");
            return;
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;
        builder.Database = "postgres";

        using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        var checkDbCommand = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @databaseName",
            connection);
        checkDbCommand.Parameters.AddWithValue("@databaseName", databaseName ?? "recruitingdb");

        var exists = await checkDbCommand.ExecuteScalarAsync();

        if (exists == null)
        {
            var createDbCommand = new NpgsqlCommand(
                $"CREATE DATABASE \"{databaseName}\"",
                connection);
            await createDbCommand.ExecuteNonQueryAsync();
            Log.Information("Database {DatabaseName} created successfully", databaseName);
        }
        else
        {
            Log.Information("Database {DatabaseName} already exists", databaseName);
        }

        // Initialize Full-Text Search infrastructure if it doesn't exist
        await InitializeFullTextSearchAsync(connectionString);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error initializing database");
    }
}

static async Task InitializeFullTextSearchAsync(string? connectionString)
{
    try
    {
        if (string.IsNullOrEmpty(connectionString)) return;

        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Check if FTS infrastructure exists
        var checkFtsCommand = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'candidates' AND column_name = 'search_vector')",
            connection);
        
        var ftsExists = (bool)(await checkFtsCommand.ExecuteScalarAsync() ?? false);

        if (!ftsExists)
        {
            Log.Information("Initializing Full-Text Search infrastructure...");
            
            // Enable pg_trgm extension
            var enableExtensionCommand = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS pg_trgm", connection);
            await enableExtensionCommand.ExecuteNonQueryAsync();

            // Add search vector columns
            var alterCandidatesCommand = new NpgsqlCommand(
                "ALTER TABLE candidates ADD COLUMN IF NOT EXISTS search_vector tsvector", connection);
            await alterCandidatesCommand.ExecuteNonQueryAsync();

            var alterResumesCommand = new NpgsqlCommand(
                "ALTER TABLE resumes ADD COLUMN IF NOT EXISTS search_vector tsvector", connection);
            await alterResumesCommand.ExecuteNonQueryAsync();

            var alterSkillsCommand = new NpgsqlCommand(
                "ALTER TABLE skills ADD COLUMN IF NOT EXISTS search_vector tsvector", connection);
            await alterSkillsCommand.ExecuteNonQueryAsync();

            Log.Information("Full-Text Search infrastructure initialized successfully");
        }
        else
        {
            Log.Information("Full-Text Search infrastructure already exists");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error initializing database");
    }
}
