using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace super_powers_plugin.src;

public class SuperPowerConfig : IBasePluginConfig
{
    [JsonPropertyName("args")]
    public Dictionary<string, Dictionary<string, string>> args { get; set; } = SuperPowerController.GenerateDefaultConfig();
    public int Version { get; set; } = 0x0ff - 0x0b;
    // replace with your own connection string
    public string DataBaseConnectionString { get; set; } = "Server=localhost;User=root;Password=dwKWAdX8k6iHWg==;Database=cs2sp"; 
}