using System.Net;
using System.Text.Json;

namespace StoryMixer.Api.Tests.Features.OpenApi;

public class OpenApiTests(StoryMixerApiFactory factory) : IClassFixture<StoryMixerApiFactory>
{
    [Fact]
    public async Task OpenApiDocument_DeclaresBearerSecurityScheme()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var bearer = document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
    }
}
