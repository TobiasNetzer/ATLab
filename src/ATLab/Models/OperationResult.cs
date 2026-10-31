using ATLab.Enums;

namespace ATLab.Models;

/// <summary>Outcome of an operation without value: success, failure with message, timeout or I²C NACK.</summary>
public class OperationResult
{
    private OperationStatus Status { get; }
    public string ErrorMessage { get; }

    public bool IsSuccess => Status == OperationStatus.SUCCESS;
    public bool IsFailure => Status == OperationStatus.FAILURE;
    public bool IsTimeout => Status == OperationStatus.TIMEOUT;
    public bool IsNack => Status == OperationStatus.NACK;

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

    public static OperationResult Nack(string? message = null) =>
        new(OperationStatus.NACK, message ?? "NACK");
}

/// <summary>Outcome of an operation that returns a value on success.</summary>
public class OperationResult<T>
{
    public T? Value { get; }
    public OperationStatus Status { get; }
    public string ErrorMessage { get; }

    public bool IsSuccess => Status == OperationStatus.SUCCESS;
    public bool IsFailure => Status == OperationStatus.FAILURE;
    public bool IsTimeout => Status == OperationStatus.TIMEOUT;
    public bool IsNack => Status == OperationStatus.NACK;

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

    public static OperationResult<T> Nack(string? message = null) =>
        new(default, OperationStatus.NACK, message ?? "NACK");
}