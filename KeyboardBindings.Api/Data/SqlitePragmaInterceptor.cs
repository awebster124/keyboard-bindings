using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KeyboardBindings.Api.Data;

/// <summary>
/// Applies SQLite PRAGMAs on every connection open: <c>journal_mode=WAL</c> (readers don't block the writer) and
/// <c>busy_timeout</c> (wait for a lock instead of failing immediately with SQLITE_BUSY).
/// </summary>
public sealed class SqlitePragmaInterceptor : DbConnectionInterceptor
{
    private const string Pragmas =
        "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        => Execute(connection);

    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        => await ExecuteAsync(connection, cancellationToken);

    private static void Execute(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = Pragmas;
        command.ExecuteNonQuery();
    }

    private static async Task ExecuteAsync(DbConnection connection, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = Pragmas;
        await command.ExecuteNonQueryAsync(ct);
    }
}
