namespace ATLab.Records;

public sealed record TestStepCsvRow(
    int Number,
    string Name,
    string LowerLimit,
    string UpperLimit,
    string? Result,
    string Unit,
    string IsPassed,
    string? Deviation
);
