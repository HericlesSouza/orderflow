namespace OrderFlow.Application.Common.Errors;

public sealed record AppError(string Code, string Message, AppErrorType Type)
{
    public static AppError Validation(string code, string message)
    {
        return new AppError(code, message, AppErrorType.Validation);
    }

    public static AppError NotFound(string code, string message)
    {
        return new AppError(code, message, AppErrorType.NotFound);
    }

    public static AppError Conflict(string code, string message)
    {
        return new AppError(code, message, AppErrorType.Conflict);
    }
}
