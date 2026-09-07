using TinyLink.Api.Extensions;
using TinyLink.Api.Features.Links;

var builder = WebApplication.CreateBuilder(args);

builder.AddForwardedHeaders();
builder.AddFrontendCors();
builder.AddDocumentation();
builder.AddErrorHandling();
builder.AddOptions();
builder.AddPersistence();
builder.AddServices();
builder.AddObservability();
builder.Services.AddTinyLinkRateLimiting(builder.Configuration);

var app = builder.Build();

await app.MigrateDatabaseAsync();

app.UseForwardedHeadersFromConfig();
app.UseErrorHandling();
app.UseScalarWithDefaults();
app.UseCors(CorsExtensions.FrontendPolicy);
app.UseRateLimiter();

app.MapHealthChecks("/healthz");
app.MapLinkEndpoints();

// Single-origin deployment: the compiled SPA lives in wwwroot (populated by
// `npm run build` locally, by the Dockerfile in production). Explicit API
// routes above always win — including GET /{code}, which must 302 rather than
// render the SPA — and every other path falls back to index.html.
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

await app.RunAsync();
