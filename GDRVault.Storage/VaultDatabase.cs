using Microsoft.Data.Sqlite;

namespace GDRVault.Storage;

public class VaultDatabase
{
    private readonly string _connectionString;

    public VaultDatabase(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException(
                "Il percorso del database non può essere vuoto.",
                nameof(databasePath));

        string? directory =
            Path.GetDirectoryName(databasePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString =
            $"Data Source={databasePath}";
    }

    public async Task InitializeAsync()
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Vault (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                Version INTEGER NOT NULL,
                Data BLOB NOT NULL,
                CreatedAt TEXT NOT NULL,
                ModifiedAt TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync();
    }

    public async Task SaveAsync(
        byte[] encryptedData,
        int version,
        DateTime createdAt,
        DateTime modifiedAt)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);

        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO Vault
                (Id, Version, Data, CreatedAt, ModifiedAt)
            VALUES
                (1, $version, $data, $createdAt, $modifiedAt)
            ON CONFLICT(Id) DO UPDATE SET
                Version = excluded.Version,
                Data = excluded.Data,
                CreatedAt = excluded.CreatedAt,
                ModifiedAt = excluded.ModifiedAt;
            """;

        command.Parameters.AddWithValue(
            "$version",
            version);

        command.Parameters.Add(
            "$data",
            SqliteType.Blob).Value = encryptedData;

        command.Parameters.AddWithValue(
            "$createdAt",
            createdAt.ToString("O"));

        command.Parameters.AddWithValue(
            "$modifiedAt",
            modifiedAt.ToString("O"));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<byte[]?> LoadAsync()
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT Data
            FROM Vault
            WHERE Id = 1;
            """;

        var result =
            await command.ExecuteScalarAsync();

        if (result is null ||
            result is DBNull)
        {
            return null;
        }

        return (byte[])result;
    }

    public async Task<bool> ExistsAsync()
    {
        await using var connection =
            new SqliteConnection(_connectionString);

        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT COUNT(*)
            FROM Vault
            WHERE Id = 1;
            """;

        var result =
            await command.ExecuteScalarAsync();

        return Convert.ToInt32(result) > 0;
    }
}