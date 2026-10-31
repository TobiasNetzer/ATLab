namespace ATLab.Records;

/// <summary>Whether a measured value is within the limits, and its deviation from nominal in percent.</summary>
public record TestEvaluationResult(bool IsValid, double Deviation);