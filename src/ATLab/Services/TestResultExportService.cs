using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

/// <summary>
/// Exports CSV, PDF and JSON after a run according to the project settings, named after serial number and start time.
/// </summary>
public class TestResultExportService : ITestResultExportService
{
    private readonly ProjectModel _projectModel;
    private readonly ProjectSettings _settings;
    private readonly ICsvExportService _csvExportService;
    private readonly IPdfExportService _pdfExportService;
    private readonly IJsonExportService _jsonExportService;
    private readonly IDeviceIdentificationService _deviceIdentificationService;
    private readonly ILoggingService _loggingService;

    public TestResultExportService(
        ProjectModel projectModel,
        ICsvExportService csvExportService,
        IPdfExportService pdfExportService,
        IJsonExportService jsonExportService,
        IDeviceIdentificationService deviceIdentificationService,
        ILoggingService loggingService)
    {
        _projectModel = projectModel;
        _settings = projectModel.Settings;
        _csvExportService = csvExportService;
        _pdfExportService = pdfExportService;
        _jsonExportService = jsonExportService;
        _deviceIdentificationService = deviceIdentificationService;
        _loggingService = loggingService;
    }

    public async Task SaveAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo)
    {
        if (!_settings.IsSaveTestResult || !_settings.IsUseSerialNumber) return;
        if (_settings.SaveTestResultOptions == SaveTestResultOptions.ONLY_WHEN_PASSED && testInfo.FailedSteps > 0) return;

        try
        {
            var timestamp = testInfo.StartTime.ToString("yyyy-MM-dd_HH-mm-ss");
            var filename = $"{testInfo.SerialNumber}_{timestamp}";
            var stepList = steps.ToList();

            // Each format resolves its own folder, so a missing path only skips that format
            var csvPath = _settings.IsExportCsv
                ? GetFilePath(_settings.CsvOutputPath, "CSV", filename, testInfo)
                : null;
            var pdfPath = _settings.IsExportPdf
                ? GetFilePath(_settings.PdfOutputPath, "PDF", filename, testInfo)
                : null;
            var jsonPath = _settings.IsExportJson
                ? GetFilePath(_settings.JsonOutputPath, "JSON", filename, testInfo)
                : null;

            if (csvPath != null)
                await _csvExportService.ExportToPathAsync(stepList, csvPath);

            if (pdfPath == null && jsonPath == null)
                return;

            // Queried once for PDF and JSON: a device that is switched off delays the export by its timeout
            var testEquipment = await _deviceIdentificationService.IdentifyReportDevicesAsync(
                _projectModel.Devices,
                CancellationToken.None);

            if (pdfPath != null)
                await _pdfExportService.ExportToPathAsync(stepList, testInfo, testEquipment, pdfPath);

            if (jsonPath != null)
                await _jsonExportService.ExportToPathAsync(stepList, testInfo, testEquipment, jsonPath);
        }
        catch (Exception ex)
        {
            _loggingService.Error(ex.Message);
        }
    }

    /// <summary>
    /// Path without extension for one export format: its folder plus the Pass/Fail subfolder. Null (and an error
    /// in the log) if the format has no folder.
    /// </summary>
    private string? GetFilePath(string directory, string format, string filename, TestInfo testInfo)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            _loggingService.Error($"No output path for the {format} export provided.");
            return null;
        }

        // Folder names are fixed (not localized) so scripts and tools can rely on them
        if (_settings.IsSortResultsByVerdict)
            directory = Path.Combine(directory, testInfo.FailedSteps > 0 ? "Fail" : "Pass");

        Directory.CreateDirectory(directory);

        return Path.Combine(directory, filename);
    }
}