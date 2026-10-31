using System;
using System.Collections.Generic;

namespace ATLab.Results;

// The JSON test result file. These types only describe the file and depend on nothing else in ATLab,
// so a separate analysis tool can reference them to read the files.

/// <summary>A JSON test result file: the metadata of one run and the results of its exported steps.</summary>
public sealed record TestResultFile
{
    public const string CurrentFormatId = "0xA71AB-RESULT";
    public const int CurrentFormatVersion = 1;

    public string FormatId { get; init; } = CurrentFormatId;
    public int FormatVersion { get; init; } = CurrentFormatVersion;

    public required TestRunMetadata Run { get; init; }
    public required TestProjectMetadata Project { get; init; }
    public required DeviceUnderTestMetadata DeviceUnderTest { get; init; }
    public required TestHardwareMetadata TestHardware { get; init; }
    public IReadOnlyList<TestEquipmentMetadata> TestEquipment { get; init; } = [];
    public string? Notes { get; init; }
    public IReadOnlyList<TestStepResult> Steps { get; init; } = [];
}

/// <summary>When, where and by whom the run was executed, and its verdict.</summary>
public sealed record TestRunMetadata
{
    public string? SerialNumber { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public double DurationSeconds { get; init; }

    /// <summary>Verdict of the whole run, including failed steps that are excluded from export.</summary>
    public TestVerdict OverallResult { get; init; }

    public int FailedSteps { get; init; }
    public string? Operator { get; init; }
    public string? Station { get; init; }
    public TestRunMode RunMode { get; init; }

    /// <summary>1-based loop number of a continuous run, null for a single run.</summary>
    public int? LoopIndex { get; init; }
}

/// <summary>The project that defined the sequence and limits of the run.</summary>
public sealed record TestProjectMetadata
{
    public string? Name { get; init; }

    /// <summary>"sha256:" + hex hash of the project file on disk, null if the project was never saved.</summary>
    public string? FileHash { get; init; }

    /// <summary>True if the project had unsaved changes, i.e. the run does not match <see cref="FileHash"/>.</summary>
    public bool HasUnsavedChanges { get; init; }

    public string? AtlabVersion { get; init; }
}

public sealed record DeviceUnderTestMetadata
{
    public string? DeviceName { get; init; }
    public string? Revision { get; init; }
    public string? Variant { get; init; }
    public string? PartNumber { get; init; }
}

public sealed record TestHardwareMetadata
{
    public string? Name { get; init; }
    public string? SerialNumber { get; init; }
    public string? FirmwareVersion { get; init; }
    public bool IsSimulator { get; init; }
}

public sealed record TestEquipmentMetadata
{
    public required string Name { get; init; }

    /// <summary>Answer to the identification query (e.g. *IDN?), null if the device did not answer.</summary>
    public string? Identification { get; init; }
}

/// <summary>Result of one executed step.</summary>
public sealed record TestStepResult
{
    /// <summary>Stable ID of the module, unchanged by renaming or reordering.</summary>
    public required string ModuleId { get; init; }
    public required string Module { get; init; }

    /// <summary>Stable ID of the step, unchanged by renaming or renumbering.</summary>
    public required string StepId { get; init; }
    public int Number { get; init; }
    public required string Name { get; init; }

    public double? LowerLimit { get; init; }
    public double? UpperLimit { get; init; }
    /// <summary>
    /// Unit as entered in the project. In braces (e.g. "{°C}") the value is shown without SI prefix, otherwise with
    /// one (0.0013 "A" → 1.3mA).
    /// </summary>
    public string Unit { get; init; } = string.Empty;

    public TestValueStatus Status { get; init; }

    /// <summary>Measured value without SI prefix, null unless <see cref="Status"/> is Measured.</summary>
    public double? Value { get; init; }

    public double? DeviationPercent { get; init; }
    public TestVerdict Result { get; init; }

    /// <summary>Additional messages of the step, e.g. failed response checks.</summary>
    public IReadOnlyList<string>? Details { get; init; }
}

public enum TestVerdict
{
    Pass,
    Fail
}

public enum TestRunMode
{
    Single,
    Continuous
}

public enum TestValueStatus
{
    /// <summary>A numeric value was measured.</summary>
    Measured,

    /// <summary>The step produced no value (e.g. a step that only switches relays or sends a command).</summary>
    NoValue,

    Timeout,
    NACK,
    Overflow,

    /// <summary>The result could not be interpreted.</summary>
    Unknown
}