namespace ATLab.Models;

/// <summary>
/// Universal result class for operations that don't return a value
/// </summary>
public class OperationResult
{
    public bool IsSuccess { get; }
    public string ErrorMessage { get; }
    public RespCmd? ResponseCommand { get; }

    private OperationResult(bool isSuccess, string errorMessage = "", RespCmd? responseCommand = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ResponseCommand = responseCommand;
    }

    public static OperationResult Success() => new(true);
    
    public static OperationResult Failure(string errorMessage, RespCmd? responseCommand = null) 
        => new(false, errorMessage, responseCommand);
}

/// <summary>
/// Universal result class for operations that return a value
/// </summary>
public class OperationResult<T>
{
    public T? Value { get; }
    public bool IsSuccess { get; }
    public string ErrorMessage { get; }
    public RespCmd? ResponseCommand { get; }

    private OperationResult(T? value, bool isSuccess, string errorMessage = "", RespCmd? responseCommand = null)
    {
        Value = value;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ResponseCommand = responseCommand;
    }

    public static OperationResult<T> Success(T value) => new(value, true);
    
    public static OperationResult<T> Failure(string errorMessage, RespCmd? responseCommand = null) 
        => new(default, false, errorMessage, responseCommand);
}
