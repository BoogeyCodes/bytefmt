using ByteFmt;
using ByteFmt.DiskScan;
using ByteFmt.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<DirectoryScanner>();
var app = builder.Build();

static UnitStandard ParseStandard(string? s) =>
    string.Equals(s, "Decimal", StringComparison.OrdinalIgnoreCase) ? UnitStandard.Decimal : UnitStandard.Binary;

// Home: the scan form.
app.MapGet("/", () => Results.Content(HtmlView.Dashboard(null, null, "Binary", null), "text/html; charset=utf-8"));

// HTML results.
app.MapGet("/scan", (DirectoryScanner scanner, string? path, string? standard, int? top) =>
{
    var std = ParseStandard(standard);
    if (string.IsNullOrWhiteSpace(path))
    {
        return Results.Content(HtmlView.Dashboard(null, path, std.ToString(), "Enter a directory path to scan."),
            "text/html; charset=utf-8");
    }

    try
    {
        var result = scanner.Scan(path, new ScanOptions { Top = top ?? 10 });
        var response = ScanPresenter.ToResponse(result, std);
        return Results.Content(HtmlView.Dashboard(response, path, std.ToString(), null), "text/html; charset=utf-8");
    }
    catch (Exception e) when (e is DirectoryNotFoundException or ArgumentException or UnauthorizedAccessException)
    {
        return Results.Content(HtmlView.Dashboard(null, path, std.ToString(), e.Message), "text/html; charset=utf-8");
    }
});

// JSON API.
app.MapGet("/api/scan", (DirectoryScanner scanner, string? path, string? standard, int? top) =>
{
    if (string.IsNullOrWhiteSpace(path))
    {
        return Results.BadRequest(new { error = "a 'path' query parameter is required" });
    }

    try
    {
        var result = scanner.Scan(path, new ScanOptions { Top = top ?? 10 });
        return Results.Ok(ScanPresenter.ToResponse(result, ParseStandard(standard)));
    }
    catch (Exception e) when (e is DirectoryNotFoundException or ArgumentException)
    {
        return Results.BadRequest(new { error = e.Message });
    }
    catch (UnauthorizedAccessException e)
    {
        return Results.Json(new { error = e.Message }, statusCode: StatusCodes.Status403Forbidden);
    }
});

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.Run();

// Exposed so the integration-test project can boot the app with WebApplicationFactory.
public partial class Program;
