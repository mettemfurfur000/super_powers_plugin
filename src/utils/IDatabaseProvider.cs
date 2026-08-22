namespace SuperPowersPlugin.Utils;

public interface IDatabaseProvider
{
    void Initialize();
    void EnsurePlayerExists(ulong steamId64);
    void PersistAttribute(ulong steamId64, string key, string valueJson, string valueType);
    void RemoveAttribute(ulong steamId64, string key);
    void RemovePlayer(ulong steamId64);
    List<(string Key, string Value, string Type)> LoadAttributes(ulong steamId64);
    List<ulong> GetAllPlayerSteamIds();
    void Close();
}
