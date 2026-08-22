using MySqlConnector;

namespace SuperPowersPlugin.Utils;

public class MySqlDatabaseProvider : IDatabaseProvider
{
    private readonly string _connectionString;
    private MySqlDataSource? _dbDataSource;
    private readonly object _lock = new();

    public MySqlDataSource? DataSource => _dbDataSource;

    public MySqlDatabaseProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void Initialize()
    {
        _dbDataSource = new MySqlDataSourceBuilder(_connectionString).Build();

        lock (_lock)
        {
            using var connection = _dbDataSource.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS player_data (
                    steam_id BIGINT PRIMARY KEY,
                    last_updated DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS player_attributes (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    steam_id BIGINT NOT NULL,
                    attribute_key VARCHAR(255) NOT NULL,
                    attribute_value LONGTEXT NOT NULL,
                    attribute_type VARCHAR(50) NOT NULL,
                    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    UNIQUE KEY unique_steam_attribute (steam_id, attribute_key),
                    FOREIGN KEY (steam_id) REFERENCES player_data(steam_id) ON DELETE CASCADE
                );
            ";
            cmd.ExecuteNonQuery();
        }
    }

    public void EnsurePlayerExists(ulong steamId64)
    {
        if (_dbDataSource == null) return;
        lock (_lock)
        {
            using var connection = _dbDataSource.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT IGNORE INTO player_data (steam_id) VALUES (@steamId)";
            cmd.Parameters.AddWithValue("@steamId", steamId64);
            _ = cmd.ExecuteNonQuery();
        }
    }

    public void PersistAttribute(ulong steamId64, string key, string valueJson, string valueType)
    {
        if (_dbDataSource == null) return;
        EnsurePlayerExists(steamId64);
        lock (_lock)
        {
            using var connection = _dbDataSource.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO player_attributes (steam_id, attribute_key, attribute_value, attribute_type)
                VALUES (@steamId, @key, @value, @type)
                ON DUPLICATE KEY UPDATE
                    attribute_value = @value,
                    attribute_type = @type,
                    updated_at = CURRENT_TIMESTAMP
            ";
            cmd.Parameters.AddWithValue("@steamId", steamId64);
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@value", valueJson);
            cmd.Parameters.AddWithValue("@type", valueType);
            _ = cmd.ExecuteNonQuery();
        }
    }

    public void RemoveAttribute(ulong steamId64, string key)
    {
        if (_dbDataSource == null) return;
        lock (_lock)
        {
            using var connection = _dbDataSource.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM player_attributes WHERE steam_id = @steamId AND attribute_key = @key";
            cmd.Parameters.AddWithValue("@steamId", steamId64);
            cmd.Parameters.AddWithValue("@key", key);
            _ = cmd.ExecuteNonQuery();
        }
    }

    public void RemovePlayer(ulong steamId64)
    {
        if (_dbDataSource == null) return;
        lock (_lock)
        {
            using var connection = _dbDataSource.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM player_data WHERE steam_id = @steamId";
            cmd.Parameters.AddWithValue("@steamId", steamId64);
            _ = cmd.ExecuteNonQuery();
        }
    }

    public List<(string Key, string Value, string Type)> LoadAttributes(ulong steamId64)
    {
        if (_dbDataSource == null) return [];
        lock (_lock)
        {
            using var connection = _dbDataSource.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT attribute_key, attribute_value, attribute_type
                FROM player_attributes
                WHERE steam_id = @steamId
            ";
            cmd.Parameters.AddWithValue("@steamId", steamId64);

            var results = new List<(string, string, string)>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                results.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
            return results;
        }
    }

    public List<ulong> GetAllPlayerSteamIds()
    {
        if (_dbDataSource == null) return [];
        lock (_lock)
        {
            using var connection = _dbDataSource.OpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT steam_id FROM player_data";
            var ids = new List<ulong>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                ids.Add(reader.GetUInt64(0));
            return ids;
        }
    }

    public void Close()
    {
        _dbDataSource?.Dispose();
    }
}
