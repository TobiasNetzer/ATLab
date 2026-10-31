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

            if (string.IsNullOrWhiteSpace(_settings.SaveTestResultFilePath))
            {
                _loggingService.Error("No output path for test results provided.");
                return;
            }

            var directory = _settings.SaveTestResultFilePath;

            // Folder names are fixed (not localized) so scripts and tools can rely on them
            if (_settings.IsSortResultsByVerdict)
                directory = Path.Combine(directory, testInfo.FailedSteps > 0 ? "Fail" : "Pass");

            Directory.CreateDirectory(directory);

            var fullPath = Path.Combine(directory, filename);
            var stepList = steps.ToList();

            if (_settings.IsExportCsv)
                await _csvExportService.ExportToPathAsync(stepList, fullPath);

            if (!_settings.IsExportPdf && !_settings.IsExportJson)
                return;

            // Queried once for PDF and JSON: a device that is switched off delays the export by its timeout
            var testEquipment = await _deviceIdentificationService.IdentifyReportDevicesAsync(
                _projectModel.Devices,
                CancellationToken.None);

            if (_settings.IsExportPdf)
                await _pdfExportService.ExportToPathAsync(stepList, testInfo, testEquipment, fullPath);

            if (_settings.IsExportJson)
                await _jsonExportService.ExportToPathAsync(stepList, testInfo, testEquipment, fullPath);
        }
        catch (Exception ex)
        {
            _loggingService.Error(ex.Message);
        }
    }
}