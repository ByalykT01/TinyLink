using TinyLink.Api.Extensions;
using TinyLink.Api.Features.Links;

var builder = WebApplication.CreateBuilder(args);

builder.AddDocumentation();
builder.AddOptions();
builder.AddPersistence();
builder.AddServices();
builder.Services.AddTinyLinkRateLimiting(builder.Configuration);

var app = builder.Build();

await app.MigrateDatabaseAsync();
app.UseScalarWithDefaults();
app.UseHsts();
app.UseHttpsRedirection();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/healthz");
app.MapLinkEndpoints();

await app.RunAsync();
