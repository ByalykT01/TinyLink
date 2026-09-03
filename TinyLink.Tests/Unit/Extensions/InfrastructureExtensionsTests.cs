using FluentAssertions;
using Microsoft.Extensions.Hosting;
using TinyLink.Api.Extensions;
using Xunit;

namespace TinyLink.Tests.Unit.Extensions;

public sealed class InfrastructureExtensionsTests
{
    [Fact]
    public void AddObservability_RegistersWithoutThrowing()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var result = builder.AddObservability();

        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddServices_MissingShortCodeKey_Throws()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var act = () => builder.AddServices();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ShortCodes:Key*");
    }
}
