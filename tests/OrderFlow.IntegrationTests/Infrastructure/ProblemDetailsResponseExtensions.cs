using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace OrderFlow.IntegrationTests.Infrastructure;

public static class ProblemDetailsResponseExtensions
{
    public static Task<ProblemDetails?> ReadProblemDetailsAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        return response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
    }
}
