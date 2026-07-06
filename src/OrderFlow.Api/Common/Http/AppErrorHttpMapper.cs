using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Common.Results;

namespace OrderFlow.Api.Common.Http;

public static class AppErrorHttpMapper
{
    public static IResult ToProblem<T>(this Result<T> result)
    {
        if (result.Error is null)
            throw new InvalidOperationException("A failed result must contain an error.");

        return result.Error.ToProblem();
    }

    public static IResult ToProblem(this AppError error)
    {
        var (title, statusCode) = GetProblemShape(error.Type);

        return Results.Problem(
            title: title,
            detail: error.Message,
            statusCode: statusCode);
    }

    private static (string Title, int StatusCode) GetProblemShape(AppErrorType type)
    {
        return type switch
        {
            AppErrorType.Validation => ("Validation failed.", StatusCodes.Status400BadRequest),
            AppErrorType.NotFound => ("Resource not found.", StatusCodes.Status404NotFound),
            AppErrorType.Conflict => ("Conflict detected.", StatusCodes.Status409Conflict),
            _ => ("Unexpected application error.", StatusCodes.Status500InternalServerError)
        };
    }
}
