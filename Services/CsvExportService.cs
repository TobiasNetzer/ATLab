using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    
    private readonly string _separator = "\t"; // or "\t"

    public CsvExportService(IFileDialogService fileDialogService,
        IErrorService errorService)
    {
        _fileDialogService = fileDialogService;
        _errorService = errorService;
    }

    public async Task<bool> ExportWithDialogAsync(IEnumerable<TestStepViewModel> steps)
    {
        var file = await _fileDialogService.SaveFileAsync(
            title: "Export Measurement Data",
            suggestedName: "measurement-data",
            defaultExtension: "csv",
            extensions: new[] { "csv" });

        if (file == null)
            return false; // user cancelled

        try
        {
            await ExportToFileAsync(steps, file);
            return true;
        }
        catch (Exception ex)
        {
            _errorService.AddError(ex.Message);
            return false;
        }
    }


    private async Task ExportToFileAsync(IEnumerable<TestStepViewModel> steps, IStorageFile file)
    {
        var csv = BuildCsv(steps);
        
        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);

        await writer.WriteAsync(csv);
    }

    
    public IEnumerable<TestStepCsvRow> ToCsvRows(IEnumerable<TestStepViewModel> steps)
    {
        foreach (var vm in steps)
        {
            var ts = vm.TestStep;

            yield return new TestStepCsvRow(
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
    }

    private string BuildCsv(IEnumerable<TestStepViewModel> steps)
    {
        var rows = ToCsvRows(steps);
        var sb = new StringBuilder();

        // Header
        sb.AppendLine(string.Join(_separator, new[]
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
            sb.AppendLine(string.Join(_separator, new[]
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
        
        if (escaped.Contains(_separator) || escaped.Contains('"') || escaped.Contains('\n'))
            return $"\"{escaped}\"";

        return escaped;
    }

}
