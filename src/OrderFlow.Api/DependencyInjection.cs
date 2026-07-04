using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace OrderFlow.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "OrderFlow API",
                    Version = "v1",
                    Description = "Interactive OpenAPI document for the OrderFlow study project."
                };

                return Task.CompletedTask;
            });
        });

        services.AddProblemDetails();
        services.AddValidatorsFromAssembly(typeof(Program).Assembly);

        return services;
    }
}
