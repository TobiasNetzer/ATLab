using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Records;
using ATLab.Results;
using ATLab.ViewModels;

namespace ATLab.Services;

/// <summary>
/// Writes the JSON test result file: run, project, DUT and equipment metadata plus the results of the exported steps,
/// with numbers as plain JSON numbers.
/// </summary>
public class JsonExportService : IJsonExportService
{
    private readonly ILoggingService _loggingService;
    private readonly IHardwareAccessor _hardwareAccessor;
    private readonly ProjectModel _projectModel;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private static string AppVersion =>
        Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            .Split('+')[0]
        ?? "Unknown";

    public JsonExportService(
        ILoggingService loggingService,
        IHardwareAccessor hardwareAccessor,
        ProjectModel projectModel)
    {
        _loggingService = loggingService;
        _hardwareAccessor = hardwareAccessor;
        _projectModel = projectModel;
    }

    public async Task ExportToPathAsync(
        IEnumerable<TestStepViewModel> steps,
        TestInfo testInfo,
        IReadOnlyList<DeviceIdentification> testEquipment,
        string path)
    {
        var outputPath = $"{path}.json";

        try
        {
            var file = await BuildAsync(steps, testInfo, testEquipment);

            await using var stream = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(stream, file, JsonOptions);

            _loggingService.Info($"JSON exported to '{outputPath}'.");
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Failed to export JSON to '{outputPath}'. {ex.Message}");
        }
    }

    private async Task<TestResultFile> BuildAsync(
        IEnumerable<TestStepViewModel> steps,
        TestInfo testInfo,
        IReadOnlyList<DeviceIdentification> testEquipment)
    {
        var hardwareInfo = _hardwareAccessor.Hardware.HardwareInfo;
        var dut = testInfo.DeviceUnderTestInfo;

        return new TestResultFile
        {
            Run = new TestRunMetadata
            {
                SerialNumber = testInfo.SerialNumber,
                StartTime = testInfo.StartTime,
                DurationSeconds = Math.Round(testInfo.Elapsed.TotalSeconds, 3),
                OverallResult = testInfo.FailedSteps > 0 ? TestVerdict.Fail : TestVerdict.Pass,
                FailedSteps = testInfo.FailedSteps,
                Operator = testInfo.Operator,
                Station = Environment.MachineName,
                RunMode = testInfo.RunMode,
                LoopIndex = testInfo.LoopIndex
            },
            Project = new TestProjectMetadata
            {
                Name = testInfo.ProjectName,
                FileHash = await ComputeProjectFileHashAsync(),
                HasUnsavedChanges = _projectModel.IsDirty,
                AtlabVersion = AppVersion
            },
            DeviceUnderTest = new DeviceUnderTestMetadata
            {
                DeviceName = NullIfEmpty(dut.DeviceName),
                Revision = NullIfEmpty(dut.Revision),
                Variant = NullIfEmpty(dut.Variant),
                PartNumber = NullIfEmpty(dut.PartNumber)
            },
            TestHardware = new TestHardwareMetadata
            {
                Name = hardwareInfo.DeviceName,
                SerialNumber = hardwareInfo.SerialNumber,
                FirmwareVersion = hardwareInfo.FirmwareVersion,
                IsSimulator = hardwareInfo is DummyHardwareInfo
            },
            TestEquipment = testEquipment
                .Select(d => new TestEquipmentMetadata { Name = d.Name, Identification = d.Identification })
                .ToList(),
            Notes = NullIfEmpty(dut.AdditionalNotes),
            Steps = steps
                .Where(vm => !vm.TestStep.IsIgnoreStep &&
                             !vm.TestStep.IsExcludeFromExport &&
                             vm.IsExecuted)
                .Select(ToStepResult)
                .ToList()
        };
    }

    private static TestStepResult ToStepResult(TestStepViewModel vm)
    {
        var ts = vm.TestStep;
        var (status, value) = ParseValue(vm.ResultNoFormatting);

        return new TestStepResult
        {
            ModuleId = vm.Module.Id,
            Module = vm.Module.Name,
            StepId = ts.Id,
            Number = ts.Number,
            Name = ts.Name,
            LowerLimit = Finite(ts.LowerLimit),
            UpperLimit = Finite(ts.UpperLimit),
            Unit = ts.Unit.Trim('{', '}'),
            Status = status,
            Value = value,
            DeviationPercent = status == TestValueStatus.Measured ? ParseDeviation(vm.Deviation) : null,
            Result = vm.IsPassed ? TestVerdict.Pass : TestVerdict.Fail,
            Details = vm.ResultDetailLines.Count > 0 ? vm.ResultDetailLines.ToList() : null
        };
    }

    // ResultNoFormatting holds the value in the current culture (G15) or one of the special texts set by the executor
    private static (TestValueStatus Status, double? Value) ParseValue(string? raw)
    {
        switch (raw)
        {
            case null or "":
                return (TestValueStatus.NoValue, null);
            case "Timeout":
                return (TestValueStatus.Timeout, null);
            case "NACK":
                return (TestValueStatus.NACK, null);
            case "Overflow":
                return (TestValueStatus.Overflow, null);
        }

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out var value) &&
            double.IsFinite(value))
            return (TestValueStatus.Measured, value);

        return (TestValueStatus.Unknown, null);
    }

    // Deviation is shown as e.g. "1,25 %"
    private static double? ParseDeviation(string? deviation)
    {
        if (string.IsNullOrWhiteSpace(deviation))
            return null;

        var raw = deviation.Replace("%", "").Trim();

        return double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out var value)
            ? Finite(value)
            : null;
    }

    private async Task<string?> ComputeProjectFileHashAsync()
    {
        var filePath = _projectModel.FilePath;

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return null;

        try
        {
            await using var stream = File.OpenRead(filePath);
            var hash = await SHA256.HashDataAsync(stream);
            return "sha256:" + Convert.ToHexStringLower(hash);
        }
        catch (Exception ex)
        {
            _loggingService.Warning($"Could not hash project file for JSON export. {ex.Message}");
            return null;
        }
    }

    // JSON has no NaN or infinity
    private static double? Finite(double value) => double.IsFinite(value) ? value : null;

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}