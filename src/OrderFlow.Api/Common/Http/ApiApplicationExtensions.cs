using Scalar.AspNetCore;

namespace OrderFlow.Api.Common.Http;

public static class ApiApplicationExtensions
{
    public static WebApplication UseApi(this WebApplication app)
    {
        app.UseGlobalExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        return app;
    }
}
