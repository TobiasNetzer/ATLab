namespace ATLab.Models;

/// <summary>
/// Universal result class for operations that don't return a value
/// </summary>
public class OperationResult
{
    public bool IsSuccess { get; }
    public string ErrorMessage { get; }

    private OperationResult(bool isSuccess, string errorMessage = "")
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static OperationResult Success() => new(true);
    
    public static OperationResult Failure(string errorMessage) 
        => new(false, errorMessage);
}

/// <summary>
/// Universal result class for operations that return a value
/// </summary>
public class OperationResult<T>
{
    public T? Value { get; }
    public bool IsSuccess { get; }
    public string ErrorMessage { get; }

    private OperationResult(T? value, bool isSuccess, string errorMessage = "")
    {
        Value = value;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static OperationResult<T> Success(T value) => new(value, true);
    
    public static OperationResult<T> Failure(string errorMessage) 
        => new(default, false, errorMessage);
}
