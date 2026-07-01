using FluentValidation;
using OrderFlow.Api.Common.Http;
using OrderFlow.Api.Products;
using OrderFlow.Api.Products.Create;
using OrderFlow.Application;
using OrderFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.EnvironmentName);

var app = builder.Build();

app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new HealthCheckResponse("Healthy")))
    .WithName("HealthCheck");

app.MapProductEndpoints();

app.Run();

internal sealed record HealthCheckResponse(string Status);

public partial class Program;
