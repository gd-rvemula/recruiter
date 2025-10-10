using RecruiterApi.DTOs;

namespace RecruiterApi.Services;

public interface IExcelImportService
{
    Task<ExcelImportResultDto> ImportCandidatesFromExcelAsync(string filePath, bool overwriteExisting = false);
    Task<ExcelImportResultDto> ProcessExcelFileAsync(Stream fileStream, string fileName);
    Task<SearchIndexResponse> RefreshSearchIndexAfterImportAsync();
}
