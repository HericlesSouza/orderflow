using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace OrderFlow.Api.Common.Http;

public static class ExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                var problem = new ProblemDetails
                {
                    Title = "An unexpected error occurred.",
                    Detail = app.ApplicationServices
                        .GetRequiredService<IHostEnvironment>()
                        .IsDevelopment()
                        ? exception?.Message
                        : null,
                    Status = StatusCodes.Status500InternalServerError
                };

                context.Response.StatusCode = problem.Status.Value;

                await Results.Problem(
                    title: problem.Title,
                    detail: problem.Detail,
                    statusCode: problem.Status)
                    .ExecuteAsync(context);
            });
        });

        return app;
    }
}
