using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class TestResultExportService
{
    private readonly ProjectSettings _settings;
    private readonly CsvExportService _csvExportService;
    private readonly IErrorService _errorService;

    public TestResultExportService(
        ProjectSettings settings,
        CsvExportService csvExportService,
        IErrorService errorService)
    {
        _settings = settings;
        _csvExportService = csvExportService;
        _errorService = errorService;
    }

    public async Task SaveAsync(IEnumerable<TestStepViewModel> steps, string serialNumber, int failedSteps)
    {
        if (!_settings.IsSaveTestResult || !_settings.IsUseSerialNumber) return;
        if (_settings.SaveTestResultOptions == SaveTestResultOptions.ONLY_WHEN_PASSED && failedSteps > 0) return;

        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"{serialNumber}_{timestamp}.csv";

            var directory = _settings.SaveTestResultFilePath;
            Directory.CreateDirectory(directory);

            var fullPath = Path.Combine(directory, filename);
            await _csvExportService.ExportToPathAsync(steps, fullPath);
        }
        catch (Exception ex)
        {
            _errorService.AddError(ex.Message);
        }
    }
}
