using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Records;
using ATLab.Reporting;
using ATLab.ViewModels;
using Avalonia.Platform.Storage;
using QuestPDF.Fluent;

namespace ATLab.Services;

/// <summary>
/// Creates the PDF test report, including the identification of the devices marked for the report.
/// </summary>
public class PdfExportService : IPdfExportService
{
    private readonly IFileDialogService _fileDialogService;
    private readonly ILoggingService _loggingService;
    private readonly IHardwareInfo _hardwareInfo;
    private readonly IDeviceIdentificationService _deviceIdentificationService;
    private readonly ProjectModel _projectModel;

    public PdfExportService(
        IFileDialogService fileDialogService,
        ILoggingService loggingService,
        IHardwareAccessor hardwareAccessor,
        IDeviceIdentificationService deviceIdentificationService,
        ProjectModel projectModel)
    {
        _fileDialogService = fileDialogService;
        _loggingService = loggingService;
        _hardwareInfo = hardwareAccessor.Hardware.HardwareInfo;
        _deviceIdentificationService = deviceIdentificationService;
        _projectModel = projectModel;
    }

    public async Task ExportWithDialogAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo)
    {
        var file = await _fileDialogService.SaveFileAsync(
            title: "Export Test Report",
            suggestedName: "Test Report",
            defaultExtension: "pdf",
            extensions: new[] { "pdf" });

        if (file == null)
        {
            _loggingService.Info("PDF export canceled.");
            return;
        }

        try
        {
            await ExportToFileAsync(steps, testInfo, file);

            _loggingService.Info(
                $"PDF export completed: {file.Name}");
        }
        catch (Exception ex)
        {
            _loggingService.Error(
                $"PDF export failed. {ex.Message}");
        }
    }

    public async Task ExportToPathAsync(
        IEnumerable<TestStepViewModel> steps,
        TestInfo testInfo,
        IReadOnlyList<DeviceIdentification> testEquipment,
        string path)
    {
        try
        {
            var outputPath = $"{path}.pdf";
            var pdfBytes = BuildPdf(steps, testInfo, testEquipment);

            await File.WriteAllBytesAsync(outputPath, pdfBytes);

            _loggingService.Info(
                $"PDF exported to '{outputPath}'.");
        }
        catch (Exception ex)
        {
            _loggingService.Error(
                $"Failed to export PDF to '{path}.pdf'. {ex.Message}");
        }
    }

    private async Task ExportToFileAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo, IStorageFile file)
    {
        var pdfBytes = BuildPdf(steps, testInfo, await _deviceIdentificationService.IdentifyReportDevicesAsync(_projectModel.Devices, CancellationToken.None));

        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(pdfBytes);
    }

    private byte[] BuildPdf(
        IEnumerable<TestStepViewModel> steps,
        TestInfo testInfo,
        IReadOnlyList<DeviceIdentification> testEquipment)
    {
        var executed = steps
            .Where(vm => !vm.TestStep.IsIgnoreStep && vm.IsExecuted)
            .ToList();

        var stepList = executed
            .Where(vm => !vm.TestStep.IsExcludeFromExport)
            .ToList();

        // Failed steps hidden from the table still make the run fail; the report says how many are not shown
        var failedExcludedSteps = executed
            .Count(vm => vm.TestStep.IsExcludeFromExport && !vm.IsPassed);

        var document = new TestReportDocument(
            stepList,
            failedExcludedSteps,
            testInfo,
            _hardwareInfo,
            testEquipment.ToList());

        return document.GeneratePdf();
    }
}