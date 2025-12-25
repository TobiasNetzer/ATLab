namespace ATLab.Models;

public class TestStepResult
{
    public bool IsSuccess { get; set; }
    public double MeasuredValue { get; set; }

    public TestStepResult(bool isSuccess, double measuredValue)
    {
        IsSuccess = isSuccess;
        MeasuredValue = measuredValue;
    }

    public TestStepResult() { }
}
