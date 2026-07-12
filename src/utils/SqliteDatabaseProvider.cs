using Microsoft.Data.Sqlite;

namespace SuperPowersPlugin.Utils;

public class SqliteDatabaseProvider : IDatabaseProvider
{
    private readonly string _dbPath;
    private readonly object _lock = new();

    public SqliteDatabaseProvider(string dbPath)
    {
        _dbPath = dbPath;
    }

    private SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }

    public void Initialize()
    {
        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        lock (_lock)
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS player_data (
                    steam_id INTEGER PRIMARY KEY,
                    last_updated TEXT DEFAULT (datetime('now'))
                );

                CREATE TABLE IF NOT EXISTS player_attributes (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    steam_id INTEGER NOT NULL,
                    attribute_key TEXT NOT NULL,
                    attribute_value TEXT NOT NULL,
                    attribute_type TEXT NOT NULL,
                    updated_at TEXT DEFAULT (datetime('now')),
                    UNIQUE(steam_id, attribute_key),
                    FOREIGN KEY (steam_id) REFERENCES player_data(steam_id) ON DELETE CASCADE
                );
            ";
            cmd.ExecuteNonQuery();
        }
    }

    public void EnsurePlayerExists(ulong steamId64)
    {
        lock (_lock)
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO player_data (steam_id) VALUES (@steamId)";
            cmd.Parameters.AddWithValue("@steamId", (long)steamId64);
            _ = cmd.ExecuteNonQuery();
        }
    }

    public void PersistAttribute(ulong steamId64, string key, string valueJson, string valueType)
    {
        EnsurePlayerExists(steamId64);
        lock (_lock)
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO player_attributes (steam_id, attribute_key, attribute_value, attribute_type)
                VALUES (@steamId, @key, @value, @type)
                ON CONFLICT(steam_id, attribute_key) DO UPDATE SET
                    attribute_value = excluded.attribute_value,
                    attribute_type = excluded.attribute_type,
                    updated_at = datetime('now')
            ";
            cmd.Parameters.AddWithValue("@steamId", (long)steamId64);
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@value", valueJson);
            cmd.Parameters.AddWithValue("@type", valueType);
            _ = cmd.ExecuteNonQuery();
        }
    }

    public void RemoveAttribute(ulong steamId64, string key)
    {
        lock (_lock)
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM player_attributes WHERE steam_id = @steamId AND attribute_key = @key";
            cmd.Parameters.AddWithValue("@steamId", (long)steamId64);
            cmd.Parameters.AddWithValue("@key", key);
            _ = cmd.ExecuteNonQuery();
        }
    }

    public void RemovePlayer(ulong steamId64)
    {
        lock (_lock)
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM player_data WHERE steam_id = @steamId";
            cmd.Parameters.AddWithValue("@steamId", (long)steamId64);
            _ = cmd.ExecuteNonQuery();
        }
    }

    public List<(string Key, string Value, string Type)> LoadAttributes(ulong steamId64)
    {
        lock (_lock)
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT attribute_key, attribute_value, attribute_type
                FROM player_attributes
                WHERE steam_id = @steamId
            ";
            cmd.Parameters.AddWithValue("@steamId", (long)steamId64);

            var results = new List<(string, string, string)>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                results.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
            return results;
        }
    }

    public List<ulong> GetAllPlayerSteamIds()
    {
        lock (_lock)
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT steam_id FROM player_data";
            var ids = new List<ulong>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                ids.Add((ulong)reader.GetInt64(0));
            return ids;
        }
    }

    public void Close()
    {
        // SQLite doesn't need connection pool cleanup
    }
}
