using TinyLink.Api.Extensions;
using TinyLink.Api.Features.Links;

var builder = WebApplication.CreateBuilder(args);

builder.AddForwardedHeaders();
builder.AddDocumentation();
builder.AddErrorHandling();
builder.AddOptions();
builder.AddPersistence();
builder.AddServices();
builder.Services.AddTinyLinkRateLimiting(builder.Configuration);

var app = builder.Build();

await app.MigrateDatabaseAsync();

app.UseForwardedHeadersFromConfig();
app.UseErrorHandling();
app.UseScalarWithDefaults();
app.UseRateLimiter();

app.MapHealthChecks("/healthz");
app.MapLinkEndpoints();

await app.RunAsync();
