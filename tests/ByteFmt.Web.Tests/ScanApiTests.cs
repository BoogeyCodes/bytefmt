using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ByteFmt.Web.Tests;

public sealed class ScanApiTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dir;

    public ScanApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _dir = Path.Combine(Path.GetTempPath(), "bytefmt-web-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        File.WriteAllBytes(Path.Combine(_dir, "a.txt"), new byte[1500]);
        File.WriteAllBytes(Path.Combine(_dir, "b.bin"), new byte[3000]);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public async Task Healthz_is_ok()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/healthz");
        response.EnsureSuccessStatusCode();
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ok", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Api_scan_returns_totals_for_a_directory()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/scan?path={Uri.EscapeDataString(_dir)}");
        response.EnsureSuccessStatusCode();

        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(4500, root.GetProperty("totalBytes").GetInt64());
        Assert.Equal(2, root.GetProperty("files").GetInt32());
        Assert.Equal("Binary", root.GetProperty("standard").GetString());
        // Largest file first.
        Assert.Equal("b.bin", root.GetProperty("largestFiles")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Api_scan_decimal_units_change_the_human_string()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/scan?path={Uri.EscapeDataString(_dir)}&standard=Decimal");
        response.EnsureSuccessStatusCode();
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Decimal", root.GetProperty("standard").GetString());
        Assert.Contains("KB", root.GetProperty("totalHuman").GetString());
    }

    [Fact]
    public async Task Api_scan_without_path_is_400()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/scan");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Api_scan_missing_directory_is_400()
    {
        var client = _factory.CreateClient();
        var missing = Path.Combine(_dir, "nope");
        var response = await client.GetAsync($"/api/scan?path={Uri.EscapeDataString(missing)}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Home_page_renders()
    {
        var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/");
        Assert.Contains("Disk usage explorer", html);
    }

    [Fact]
    public async Task Scan_page_shows_results()
    {
        var client = _factory.CreateClient();
        var html = await client.GetStringAsync($"/scan?path={Uri.EscapeDataString(_dir)}");
        Assert.Contains("Largest files", html);
        Assert.Contains("b.bin", html);
    }
}
