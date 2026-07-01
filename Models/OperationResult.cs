using ATLab.Enums;

namespace ATLab.Models;

public class OperationResult
{
    private OperationStatus Status { get; }
    public string ErrorMessage { get; }

    public bool IsSuccess => Status == OperationStatus.SUCCESS;
    public bool IsFailure => Status == OperationStatus.FAILURE;
    public bool IsTimeout => Status == OperationStatus.TIMEOUT;

    private OperationResult(OperationStatus status, string errorMessage = "")
    {
        Status = status;
        ErrorMessage = errorMessage;
    }

    public static OperationResult Success() =>
        new(OperationStatus.SUCCESS);

    public static OperationResult Failure(string errorMessage) =>
        new(OperationStatus.FAILURE, errorMessage);

    public static OperationResult Timeout(string? message = null) =>
        new(OperationStatus.TIMEOUT, message ?? "Timeout");
}

public class OperationResult<T>
{
    public T? Value { get; }
    public OperationStatus Status { get; }
    public string ErrorMessage { get; }

    public bool IsSuccess => Status == OperationStatus.SUCCESS;
    public bool IsFailure => Status == OperationStatus.FAILURE;
    public bool IsTimeout => Status == OperationStatus.TIMEOUT;

    private OperationResult(T? value, OperationStatus status, string errorMessage = "")
    {
        Value = value;
        Status = status;
        ErrorMessage = errorMessage;
    }

    public static OperationResult<T> Success(T value) =>
        new(value, OperationStatus.SUCCESS);

    public static OperationResult<T> Failure(string errorMessage) =>
        new(default, OperationStatus.FAILURE, errorMessage);

    public static OperationResult<T> Timeout(string? message = null) =>
        new(default, OperationStatus.TIMEOUT, message ?? "Timeout");
}