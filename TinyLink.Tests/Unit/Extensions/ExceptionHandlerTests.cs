using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using TinyLink.Api.Extensions;
using Xunit;

namespace TinyLink.Tests.Unit.Extensions;

public sealed class ExceptionHandlerTests
{
    [Fact]
    public async Task ClientDisconnected_NonCancellation_ReturnsFalse()
    {
        var handler = new ClientDisconnectedExceptionHandler(NullLogger<ClientDisconnectedExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(
            httpContext, new InvalidOperationException("boom"), CancellationToken.None);

        handled.Should().BeFalse();
    }

    [Fact]
    public async Task ClientDisconnected_CancellationWithoutAbort_ReturnsFalse()
    {
        var handler = new ClientDisconnectedExceptionHandler(NullLogger<ClientDisconnectedExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(
            httpContext, new OperationCanceledException(), CancellationToken.None);

        handled.Should().BeFalse();
    }

    [Fact]
    public async Task ClientDisconnected_AbortedRequest_ReturnsTrueWith499()
    {
        var handler = new ClientDisconnectedExceptionHandler(NullLogger<ClientDisconnectedExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.RequestAborted = new CancellationToken(canceled: true);

        var handled = await handler.TryHandleAsync(
            httpContext, new OperationCanceledException(), CancellationToken.None);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(499);
    }

    [Fact]
    public async Task Database_UniqueViolation_ReturnsTrueWith409()
    {
        var (handler, httpContext) = CreateDatabaseHandler();
        var exception = new PostgresException("duplicate key", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation);

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task Database_SequenceExhausted_ReturnsTrueWith503()
    {
        var (handler, httpContext) = CreateDatabaseHandler();
        var exception = new PostgresException("sequence exhausted", "FATAL", "FATAL", "2200H");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task Database_TransientFailure_ReturnsTrueWith503()
    {
        var (handler, httpContext) = CreateDatabaseHandler();

        var handled = await handler.TryHandleAsync(
            httpContext, new TimeoutException("connection timed out"), CancellationToken.None);

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task Database_UnknownPostgresError_ReturnsFalse()
    {
        var (handler, httpContext) = CreateDatabaseHandler();
        var exception = new PostgresException("not null", "ERROR", "ERROR", PostgresErrorCodes.NotNullViolation);

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.Should().BeFalse();
    }

    [Fact]
    public async Task Database_NonDatabaseError_ReturnsFalse()
    {
        var (handler, httpContext) = CreateDatabaseHandler();

        var handled = await handler.TryHandleAsync(
            httpContext, new InvalidOperationException("boom"), CancellationToken.None);

        handled.Should().BeFalse();
    }

    private static (DatabaseExceptionHandler Handler, DefaultHttpContext HttpContext) CreateDatabaseHandler()
    {
        var services = new ServiceCollection();
        services.AddProblemDetails();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var handler = new DatabaseExceptionHandler(
            provider.GetRequiredService<Microsoft.AspNetCore.Http.IProblemDetailsService>(),
            NullLogger<DatabaseExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider,
            Response = { Body = new MemoryStream() }
        };
        return (handler, httpContext);
    }
}
