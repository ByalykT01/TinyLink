using System.Text.Encodings.Web;

namespace TinyLink.Api.Features.Links;

/// <summary>
/// Bare statuses stay bare for machines, but a browser navigating to a dead
/// short link would otherwise render a blank page. When the request accepts
/// HTML, these pages give the 410/404 a body — status codes and cache headers
/// are unchanged, so correctness and caching behave exactly as before.
/// </summary>
internal static class StatusPages
{
    public static string Gone(string? frontendOrigin) =>
        Page(
            "This link is gone.",
            "It was deleted or it expired.",
            frontendOrigin);

    public static string NotFound(string? frontendOrigin) =>
        Page(
            "No link with this code.",
            "Check the code and try again.",
            frontendOrigin);

    private static string Page(string heading, string detail, string? frontendOrigin)
    {
        var encoder = HtmlEncoder.Default;
        var link = frontendOrigin is { Length: > 0 }
            ? $"<p><a href=\"{encoder.Encode(frontendOrigin)}\">Shorten a URL</a></p>"
            : string.Empty;
        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{{encoder.Encode(heading)}}</title>
            <style>body{margin:0;background:#fff;color:#1a1a1a;font:16px/1.6 -apple-system,"Segoe UI",Roboto,sans-serif}main{max-width:520px;margin:0 auto;padding:32px 20px}h1{font-size:18px}p.sub{color:#666}a{color:#1a73e8}</style>
            </head>
            <body><main><h1>{{encoder.Encode(heading)}}</h1><p class="sub">{{encoder.Encode(detail)}}</p>{{link}}</main></body>
            </html>
            """;
    }
}
