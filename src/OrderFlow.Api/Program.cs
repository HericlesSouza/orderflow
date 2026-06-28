var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new HealthCheckResponse("Healthy")))
    .WithName("HealthCheck");

app.Run();

internal sealed record HealthCheckResponse(string Status);
