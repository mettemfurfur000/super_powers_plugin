using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using MySqlConnector;

namespace SuperPowersPlugin.Utils
{
    /// <summary>
    /// Static storage class for custom player data using SteamID64 as keys.
    /// Supports database serialization and mutations through references.
    /// Integrates MySQL persistence for all attribute changes.
    /// </summary>
    public static class CustomStorage
    {
        private static readonly Dictionary<ulong, PlayerData> PlayerDataMap = new();
        private static readonly object _dbLock = new object();

        public static MySqlDataSource? _dbDataSource;
        public static bool _dbInitialized = false;
        public static string _connectionString = "";

        /// <summary>
        /// Initializes the MySQL database connection pool and creates required tables.
        /// </summary>
        public static void InitializeDatabase(string connectionString)
        {
            _connectionString = connectionString;

            // Use MySqlDataSource for connection pooling
            _dbDataSource = new MySqlDataSourceBuilder(_connectionString).Build();

            lock (_dbLock)
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

            _dbInitialized = true;
        }

        /// <summary>
        /// Gets or creates player data for a given SteamID64.
        /// Returns a reference allowing direct mutations.
        /// If database is initialized and player data exists there but not in memory, loads it.
        /// </summary>
        public static PlayerData GetOrCreatePlayerData(ulong steamId64)
        {
            if (!PlayerDataMap.TryGetValue(steamId64, out var data))
            {
                data = new PlayerData { SteamId64 = steamId64 };
                PlayerDataMap[steamId64] = data;

                // Ensure player exists in database
                if (_dbInitialized)
                {
                    EnsurePlayerInDatabase(steamId64);
                }
            }
            return data;
        }

        public static PlayerData GetOrCreatePlayerData(CCSPlayerController controller)
        {
            return GetOrCreatePlayerData(controller.SteamID);
        }

        /// <summary>
        /// Gets or creates and loads player data for a given SteamID64.
        /// If the player exists in the database, their attributes are loaded.
        /// Call this when a player connects to restore their previous data.
        /// </summary>
        public static PlayerData GetOrLoadPlayerData(ulong steamId64)
        {
            if (PlayerDataMap.TryGetValue(steamId64, out var existing))
            {
                return existing;
            }

            if (_dbInitialized)
            {
                LoadPlayerDataFromDatabase(steamId64);
            }

            // Return from memory (either loaded or newly created)
            return GetOrCreatePlayerData(steamId64);
        }

        /// <summary>
        /// Gets or creates and loads player data for a given player controller.
        /// Call this when a player connects to restore their previous data.
        /// </summary>
        public static PlayerData GetOrLoadPlayerData(CCSPlayerController controller)
        {
            return GetOrLoadPlayerData(controller.SteamID);
        }

        [GameEventHandler]
        public static HookResult OnPlayerConnect(EventPlayerConnectFull @event)
        {
            GetOrLoadPlayerData(@event.Userid!);
            return HookResult.Continue;
        }

        public static void RemovePlayerData(ulong steamId64)
        {
            PlayerDataMap.Remove(steamId64);

            // Remove from database
            if (_dbInitialized)
            {
                RemovePlayerFromDatabase(steamId64);
            }
        }

        public static void RemovePlayerData(CCSPlayerController controller)
        {
            RemovePlayerData(controller.SteamID);
        }

        /// <summary>
        /// Persists a player attribute to the database.
        /// </summary>
        public static void PersistAttributeToDatabase(ulong steamId64, string key, object? value)
        {
            if (!_dbInitialized || _dbDataSource == null)
                return;

            // Ensure player exists in database first
            EnsurePlayerInDatabase(steamId64);

            string valueJson = JsonSerializer.Serialize(value);
            string valueType = value?.GetType().Name ?? "null";

            lock (_dbLock)
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

        /// <summary>
        /// Ensures a player record exists in the database.
        /// </summary>
        private static void EnsurePlayerInDatabase(ulong steamId64)
        {
            if (!_dbInitialized || _dbDataSource == null)
                return;

            lock (_dbLock)
            {
                using var connection = _dbDataSource.OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                            INSERT IGNORE INTO player_data (steam_id)
                            VALUES (@steamId)
                        ";

                cmd.Parameters.AddWithValue("@steamId", steamId64);
                _ = cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Removes a player from the database.
        /// </summary>
        private static void RemovePlayerFromDatabase(ulong steamId64)
        {
            if (!_dbInitialized || _dbDataSource == null)
                return;

            lock (_dbLock)
            {
                using var connection = _dbDataSource.OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM player_data WHERE steam_id = @steamId";
                cmd.Parameters.AddWithValue("@steamId", steamId64);

                _ = cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Gets all stored player data (read-only snapshot).
        /// </summary>
        public static IReadOnlyDictionary<ulong, PlayerData> GetAllPlayerData()
        {
            return new Dictionary<ulong, PlayerData>(PlayerDataMap);
        }

        /// <summary>
        /// Clears all stored data.
        /// </summary>
        public static void Clear()
        {
            PlayerDataMap.Clear();
        }

        /// <summary>
        /// Closes the database connection pool gracefully.
        /// </summary>
        public static void CloseDatabase()
        {
            if (_dbDataSource != null)
            {
                _dbDataSource.Dispose();
                Console.WriteLine("Database connection pool closed.");
            }
        }

        public static void RemoveAttributeFromDatabase(ulong steamId64, string key)
        {
            if (!_dbInitialized || _dbDataSource == null)
                return;

            lock (_dbLock)
            {
                using var connection = _dbDataSource.OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM player_attributes WHERE steam_id = @steamId AND attribute_key = @key";
                cmd.Parameters.AddWithValue("@steamId", steamId64);
                cmd.Parameters.AddWithValue("@key", key);

                _ = cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Loads player data from the database for a specific Steam ID.
        /// This should be called when a player connects to restore their saved attributes.
        /// </summary>
        public static void LoadPlayerDataFromDatabase(ulong steamId64)
        {
            if (!_dbInitialized || _dbDataSource == null)
                return;

            // Skip if already loaded
            if (PlayerDataMap.ContainsKey(steamId64))
                return;

            Dictionary<string, object> attributes = new();

            lock (_dbLock)
            {
                using var connection = _dbDataSource.OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                            SELECT attribute_key, attribute_value, attribute_type 
                            FROM player_attributes 
                            WHERE steam_id = @steamId
                        ";
                cmd.Parameters.AddWithValue("@steamId", steamId64);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string key = reader.GetString(0);
                    string valueJson = reader.GetString(1);
                    string typeName = reader.GetString(2);

                    // Attempt to deserialize based on type name
                    object? value = DeserializeAttributeValue(valueJson, typeName);
                    if (value != null)
                    {
                        attributes[key] = value;
                    }
                }
            }

            if (attributes.Count > 0)
            {
                var playerData = new PlayerData { SteamId64 = steamId64, CustomAttributes = attributes };
                PlayerDataMap[steamId64] = playerData;
            }
            else
            {
                // Create empty player data if no attributes found
                if (!PlayerDataMap.ContainsKey(steamId64))
                {
                    PlayerDataMap[steamId64] = new PlayerData { SteamId64 = steamId64 };
                }
            }
        }

        /// <summary>
        /// Loads all player data from the database into memory.
        /// Call this after InitializeDatabaseAsync to restore previous session data.
        /// </summary>
        public static void LoadAllPlayerDataFromDatabase()
        {
            if (!_dbInitialized || _dbDataSource == null)
                return;

            PlayerDataMap.Clear();

            var steamIds = new List<ulong>();

            lock (_dbLock)
            {
                using var connection = _dbDataSource.OpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                            SELECT DISTINCT steam_id FROM player_data
                        ";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    steamIds.Add(reader.GetUInt64(0));
                }
            }

            // Load each player's attributes
            foreach (var steamId in steamIds)
            {
                LoadPlayerDataFromDatabase(steamId);
            }

            Console.WriteLine($"Loaded {steamIds.Count} player records from database");
        }

        /// <summary>
        /// Deserializes a JSON value based on its type name.
        /// </summary>
        private static object? DeserializeAttributeValue(string json, string typeName)
        {
            if (string.IsNullOrEmpty(json) || json == "null")
                return null;

            // Try to deserialize as the known type
            return typeName switch
            {
                "String" => JsonSerializer.Deserialize<string>(json),
                "Int32" => JsonSerializer.Deserialize<int>(json),
                "Int64" => JsonSerializer.Deserialize<long>(json),
                "Double" => JsonSerializer.Deserialize<double>(json),
                "Single" => JsonSerializer.Deserialize<float>(json),
                "Boolean" => JsonSerializer.Deserialize<bool>(json),
                "Decimal" => JsonSerializer.Deserialize<decimal>(json),
                "DateTime" => JsonSerializer.Deserialize<DateTime>(json),
                "DateTimeOffset" => JsonSerializer.Deserialize<DateTimeOffset>(json),
                "List`1" => JsonSerializer.Deserialize<List<object>>(json),
                "Dictionary`2" => JsonSerializer.Deserialize<Dictionary<string, object>>(json),
                _ => JsonSerializer.Deserialize<object>(json)
            };
        }
    }
    /// <summary>
    /// Represents custom data stored for a player.
    /// All attribute modifications are persisted to MySQL automatically.
    /// Includes type checking to ensure values can be safely serialized.
    /// </summary>
    public class PlayerData
    {
        public ulong SteamId64 { get; set; }
        public Dictionary<string, object> CustomAttributes { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Checks if a value is serializable to JSON (for MySQL storage).
        /// </summary>
        private bool IsSerializable(object? value)
        {
            if (value == null)
                return true;

            var type = value.GetType();

            // Primitive types are always serializable
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                return true;

            // DateTime is serializable
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                return true;

            // Nullable types
            if (Nullable.GetUnderlyingType(type) != null)
                return IsSerializable(((dynamic)value).Value);

            // Collections (must contain serializable items)
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            {
                if (type.IsGenericType)
                {
                    var genericArgs = type.GetGenericArguments();
                    if (genericArgs.Length == 1)
                    {
                        // List<T> or similar
                        return IsSerializable(Activator.CreateInstance(genericArgs[0]));
                    }
                    else if (genericArgs.Length == 2)
                    {
                        // Dictionary<K, V> or similar
                        return IsSerializable(Activator.CreateInstance(genericArgs[0])) &&
                               IsSerializable(Activator.CreateInstance(genericArgs[1]));
                    }
                }
                return false;
            }

            // Dictionaries
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                return true;
            }

            // Custom objects (try to serialize to test)
            try
            {
                JsonSerializer.Serialize(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sets an attribute with type checking. Throws if value is not serializable.
        /// Changes are automatically persisted to the database.
        /// </summary>
        public void SetAttribute(string key, object? value)
        {
            if (!IsSerializable(value))
            {
                throw new InvalidOperationException(
                    $"Cannot serialize attribute '{key}' of type '{value?.GetType().Name ?? "null"}' to MySQL. " +
                    $"Value must be a primitive, string, DateTime, or a collection of serializable items.");
            }

            CustomAttributes[key] = value!;
            LastUpdated = DateTime.UtcNow;

            // Persist to database
            if (CustomStorage._dbInitialized)
            {
                CustomStorage.PersistAttributeToDatabase(SteamId64, key, value);
            }
        }

        /// <summary>
        /// Gets an attribute value. Returns null if not found.
        /// </summary>
        public object? GetAttribute(string key)
        {
            CustomAttributes.TryGetValue(key, out var value);
            return value;
        }

        /// <summary>
        /// Gets an attribute value with type casting.
        /// </summary>
        public T? GetAttribute<T>(string key)
        {
            if (CustomAttributes.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        /// <summary>
        /// Removes an attribute.
        /// </summary>
        public bool RemoveAttribute(string key)
        {
            bool removed = CustomAttributes.Remove(key);

            if (removed && CustomStorage._dbInitialized)
            {
                CustomStorage.RemoveAttributeFromDatabase(SteamId64, key);
            }

            return removed;
        }
    }
}