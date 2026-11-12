using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using BookingSystem.Api;
using BookingSystem.Api.Attributes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BookingSystem.Api.Tests;

public class ProblemDetailsTests
{
    [Theory]
    [InlineData("Development", "InMemory")]
    [InlineData("Production", "InMemory")]
    [InlineData("Development", "EfCoreInMemory")]
    [InlineData("Production", "EfCoreInMemory")]
    public async Task UnhandledExceptionReturnsGenericProblemDetails(string environment, string provider)
    {
        await using var factory = CreateFactory(environment, provider);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/v1/bookings", new { userId = -1, eventId = 1 });
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
        Assert.DoesNotContain("User not found", body);
        Assert.DoesNotContain("ArgumentException", body);
    }

    [Fact]
    public void ParameterValidationFilterReturnsValidationProblemDetails()
    {
        var descriptor = new ControllerActionDescriptor
        {
            MethodInfo = typeof(ValidationTarget).GetMethod(nameof(ValidationTarget.Action))!
        };
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), descriptor, new ModelStateDictionary());
        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?> { ["value"] = null },
            new ValidationTarget());

        new ValidateModelStateAttribute().OnActionExecuting(context);

        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.Contains("value", problem.Errors.Keys);
    }

    [Fact]
    public async Task InvalidInputReturnsValidationProblemDetails()
    {
        await using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();
        using var content = new StringContent("{ invalid json", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/v1/bookings", content);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
        Assert.True(json.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task UnknownRouteReturnsProblemDetails()
    {
        await using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/no-such-route");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task SuccessfulRequestIsUnchanged()
    {
        await using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/users/list");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
        Assert.Contains(json.RootElement.EnumerateArray(), user => user.GetProperty("name").GetString() == "Alice");
    }

    private static WebApplicationFactory<Program> CreateFactory(string environment, string provider = "InMemory") =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["DataStore:Provider"] = provider }));
        });

    private sealed class ValidationTarget
    {
        public void Action([Required] string? value) { }
    }
}
