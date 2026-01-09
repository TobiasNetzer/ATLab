namespace ATLab.Models;

public sealed record TestStepCsvRow(
    int Number,
    string Name,
    double NominalValue,
    double LowerLimit,
    double UpperLimit,
    string Unit,
    string? Result,
    string IsPassed,
    string? Deviation
);
