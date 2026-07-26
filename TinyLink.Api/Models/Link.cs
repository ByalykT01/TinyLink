namespace TinyLink.Api.Models;

public class Link
{
    public long Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
