using System.Net;
using System.Net.Http.Json;
using System.Text;
using KeyboardBindings.Api.Contracts;

namespace KeyboardBindings.Tests;

/// <summary>
/// Boots the real API (via WebApplicationFactory) against a throwaway SQLite
/// file so requests exercise the full HTTP + EF + validation pipeline.
/// </summary>
public class MappingsApiTests : IClassFixture<ApiTestFactory>
{
    private const string Keyboard = "Apex Pro Gen 3";
    private readonly HttpClient _client;

    public MappingsApiTests(ApiTestFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Get_ReturnsAllKeys()
    {
        var response = await _client.GetFromJsonAsync<KeyboardMappingsResponse>(
            $"/keyboards/{Uri.EscapeDataString(Keyboard)}/mappings");

        Assert.NotNull(response);
        Assert.Equal(Keyboard, response!.Keyboard);
        Assert.Equal(92, response.Mappings.Count);
    }

    [Fact]
    public async Task Put_ThenGet_ReflectsRemap()
    {
        var body = new AssignMappingsRequest([new RemapDto("0x04", "0x1D")]); // A -> Z
        var put = await _client.PutAsJsonAsync(
            $"/keyboards/{Uri.EscapeDataString(Keyboard)}/mappings", body);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var response = await _client.GetFromJsonAsync<KeyboardMappingsResponse>(
            $"/keyboards/{Uri.EscapeDataString(Keyboard)}/mappings");
        var a = response!.Mappings.Single(m => m.PhysicalKey.Code == 0x04);
        Assert.Equal(0x1D, a.MappedKey.Code);
        Assert.True(a.IsRemapped);
    }

    [Fact]
    public async Task Put_InvalidMapping_Returns400()
    {
        var body = new AssignMappingsRequest([new RemapDto("0xFF", "0x1D")]);
        var put = await _client.PutAsJsonAsync(
            $"/keyboards/{Uri.EscapeDataString(Keyboard)}/mappings", body);
        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
    }

    [Theory]
    [InlineData("""{"mappings":[null]}""")]
    [InlineData("""{"mappings":[{"from":"0x04","to":"0x1D"},null]}""")]
    public async Task Put_NullMappingEntry_Returns400_NotServerError(string body)
    {
        // Regression: a null array element used to dereference into a NullReferenceException (500) rather than 400.
        var response = await _client.PutAsync(
            $"/keyboards/{Uri.EscapeDataString(Keyboard)}/mappings",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownKeyboard_Returns404()
    {
        var response = await _client.GetAsync("/keyboards/Nope/mappings");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SecurityHeader_IsPresent()
    {
        var response = await _client.GetAsync(
            $"/keyboards/{Uri.EscapeDataString(Keyboard)}/mappings");
        Assert.Contains("nosniff", response.Headers.GetValues("X-Content-Type-Options"));
    }

    [Fact]
    public async Task Put_UnknownKeyboard_Returns404()
    {
        // A valid body against an unsupported keyboard is still 404 — the keyboard is checked before the mappings.
        var body = new AssignMappingsRequest([new RemapDto("0x04", "0x1D")]);
        var put = await _client.PutAsJsonAsync("/keyboards/Nope/mappings", body);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
    }
}
