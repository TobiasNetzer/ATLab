namespace ATLab.Records;

public sealed record TestStepCsvRow(
    string Module,
    int Number,
    string Name,
    string LowerLimit,
    string UpperLimit,
    string? Result,
    string Unit,
    string IsPassed,
    string? Deviation
);
