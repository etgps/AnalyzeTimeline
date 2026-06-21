using AnalyzeTimeline.Api;
using AnalyzeTimeline.Application;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

const long MaxUploadBytes = 512L * 1024L * 1024L;

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = MaxUploadBytes;
});

builder.Services.AddScoped<ITimelineAnalyzer, TimelineAnalyzer>();
builder.Services.AddScoped<IReportHtmlGenerator, ReportHtmlGenerator>();
builder.Services.AddSingleton<ICountryResolver, GeoJsonCountryResolver>();
builder.Services.AddSingleton<IPrefectureResolver, GeoJsonPrefectureResolver>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = MaxUploadBytes;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = 64 * 1024;
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/timeline/analyze", async (
    IFormFile file,
    ITimelineAnalyzer analyzer,
    IReportHtmlGenerator reportGenerator,
    CancellationToken cancellationToken) =>
{
    if (file.Length > MaxUploadBytes)
    {
        return Results.BadRequest(new { message = "512MB 以下の Timeline.json を選択してください。" });
    }

    if (file.Length == 0)
    {
        return Results.BadRequest(new { message = "Timeline.json を選択してください。" });
    }

    if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { message = "JSON ファイルのみアップロードできます。" });
    }

    await using var stream = file.OpenReadStream();
    var result = await analyzer.AnalyzeAsync(stream, cancellationToken);
    var html = reportGenerator.Generate(result);

    return Results.Ok(new AnalyzeResponse(
        result.ParsedLocationCount,
        result.ClassifiedLocationCount,
        result.YearlyVisits.Count,
        result.MonthlyVisits.Count,
        html));
})
.DisableAntiforgery()
.WithName("AnalyzeTimeline")
.WithSummary("Analyze a Google Timeline JSON export and return a downloadable HTML report.");

app.MapFallbackToFile("index.html");

app.Run();
