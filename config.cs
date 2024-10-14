using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace super_powers_plugin;

public class SuperPowerConfig : IBasePluginConfig
{
    [JsonPropertyName("args")]
    public Dictionary<string, Dictionary<string, string>> args { get; set; } = SuperPowerController.GenerateDefaultConfig();
    [JsonPropertyName("power_blacklist")]
    public List<string> blacklist = ["dormant_power", "banana", "nuke_nades"];
    [JsonPropertyName("ct_blacklist")]
    public List<string> ct_black_list = ["instant_plant"];
    [JsonPropertyName("t_blacklist")]
    public List<string> t_black_list = ["instant_defuse"];
    [JsonPropertyName("powers_pool")]
    public int powers_pool = 3;
    public int Version { get; set; } = 0x0ff - 0x17;
}