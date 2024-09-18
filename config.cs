using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace super_powers_plugin;

public class SuperPowerConfig : IBasePluginConfig
{
    [JsonPropertyName("args")]
    public Dictionary<string, Dictionary<string, string>> args { get; set; } = new() { { "start_health", new Dictionary<string, string> { { "value", "250" } } } };
    public int Version { get; set; } = 0x0ff - 0x17;
}