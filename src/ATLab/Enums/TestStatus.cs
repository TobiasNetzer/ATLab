namespace ATLab.Enums;

/// <summary>State of the current test run.</summary>
public enum TestStatus
{
    IDLE,
    RUNNING,
    PASSED,
    FAILED,
    CANCELED,
    EXECUTION_ERROR
}