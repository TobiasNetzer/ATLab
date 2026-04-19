using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class TestResultExportService
{
    private readonly ProjectSettings _settings;
    private readonly ICsvExportService _csvExportService;
    private readonly IPdfExportService _pdfExportService;
    private readonly IErrorService _errorService;

    public TestResultExportService(
        ProjectSettings settings,
        ICsvExportService csvExportService,
        IPdfExportService pdfExportService,
        IErrorService errorService)
    {
        _settings = settings;
        _csvExportService = csvExportService;
        _pdfExportService = pdfExportService;
        _errorService = errorService;
    }

    public async Task SaveAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo, int failedSteps)
    {
        if (!_settings.IsSaveTestResult || !_settings.IsUseSerialNumber) return;
        if (_settings.SaveTestResultOptions == SaveTestResultOptions.ONLY_WHEN_PASSED && failedSteps > 0) return;

        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var filename = $"{testInfo.SerialNumber}_{timestamp}";

            var directory = _settings.SaveTestResultFilePath;
            Directory.CreateDirectory(directory);

            var fullPath = Path.Combine(directory, filename);
            if (_settings.IsExportCsv)
                await _csvExportService.ExportToPathAsync(steps, fullPath);
            
            if (_settings.IsExportPdf)
                await _pdfExportService.ExportToPathAsync(steps, testInfo, fullPath);
        }
        catch (Exception ex)
        {
            _errorService.AddError(ex.Message);
        }
    }
}
