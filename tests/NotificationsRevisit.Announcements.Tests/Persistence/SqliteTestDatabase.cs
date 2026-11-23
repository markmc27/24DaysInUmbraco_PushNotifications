using Microsoft.Data.Sqlite;
using NPoco;
using NotificationsRevisit.Announcements.Persistence;

namespace NotificationsRevisit.Announcements.Tests.Persistence;

/// <summary>
/// An in-memory SQLite database with the extension's tables, for testing the repository SQL with NPoco.
/// The schema mirrors what the migration creates, including the unique endpoint index.
/// </summary>
internal sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        Database = new Database(_connection, DatabaseType.SQLite);

        Database.Execute($"""
            CREATE TABLE {PushSubscriptionDto.TableName} (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Endpoint TEXT NOT NULL,
                P256dh TEXT NOT NULL,
                Auth TEXT NOT NULL,
                CreatedUtc TEXT NOT NULL);
            CREATE UNIQUE INDEX IX_{PushSubscriptionDto.TableName}_Endpoint ON {PushSubscriptionDto.TableName} (Endpoint);
            CREATE TABLE {PushedContentDto.TableName} (
                ContentKey TEXT NOT NULL PRIMARY KEY,
                SentUtc TEXT NOT NULL);
            """);
    }

    public IDatabase Database { get; }

    public void Dispose()
    {
        Database.Dispose();
        _connection.Dispose();
    }
}
