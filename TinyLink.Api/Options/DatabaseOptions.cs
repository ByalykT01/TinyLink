using System.ComponentModel.DataAnnotations;
using Npgsql;

namespace TinyLink.Api.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    [Required] public required string Host { get; init; }
    [Range(1, 65535)] public required int Port { get; init; }
    [Required] public required string Name { get; init; }
    [Required] public required string User { get; init; }
    [Required] public required string Password { get; init; }

    public string ToConnectionString =>
        new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Database = Name,
            Username = User,
            Password = Password
        }.ConnectionString;
}
