using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Api.Common.Http;
using OrderFlow.Application.Common.Errors;

namespace OrderFlow.UnitTests.Api.Common.Http;

[Trait("Layer", "Api")]
[Trait("Feature", "Common")]
public sealed class AppErrorHttpMapperTests
{
    [Theory]
    [InlineData(AppErrorType.Validation, StatusCodes.Status400BadRequest, "Validation failed.")]
    [InlineData(AppErrorType.NotFound, StatusCodes.Status404NotFound, "Resource not found.")]
    [InlineData(AppErrorType.Conflict, StatusCodes.Status409Conflict, "Conflict detected.")]
    public void Should_Map_AppError_To_ProblemDetails_With_Stable_Code_When_Error_Type_Is_Known(
        AppErrorType errorType,
        int expectedStatusCode,
        string expectedTitle)
    {
        var error = new AppError("sample.error", "Sample error message.", errorType);

        var result = error.ToProblem();

        var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        var valueResult = Assert.IsAssignableFrom<IValueHttpResult<ProblemDetails>>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(valueResult.Value);

        Assert.Equal(expectedStatusCode, statusCodeResult.StatusCode);
        Assert.Equal(expectedTitle, problemDetails.Title);
        Assert.Equal(error.Message, problemDetails.Detail);
        Assert.Equal(error.Code, problemDetails.Extensions["code"]);
    }
}
