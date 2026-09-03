using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TinyLink.Api.Extensions;
using TinyLink.Api.Options;
using Xunit;

namespace TinyLink.Tests.Unit.Options;

public sealed class LinkCleanupOptionsTests
{
    [Theory]
    [InlineData("00:00:00", "7.00:00:00")]
    [InlineData("01:00:00", "00:00:00")]
    [InlineData("00:00:00", "00:00:00")]
    public void Registration_WithNonPositiveValues_FailsValidation(string interval, string retention)
    {
        var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["LinkCleanup:Interval"] = interval,
            ["LinkCleanup:Retention"] = retention
        });

        var act = () => factory.Create(Microsoft.Extensions.Options.Options.DefaultName);

        act.Should().Throw<OptionsValidationException>();
    }

    private static IOptionsFactory<LinkCleanupOptions> CreateFactory(
        Dictionary<string, string?> values)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection(
            ValidDatabaseSettings().Concat(values).ToDictionary(kv => kv.Key, kv => kv.Value));
        builder.AddOptions();
        return builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptionsFactory<LinkCleanupOptions>>();
    }

    private static Dictionary<string, string?> ValidDatabaseSettings() => new()
    {
        ["Database:Host"] = "localhost",
        ["Database:Port"] = "5432",
        ["Database:Name"] = "tinylink",
        ["Database:User"] = "tinylink",
        ["Database:Password"] = "secret"
    };
}
