using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RecruiterApi.Data;
using RecruiterApi.DTOs;
using RecruiterApi.Models;
using RecruiterApi.Services;
using Microsoft.EntityFrameworkCore;
using Foundatio.Queues;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace RecruiterApi.Tests.Services;

public class ExcelImportServiceTests
{
    private readonly Mock<ILogger<ExcelImportService>> _mockLogger;
    private readonly Mock<ISkillExtractionService> _mockSkillExtraction;
    private readonly Mock<IQueue<EmbeddingGenerationJob>> _mockEmbeddingQueue;
    private readonly Mock<IPiiSanitizationService> _mockPiiSanitization;

    public ExcelImportServiceTests()
    {
        _mockLogger = new Mock<ILogger<ExcelImportService>>();
        _mockSkillExtraction = new Mock<ISkillExtractionService>();
        _mockEmbeddingQueue = new Mock<IQueue<EmbeddingGenerationJob>>();
        _mockPiiSanitization = new Mock<IPiiSanitizationService>();
        
        // Setup default PII sanitization behavior
        _mockPiiSanitization
            .Setup(x => x.SanitizeResumeText(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string text, string name, string email, string address) => text);
    }

    private RecruiterDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RecruiterDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new RecruiterDbContext(options);
    }

    private Stream CreateTestExcelFile(bool withValidData = true)
    {
        var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet("Candidates");

        // Create header row with exact names expected by ExcelImportService
        var headerRow = sheet.CreateRow(0);
        headerRow.CreateCell(0).SetCellValue("Job Application");
        headerRow.CreateCell(1).SetCellValue("Current Title");
        headerRow.CreateCell(2).SetCellValue("Requisition Name");
        headerRow.CreateCell(3).SetCellValue("Total Years Experience");
        headerRow.CreateCell(4).SetCellValue("Email");
        headerRow.CreateCell(5).SetCellValue("Legally Authorized to work in the US?");
        headerRow.CreateCell(6).SetCellValue("Need sponsorship to obtain/maintain work authorization?");
        headerRow.CreateCell(7).SetCellValue("Resume Text");
        headerRow.CreateCell(8).SetCellValue("Salary Expectation");

        if (withValidData)
        {
            // Create data row
            var dataRow = sheet.CreateRow(1);
            dataRow.CreateCell(0).SetCellValue("John Doe (C123456)");
            dataRow.CreateCell(1).SetCellValue("Senior Developer");
            dataRow.CreateCell(2).SetCellValue("Senior .NET Developer - Remote");
            dataRow.CreateCell(3).SetCellValue("5");
            dataRow.CreateCell(4).SetCellValue("john.doe@example.com");
            dataRow.CreateCell(5).SetCellValue("Yes");
            dataRow.CreateCell(6).SetCellValue("No");
            dataRow.CreateCell(7).SetCellValue("Experienced developer with C# and .NET skills.");
            dataRow.CreateCell(8).SetCellValue("120000");
        }

        var stream = new MemoryStream();
        workbook.Write(stream, true);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task ProcessExcelFileAsync_WithValidData_ShouldReturnResult()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ExcelImportService(
            context,
            _mockLogger.Object,
            _mockSkillExtraction.Object,
            _mockEmbeddingQueue.Object,
            _mockPiiSanitization.Object
        );

        using var stream = CreateTestExcelFile(withValidData: true);

        // Act
        var result = await service.ProcessExcelFileAsync(stream, "test.xlsx");

        // Assert - Should complete without throwing exception
        result.Should().NotBeNull();
        result.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessExcelFileAsync_WithDuplicateCandidates_ShouldCompleteWithoutException()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ExcelImportService(
            context,
            _mockLogger.Object,
            _mockSkillExtraction.Object,
            _mockEmbeddingQueue.Object,
            _mockPiiSanitization.Object
        );

        using var stream1 = CreateTestExcelFile(withValidData: true);
        using var stream2 = CreateTestExcelFile(withValidData: true);

        // Act - Import same data twice
        var result1 = await service.ProcessExcelFileAsync(stream1, "test1.xlsx");
        var result2 = await service.ProcessExcelFileAsync(stream2, "test2.xlsx");

        // Assert - Both imports should complete without throwing exceptions
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessExcelFileAsync_WithInvalidFormat_ShouldReturnError()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ExcelImportService(
            context,
            _mockLogger.Object,
            _mockSkillExtraction.Object,
            _mockEmbeddingQueue.Object,
            _mockPiiSanitization.Object
        );

        using var stream = new MemoryStream();
        stream.Write(new byte[] { 0x00, 0x01, 0x02 }); // Invalid file content
        stream.Position = 0;

        // Act
        var result = await service.ProcessExcelFileAsync(stream, "test.invalid");

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ProcessExcelFileAsync_ShouldCompleteProcessing()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        
        _mockPiiSanitization
            .Setup(x => x.SanitizeResumeText(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("[SANITIZED TEXT]");

        var service = new ExcelImportService(
            context,
            _mockLogger.Object,
            _mockSkillExtraction.Object,
            _mockEmbeddingQueue.Object,
            _mockPiiSanitization.Object
        );

        using var stream = CreateTestExcelFile(withValidData: true);

        // Act
        var result = await service.ProcessExcelFileAsync(stream, "test.xlsx");

        // Assert - Verify processing completed without throwing exception
        result.Should().NotBeNull();
        result.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessExcelFileAsync_WithMissingRequiredFields_ShouldHandleGracefully()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ExcelImportService(
            context,
            _mockLogger.Object,
            _mockSkillExtraction.Object,
            _mockEmbeddingQueue.Object,
            _mockPiiSanitization.Object
        );

        var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet("Candidates");
        
        // Create header with missing fields
        var headerRow = sheet.CreateRow(0);
        headerRow.CreateCell(0).SetCellValue("Job Application");
        
        // Create incomplete data row
        var dataRow = sheet.CreateRow(1);
        dataRow.CreateCell(0).SetCellValue("Incomplete Data");

        var stream = new MemoryStream();
        workbook.Write(stream, true);
        stream.Position = 0;

        // Act
        var result = await service.ProcessExcelFileAsync(stream, "test.xlsx");

        // Assert - Should handle missing fields gracefully
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ImportCandidatesFromExcelAsync_WithFilePath_ShouldProcessFile()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var service = new ExcelImportService(
            context,
            _mockLogger.Object,
            _mockSkillExtraction.Object,
            _mockEmbeddingQueue.Object,
            _mockPiiSanitization.Object
        );

        // Create a temporary test file
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.xlsx");
        using (var fileStream = File.Create(tempPath))
        using (var testStream = CreateTestExcelFile(withValidData: true))
        {
            await testStream.CopyToAsync(fileStream);
        }

        try
        {
            // Act
            var result = await service.ImportCandidatesFromExcelAsync(tempPath);

            // Assert - Should process file without throwing exceptions
            result.Should().NotBeNull();
            result.ProcessedRows.Should().BeGreaterThanOrEqualTo(0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
