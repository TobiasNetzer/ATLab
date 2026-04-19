using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Reporting;
using ATLab.ViewModels;
using Avalonia.Platform.Storage;
using QuestPDF.Fluent;

namespace ATLab.Services;

public class PdfExportService : IPdfExportService
{
    private readonly IFileDialogService _fileDialogService;
    private readonly IErrorService _errorService;

    public PdfExportService(IFileDialogService fileDialogService,
        IErrorService errorService)
    {
        _fileDialogService = fileDialogService;
        _errorService = errorService;
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
            var pdfBytes = BuildPdf(steps, testInfo);
            await File.WriteAllBytesAsync(outputPath, pdfBytes);
        }
        catch (Exception ex)
        {
            _errorService.AddError(ex.Message);
        }
    }

    private async Task ExportToFileAsync(IEnumerable<TestStepViewModel> steps, TestInfo testInfo, IStorageFile file)
    {
        var pdfBytes = BuildPdf(steps, testInfo);

        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(pdfBytes);
    }
    
    private byte[] BuildPdf(IEnumerable<TestStepViewModel> steps, TestInfo testInfo)
    {
        var stepList = steps.ToList();
        var document = new TestReportDocument(stepList, testInfo);
        return document.GeneratePdf();
    }
}