using OrderFlow.Api.Common.Http;
using OrderFlow.Api.InventoryItems;
using OrderFlow.Api.Products;
using OrderFlow.Api;
using OrderFlow.Application;
using OrderFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.EnvironmentName);

var app = builder.Build();

app.UseApi();

app.MapGet("/health", () => Results.Ok(new HealthCheckResponse("Healthy")))
    .WithName("HealthCheck");

app.MapProductEndpoints();
app.MapInventoryItemEndpoints();

app.Run();

internal sealed record HealthCheckResponse(string Status);

public partial class Program;
