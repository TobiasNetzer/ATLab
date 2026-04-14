namespace ATLab.Models;

public sealed record TestStepCsvRow(
    int Number,
    string Name,
    double LowerLimit,
    double UpperLimit,
    string? Result,
    string Unit,
    string IsPassed,
    string? Deviation
);
