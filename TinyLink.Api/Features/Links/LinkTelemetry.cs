using System.Diagnostics;

namespace TinyLink.Api.Features.Links;

// Name must match AddSource in ObservabilityExtensions. Tags stay
// low-cardinality: never attach short codes, target URLs or tokens.
internal static class LinkTelemetry
{
    public static readonly ActivitySource Source = new("TinyLink.Api");
}
