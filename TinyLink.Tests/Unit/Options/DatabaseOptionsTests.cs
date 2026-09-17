using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Npgsql;
using TinyLink.Api.Options;
using Xunit;

namespace TinyLink.Tests.Unit.Options;

public sealed class DatabaseOptionsTests
{
    [Theory]
    [InlineData("ordinary-password")]
    [InlineData("secret;Host=attacker;Pooling=false")]
    [InlineData("a\"quoted'password;with=values")]
    public void ConnectionString_PreservesPasswordWithoutChangingSettings(string password)
    {
        var options = CreateOptions(password, 5432);

        var parsed = new NpgsqlConnectionStringBuilder(options.ToConnectionString);

        parsed.Password.Should().Be(password);
        parsed.Host.Should().Be("localhost");
        parsed.Database.Should().Be("tinylink");
        parsed.Username.Should().Be("app");
        parsed.Pooling.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void InvalidPort_FailsValidation(int port)
    {
        var options = CreateOptions("secret", port);

        Validator.TryValidateObject(options, new ValidationContext(options), [], true)
            .Should().BeFalse();
    }

    private static DatabaseOptions CreateOptions(string password, int port) => new()
    {
        Host = "localhost", Port = port, Name = "tinylink", User = "app", Password = password
    };
}
