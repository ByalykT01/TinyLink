using TinyLink.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddDocumentation();
builder.AddAppOptions();
builder.AddPersistence();
builder.AddServices();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();

var app = builder.Build();

app.UseScalarWithDefaults();
app.UseHttpsRedirection();
app.UseHsts();

app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();
