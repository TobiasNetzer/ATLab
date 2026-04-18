using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.ViewModels;
using Avalonia.Platform.Storage;
using QuestPDF.Companion;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

    public async Task ExportWithDialogAsync(IEnumerable<TestStepViewModel> steps)
    {
        var file = await _fileDialogService.SaveFileAsync(
            title: "Export Measurement Data (PDF)",
            suggestedName: "measurement-data",
            defaultExtension: "pdf",
            extensions: new[] { "pdf" });

        if (file == null)
            return;

        try
        {
            await ExportToFileAsync(steps, file);
        }
        catch (Exception ex)
        {
            _errorService.AddError(ex.Message);
        }
    }

    public async Task ExportToPathAsync(IEnumerable<TestStepViewModel> steps, string path)
    {
        try
        {
            var outputPath = $"{path}.pdf";
            var pdfBytes = BuildPdf(steps);
            await File.WriteAllBytesAsync(outputPath, pdfBytes);
        }
        catch (Exception ex)
        {
            _errorService.AddError(ex.Message);
        }
    }

    private async Task ExportToFileAsync(IEnumerable<TestStepViewModel> steps, IStorageFile file)
    {
        var pdfBytes = BuildPdf(steps);

        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(pdfBytes);
    }

    private byte[] BuildPdf(IEnumerable<TestStepViewModel> steps)
    {
        var rows = steps
            .Where(vm => !vm.TestStep.IsIgnoreStep && !vm.TestStep.IsExcludeFromExport)
            .Select(vm => new
            {
                vm.TestStep.Number,
                vm.TestStep.Name,
                vm.TestStep.LowerLimit,
                vm.TestStep.UpperLimit,
                Result = vm.ResultNoFormatting,
                vm.TestStep.Unit,
                Deviation = vm.Deviation?.Replace("%", ""),
                IsPassed = vm.IsPassed ? "Pass" : "Fail"
            })
            .ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);

                page.Header().Text("Test Report")
                    .FontSize(20)
                    .SemiBold()
                    .FontColor(Colors.Blue.Medium);

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);  // Step
                        columns.RelativeColumn();    // Name
                        columns.ConstantColumn(60);  // Lower
                        columns.ConstantColumn(80);  // Measured
                        columns.ConstantColumn(60);  // Upper
                        columns.ConstantColumn(50);  // Result
                    });

                    // Header row
                    table.Header(header =>
                    {
                        header.Cell().Element(CellHeader).Text("Step");
                        header.Cell().Element(CellHeader).Text("Name");
                        header.Cell().Element(CellHeader).Text("Lower");
                        header.Cell().Element(CellHeader).Text("Measured");
                        header.Cell().Element(CellHeader).Text("Upper");
                        header.Cell().Element(CellHeader).Text("Result");
                    });

                    // Data rows
                    foreach (var r in rows)
                    {
                        table.Cell().Element(CellBody).Text(r.Number.ToString());
                        table.Cell().Element(CellBody).Text(r.Name);
                        table.Cell().Element(CellBody).Text($"{r.LowerLimit} {r.Unit}");
                        
                        var resultWithUnit = string.IsNullOrWhiteSpace(r.Result)
                            ? "-"
                            : $"{r.Result} {r.Unit}";
    
                        table.Cell().Element(CellBody).Text(resultWithUnit);
                        
                        table.Cell().Element(CellBody).Text($"{r.UpperLimit} {r.Unit}");
                        table.Cell().Element(CellBody).Text(r.IsPassed);
                    }

                    static IContainer CellHeader(IContainer container) =>
                        container.Padding(4).Background(Colors.Grey.Lighten2).BorderBottom(1).BorderColor(Colors.Grey.Darken1);

                    static IContainer CellBody(IContainer container) =>
                        container.Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Generated by ATLab • ");
                    txt.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                });
            });
        });
        
        document.ShowInCompanion();

        return document.GeneratePdf();
    }
}