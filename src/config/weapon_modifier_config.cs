using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace super_powers_plugin.src;

public class WeaponModifierConfig : IBasePluginConfig
{
    [JsonPropertyName("modifiers")]
    public Dictionary<string, Dictionary<string, string>> Modifiers { get; set; }
        = WeaponModifierManager.GenerateDefaultConfig();

    public int Version { get; set; } = 1;
}
