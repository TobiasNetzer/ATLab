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

public class PdfExportService : IPdfExportService
{
    private readonly IFileDialogService _fileDialogService;
    private readonly IErrorService _errorService;
    private readonly IHardwareInfo _hardwareInfo;
    private readonly DeviceManagerViewModel _deviceManager;
    private readonly IDeviceIdentificationService _deviceIdentificationService;

    private readonly List<DeviceIdentification> _devices = new();

    public PdfExportService(
        IFileDialogService fileDialogService,
        IErrorService errorService,
        IHardwareInfo hardwareInfo,
        DeviceManagerViewModel deviceManager,
        IDeviceIdentificationService deviceIdentificationService)
    {
        _fileDialogService = fileDialogService;
        _errorService = errorService;
        _hardwareInfo = hardwareInfo;
        _deviceManager = deviceManager;
        _deviceIdentificationService = deviceIdentificationService;
    }

    public async Task ExportWithDialogAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo)
    {
        var file = await _fileDialogService.SaveFileAsync(
            title: "Export Test Report",
            suggestedName: "Test Report",
            defaultExtension: "pdf",
            extensions: new[] { "pdf" });

        if (file == null)
            return;

        try
        {
            await ExportToFileAsync(steps, testInfo, file);
        }
        catch (Exception ex)
        {
            _errorService.AddError(ex.Message);
        }
    }

    public async Task ExportToPathAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo, string path)
    {
        try
        {
            var outputPath = $"{path}.pdf";
            var pdfBytes = await BuildPdf(steps, testInfo);
            await File.WriteAllBytesAsync(outputPath, pdfBytes);
        }
        catch (Exception ex)
        {
            _errorService.AddError(ex.Message);
        }
    }

    private async Task ExportToFileAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo, IStorageFile file)
    {
        var pdfBytes = await BuildPdf(steps, testInfo);

        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(pdfBytes);
    }
    
    private async Task<byte[]> BuildPdf(IEnumerable<TestStepViewModel> steps, TestInfo testInfo)
    {
        _devices.Clear();
        
        foreach (var device in _deviceManager.Devices)
        {
            if (!device.IsIncludeInReport)
                continue;
            
            var identification = await _deviceIdentificationService.GetIdentificationAsync(device, CancellationToken.None);
            _devices.Add(new DeviceIdentification(device.Name, identification));
        }
        
        var stepList = steps
            .Where(vm => !vm.TestStep.IsIgnoreStep && !vm.TestStep.IsExcludeFromExport && vm.IsExecuted)
            .ToList();
        var document = new TestReportDocument(stepList, testInfo, _hardwareInfo, _devices);
        return document.GeneratePdf();
    }
}