using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
namespace TinyLink.Api.Extensions;

internal sealed class ClientDisconnectedExceptionHandler(
    ILogger<ClientDisconnectedExceptionHandler> logger) : IExceptionHandler
{
    private const int _clientClosedRequest = 499;
    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not OperationCanceledException || !httpContext.RequestAborted.IsCancellationRequested)
            return ValueTask.FromResult(false);
        ErrorLog.ClientDisconnected(logger, httpContext.Request.Path);
        if (!httpContext.Response.HasStarted)
            httpContext.Response.StatusCode = _clientClosedRequest;
        return ValueTask.FromResult(true);
    }
}
internal sealed class DatabaseExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<DatabaseExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (httpContext.Response.HasStarted)
            return false;
        var problem = Classify(exception);
        if (problem is null)
            return false;
        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
    }
    private ProblemDetails? Classify(Exception exception)
    {
        var root = exception.GetBaseException();
        if (root is PostgresException pg)
        {
            switch (pg.SqlState)
            {
                case PostgresErrorCodes.UniqueViolation:
                    ErrorLog.UniqueViolation(logger, pg.ConstraintName ?? "unknown");
                    return Problem(StatusCodes.Status409Conflict,
                        "Conflict", "That resource already exists.");
                case "2200H": //not in PostgresErrorCodes
                    ErrorLog.SequenceExhausted(logger);
                    return Problem(StatusCodes.Status503ServiceUnavailable,
                        "Short code space exhausted", "No further short codes can be issued.");
            }
            return null;
        }
        if (root is NpgsqlException or TimeoutException)
        {
            ErrorLog.DatabaseUnavailable(logger, root);
            return Problem(StatusCodes.Status503ServiceUnavailable,
                "Database unavailable", "Try again shortly.");
        }
        return null;
    }
    private static ProblemDetails Problem(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };
}

