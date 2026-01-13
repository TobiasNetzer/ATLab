using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;
using Avalonia.Platform.Storage;

namespace ATLab.Services;

public class CsvExportService
{
    private readonly IFileDialogService _fileDialogService;
    private readonly IErrorService _errorService;

    private const string Separator = "\t";

    public CsvExportService(IFileDialogService fileDialogService,
        IErrorService errorService)
    {
        _fileDialogService = fileDialogService;
        _errorService = errorService;
    }

    public async Task ExportWithDialogAsync(IEnumerable<TestStepViewModel> steps)
    {
        var file = await _fileDialogService.SaveFileAsync(
            title: "Export Measurement Data",
            suggestedName: "measurement-data",
            defaultExtension: "csv",
            extensions: new[] { "csv" });

        if (file == null)
            return; // user cancelled

        try
        {
            await ExportToFileAsync(steps, file);
        }
        catch (Exception ex)
        {
            _errorService.AddError(ex.Message);
        }
    }


    private async Task ExportToFileAsync(IEnumerable<TestStepViewModel> steps, IStorageFile file)
    {
        var csv = BuildCsv(steps);
        
        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);

        await writer.WriteAsync(csv);
    }
    
    public async Task ExportToPathAsync(IEnumerable<TestStepViewModel> steps, string path)
    {
        var csv = BuildCsv(steps);

        await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(stream, Encoding.UTF8);

        await writer.WriteAsync(csv);
    }



    private IEnumerable<TestStepCsvRow> ToCsvRows(IEnumerable<TestStepViewModel> steps)
    {
        return from vm in steps let ts = vm.TestStep select new TestStepCsvRow(
            Number: ts.Number,
            Name: ts.Name,
            NominalValue: ts.NominalValue,
            LowerLimit: ts.LowerLimit,
            UpperLimit: ts.UpperLimit,
            Unit: ts.Unit,
            Result: vm.ResultNoFormatting,
            IsPassed: vm.IsPassed ? "Pass" : "Fail",
            Deviation: vm.Deviation?.Replace("%", "")
        );
    }

    private string BuildCsv(IEnumerable<TestStepViewModel> steps)
    {
        var rows = ToCsvRows(steps);
        var sb = new StringBuilder();

        // Header
        sb.AppendLine(string.Join(Separator, new[]
        {
            "Number",
            "Name",
            "Nominal Value",
            "Lower Limit",
            "Upper Limit",
            "Measured Value",
            "Unit",
            "Deviation (%)",
            "Result"
        }));

        // Rows
        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(Separator, new[]
            {
                r.Number.ToString(CultureInfo.CurrentCulture),
                Escape(r.Name),
                r.NominalValue.ToString(CultureInfo.CurrentCulture),
                r.LowerLimit.ToString(CultureInfo.CurrentCulture),
                r.UpperLimit.ToString(CultureInfo.CurrentCulture),
                Escape(r.Result),
                Escape(r.Unit),
                Escape(r.Deviation),
                r.IsPassed
            }));
        }

        return sb.ToString();
    }


    private string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        var escaped = value.Replace("\"", "\"\"");
        
        if (escaped.Contains(Separator) || escaped.Contains('"') || escaped.Contains('\n'))
            return $"\"{escaped}\"";

        return escaped;
    }

}
