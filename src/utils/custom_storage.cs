using System.Text.Json;
using CounterStrikeSharp.API.Core;

namespace SuperPowersPlugin.Utils
{
    public static class CustomStorage
    {
        private static readonly Dictionary<ulong, PlayerData> PlayerDataMap = new();
        private static IDatabaseProvider? _db;
        internal static bool _dbInitialized = false;

        public static void InitializeDatabase(string connectionString, bool useSqlite = false)
        {
            if (useSqlite)
            {
                var dbPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "configs", "plugins", "super_powers_plugin", "standalone.db"
                );
                _db = new SqliteDatabaseProvider(dbPath);
            }
            else
            {
                _db = new MySqlDatabaseProvider(connectionString);
            }

            _db.Initialize();
            _dbInitialized = true;
        }

        public static PlayerData GetOrCreatePlayerData(ulong steamId64)
        {
            if (!PlayerDataMap.TryGetValue(steamId64, out var data))
            {
                data = new PlayerData { SteamId64 = steamId64 };
                PlayerDataMap[steamId64] = data;

                if (_dbInitialized)
                    _db!.EnsurePlayerExists(steamId64);
            }
            return data;
        }

        public static PlayerData GetOrCreatePlayerData(CCSPlayerController controller)
        {
            return GetOrCreatePlayerData(controller.SteamID);
        }

        public static PlayerData GetOrLoadPlayerData(ulong steamId64)
        {
            if (PlayerDataMap.TryGetValue(steamId64, out var existing))
                return existing;

            if (_dbInitialized)
                LoadPlayerDataFromDatabase(steamId64);

            return GetOrCreatePlayerData(steamId64);
        }

        public static PlayerData GetOrLoadPlayerData(CCSPlayerController controller)
        {
            return GetOrLoadPlayerData(controller.SteamID);
        }

        public static void RemovePlayerData(ulong steamId64)
        {
            PlayerDataMap.Remove(steamId64);
            if (_dbInitialized)
                _db!.RemovePlayer(steamId64);
        }

        public static void RemovePlayerData(CCSPlayerController controller)
        {
            RemovePlayerData(controller.SteamID);
        }

        public static void PersistAttributeToDatabase(ulong steamId64, string key, object? value)
        {
            if (!_dbInitialized) return;

            string valueJson = JsonSerializer.Serialize(value);
            string valueType = value?.GetType().Name ?? "null";

            _db!.PersistAttribute(steamId64, key, valueJson, valueType);
        }

        public static void RemoveAttributeFromDatabase(ulong steamId64, string key)
        {
            if (!_dbInitialized) return;
            _db!.RemoveAttribute(steamId64, key);
        }

        public static void LoadPlayerDataFromDatabase(ulong steamId64)
        {
            if (!_dbInitialized) return;
            if (PlayerDataMap.ContainsKey(steamId64)) return;

            var rows = _db!.LoadAttributes(steamId64);
            var attributes = new Dictionary<string, object>();

            foreach (var (key, valueJson, typeName) in rows)
            {
                var value = DeserializeAttributeValue(valueJson, typeName);
                if (value != null)
                    attributes[key] = value;
            }

            if (attributes.Count > 0)
                PlayerDataMap[steamId64] = new PlayerData { SteamId64 = steamId64, CustomAttributes = attributes };
            else if (!PlayerDataMap.ContainsKey(steamId64))
                PlayerDataMap[steamId64] = new PlayerData { SteamId64 = steamId64 };
        }

        public static void LoadAllPlayerDataFromDatabase()
        {
            if (!_dbInitialized) return;

            PlayerDataMap.Clear();
            var steamIds = _db!.GetAllPlayerSteamIds();

            foreach (var steamId in steamIds)
                LoadPlayerDataFromDatabase(steamId);

            Console.WriteLine($"Loaded {steamIds.Count} player records from database");
        }

        public static IReadOnlyDictionary<ulong, PlayerData> GetAllPlayerData()
        {
            return new Dictionary<ulong, PlayerData>(PlayerDataMap);
        }

        public static void Clear()
        {
            PlayerDataMap.Clear();
        }

        public static void CloseDatabase()
        {
            _db?.Close();
            _dbInitialized = false;
        }

        private static object? DeserializeAttributeValue(string json, string typeName)
        {
            if (string.IsNullOrEmpty(json) || json == "null")
                return null;

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

    public class PlayerData
    {
        public ulong SteamId64 { get; set; }
        public Dictionary<string, object> CustomAttributes { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        private bool IsSerializable(object? value)
        {
            if (value == null) return true;

            var type = value.GetType();
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                return true;
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                return true;
            if (Nullable.GetUnderlyingType(type) != null)
                return IsSerializable(((dynamic)value).Value);
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            {
                if (type.IsGenericType)
                {
                    var genericArgs = type.GetGenericArguments();
                    if (genericArgs.Length == 1)
                        return IsSerializable(Activator.CreateInstance(genericArgs[0]));
                    else if (genericArgs.Length == 2)
                        return IsSerializable(Activator.CreateInstance(genericArgs[0])) &&
                               IsSerializable(Activator.CreateInstance(genericArgs[1]));
                }
                return false;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                return true;
            try { JsonSerializer.Serialize(value); return true; }
            catch { return false; }
        }

        public void SetAttribute(string key, object? value)
        {
            if (!IsSerializable(value))
                throw new InvalidOperationException(
                    $"Cannot serialize attribute '{key}' of type '{value?.GetType().Name ?? "null"}' to database. " +
                    $"Value must be a primitive, string, DateTime, or a collection of serializable items.");

            CustomAttributes[key] = value!;
            LastUpdated = DateTime.UtcNow;

            if (CustomStorage._dbInitialized)
                CustomStorage.PersistAttributeToDatabase(SteamId64, key, value);
        }

        public object? GetAttribute(string key)
        {
            CustomAttributes.TryGetValue(key, out var value);
            return value;
        }

        public T? GetAttribute<T>(string key)
        {
            if (CustomAttributes.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }

        public bool RemoveAttribute(string key)
        {
            bool removed = CustomAttributes.Remove(key);
            if (removed && CustomStorage._dbInitialized)
                CustomStorage.RemoveAttributeFromDatabase(SteamId64, key);
            return removed;
        }
    }
}
