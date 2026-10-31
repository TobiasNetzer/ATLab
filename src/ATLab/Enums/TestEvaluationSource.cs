namespace ATLab.Enums;

/// <summary>
/// Where a test step gets its measured value from. New values are appended so stored projects keep their numbers.
/// </summary>
public enum TestEvaluationSource
{
    NONE,
    SCRIPT,
    COMMAND,
    INTERFACE,
    EXTERNAL_PROGRAM,
    USER_CONFIRMATION,
    USER_INPUT,
    EXPRESSION,
    FILE,
    BOUNDARY_SCAN,
    BOUNDARY_SCAN_BUS
}