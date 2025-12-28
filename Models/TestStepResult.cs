namespace ATLab.Models;

public class TestStepResult
{
    public bool IsSuccess { get; }
    public double MeasuredValue { get; }

    public TestStepResult(bool isSuccess, double measuredValue)
    {
        IsSuccess = isSuccess;
        MeasuredValue = measuredValue;
    }
}
