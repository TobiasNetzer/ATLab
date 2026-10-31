namespace ATLab.Records;

/// <summary>One row of the CSV export, values already formatted culture independent.</summary>
public sealed record TestStepCsvRow(
    string Module,
    int Number,
    string Name,
    string LowerLimit,
    string UpperLimit,
    string? Result,
    string Unit,
    string IsPassed,
    string? Deviation,
    string? Details
);
