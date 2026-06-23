using System.IO;
using Microsoft.Data.Sqlite;
using WriterWorkbench.Core.Documents;

namespace WriterWorkbench.Core.Storage;

public sealed class SqliteProjectIndex
{
    public SqliteProjectIndex(string databasePath)
    {
        DatabasePath = databasePath;
    }

    public string DatabasePath { get; }

    public async Task InitializeAsync(CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        await using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        await connection.OpenAsync(token);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS documents (
                id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                plain_text TEXT NOT NULL,
                updated_utc TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(token);
    }

    public async Task UpsertDocumentAsync(
        WriterDocument document,
        string plainText,
        DateTimeOffset updatedAt,
        CancellationToken token)
    {
        await InitializeAsync(token);
        await using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        await connection.OpenAsync(token);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO documents (id, title, plain_text, updated_utc)
            VALUES ($id, $title, $plainText, $updatedUtc)
            ON CONFLICT(id) DO UPDATE SET
                title = excluded.title,
                plain_text = excluded.plain_text,
                updated_utc = excluded.updated_utc;
            """;
        command.Parameters.AddWithValue("$id", document.Id);
        command.Parameters.AddWithValue("$title", document.Title);
        command.Parameters.AddWithValue("$plainText", plainText);
        command.Parameters.AddWithValue("$updatedUtc", updatedAt.UtcDateTime.ToString("O"));
        await command.ExecuteNonQueryAsync(token);
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(string query, CancellationToken token)
    {
        await InitializeAsync(token);
        await using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        await connection.OpenAsync(token);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, title, plain_text
            FROM documents
            WHERE title LIKE $query OR plain_text LIKE $query
            ORDER BY updated_utc DESC
            LIMIT 50;
            """;
        command.Parameters.AddWithValue("$query", $"%{query}%");

        var hits = new List<SearchHit>();
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            var id = reader.GetString(0);
            var title = reader.GetString(1);
            var plainText = reader.GetString(2);
            hits.Add(new SearchHit(id, title, CreateSnippet(plainText, query)));
        }

        return hits;
    }

    public async Task DeleteDocumentAsync(string documentId, CancellationToken token)
    {
        await InitializeAsync(token);
        await using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        await connection.OpenAsync(token);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM documents WHERE id = $id;";
        command.Parameters.AddWithValue("$id", documentId);
        await command.ExecuteNonQueryAsync(token);
    }

    private static string CreateSnippet(string plainText, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return plainText.Length <= 80 ? plainText : plainText[..80];
        }

        var index = plainText.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return plainText.Length <= 80 ? plainText : plainText[..80];
        }

        var start = Math.Max(0, index - 30);
        var length = Math.Min(plainText.Length - start, query.Length + 60);
        return plainText.Substring(start, length);
    }
}
