using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Records;
using ATLab.ViewModels;
using Avalonia.Platform.Storage;

namespace ATLab.Services;

public class CsvExportService : ICsvExportService
{
    private readonly IFileDialogService _fileDialogService;
    private readonly ILoggingService _loggingService;

    private const string Separator = "\t";

    public CsvExportService(
        IFileDialogService fileDialogService,
        ILoggingService loggingService)
    {
        _fileDialogService = fileDialogService;
        _loggingService = loggingService;
    }

    public async Task ExportWithDialogAsync(IEnumerable<TestStepViewModel> steps)
    {
        var file = await _fileDialogService.SaveFileAsync(
            title: "Export Measurement Data",
            suggestedName: "Measurement Data",
            defaultExtension: "csv",
            extensions: new[] { "csv" });

        if (file == null)
        {
            _loggingService.Info("CSV export cancelled.");
            return;
        }

        try
        {
            await ExportToFileAsync(steps, file);

            _loggingService.Info(
                $"CSV export completed: {file.Name}");
        }
        catch (Exception ex)
        {
            _loggingService.Error(
                $"CSV export failed. {ex.Message}");
        }
    }

    private async Task ExportToFileAsync(
        IEnumerable<TestStepViewModel> steps,
        IStorageFile file)
    {
        var csv = BuildCsv(steps);

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);

        await writer.WriteAsync(csv);
    }

    public async Task ExportToPathAsync(
        IEnumerable<TestStepViewModel> steps,
        string path)
    {
        try
        {
            var csv = BuildCsv(steps);

            var outputPath = $"{path}.csv";

            await using var stream = File.Open(
                outputPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            await using var writer = new StreamWriter(
                stream,
                Encoding.UTF8);

            await writer.WriteAsync(csv);

            _loggingService.Info(
                $"CSV exported to '{outputPath}'.");
        }
        catch (Exception ex)
        {
            _loggingService.Error(
                $"Failed to export CSV to '{path}.csv'. {ex.Message}");

            throw;
        }
    }

    private IEnumerable<TestStepCsvRow> ToCsvRows(
        IEnumerable<TestStepViewModel> steps)
    {
        return steps
            .Where(vm =>
                !vm.TestStep.IsIgnoreStep &&
                !vm.TestStep.IsExcludeFromExport &&
                vm.IsExecuted)
            .Select(vm =>
            {
                var ts = vm.TestStep;

                string? normalizedResult;

                if (double.TryParse(
                        vm.ResultNoFormatting,
                        NumberStyles.Any,
                        CultureInfo.CurrentCulture,
                        out var numericResult))
                {
                    normalizedResult =
                        numericResult.ToString(
                            CultureInfo.InvariantCulture);
                }
                else
                {
                    normalizedResult = vm.ResultNoFormatting;
                }

                string? normalizedDeviation = null;

                if (!string.IsNullOrWhiteSpace(vm.Deviation))
                {
                    var raw = vm.Deviation.Replace("%", "");

                    if (double.TryParse(
                            raw,
                            NumberStyles.Any,
                            CultureInfo.CurrentCulture,
                            out double dev))
                    {
                        normalizedDeviation =
                            dev.ToString(
                                CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        normalizedDeviation = raw;
                    }
                }

                return new TestStepCsvRow(
                    Number: ts.Number,
                    Name: ts.Name,
                    LowerLimit: ts.LowerLimit.ToString(
                        CultureInfo.InvariantCulture),
                    UpperLimit: ts.UpperLimit.ToString(
                        CultureInfo.InvariantCulture),
                    Result: normalizedResult,
                    Unit: ts.Unit.Trim('{', '}'),
                    IsPassed: vm.IsPassed ? "Pass" : "Fail",
                    Deviation: normalizedDeviation
                );
            });
    }

    private string BuildCsv(IEnumerable<TestStepViewModel> steps)
    {
        var stepList = steps.ToList();
        var rows = ToCsvRows(stepList);

        var sb = new StringBuilder();

        sb.AppendLine(string.Join(Separator, new[]
        {
            "Step",
            "Name",
            "Lower Limit",
            "Measured Value",
            "Upper Limit",
            "Unit",
            "Deviation (%)",
            "Result"
        }));

        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(Separator, new[]
            {
                r.Number.ToString(CultureInfo.CurrentCulture),
                Escape(r.Name),
                r.LowerLimit.ToString(CultureInfo.CurrentCulture),
                Escape(r.Result),
                r.UpperLimit.ToString(CultureInfo.CurrentCulture),
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

        if (escaped.Contains(Separator) ||
            escaped.Contains('"') ||
            escaped.Contains('\n'))
        {
            return $"\"{escaped}\"";
        }

        return escaped;
    }
}