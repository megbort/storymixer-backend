using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace StoryMixer.Api.Tests.Features.OpenApi;

public class OpenApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task OpenApiDocument_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
