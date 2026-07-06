using OrderFlow.Application.Common.Errors;

namespace OrderFlow.Application.Common.Results;

public class Result
{
    protected Result(bool isSuccess, AppError? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("A successful result cannot contain an error.");

        if (!isSuccess && error is null)
            throw new InvalidOperationException("A failed result must contain an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public AppError? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(AppError error) => new(false, error);
}

public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, AppError? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static new Result<T> Failure(AppError error) => new(false, default, error);
}
