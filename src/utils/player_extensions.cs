using System.Diagnostics.CodeAnalysis;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using super_powers_plugin.src;

public static class PlayerExtensions
{
    public static void PrintToggleable(this CCSPlayerController player, string text)
    {
        if (TemUtils.__plugin!.silent.Value)
            return;

        player.PrintToChat(text);
    }
}