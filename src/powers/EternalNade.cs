using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class EternalNade : BasePower
{
    public EternalNade()
    {
        Triggers = [
            typeof(EventHegrenadeDetonate),
            typeof(EventMolotovDetonate),
            typeof(EventSmokegrenadeDetonate),
            typeof(EventFlashbangDetonate),
            typeof(EventDecoyDetonate),
            typeof(EventGrenadeThrown),
            // typeof(EventRoundStart),
        ];

        Price = 6000;
        Rarity = "Rare";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        CCSPlayerController? player = null;
        string weaponName = "";

        switch (gameEvent)
        {
            case EventRoundStart start:
                incGrenades.Clear();
                break;
            case EventGrenadeThrown thrown:
                if (thrown.Weapon == "incgrenade")
                {
                    incGrenades.Add(thrown.Userid!);
                    // Server.PrintToChatAll($"handing the edge case");
                }
                // Server.PrintToChatAll($"thrown {thrown.Userid!}, {thrown.Weapon}");
                break;
            case EventHegrenadeDetonate he:
                player = he.Userid;
                weaponName = "weapon_hegrenade";
                break;
            case EventMolotovDetonate molly:
                player = molly.Userid!;
                weaponName = "weapon_molotov";
                if (incGrenades.Contains(player))
                {
                    weaponName = "weapon_incgrenade";
                    incGrenades.Remove(player);
                    // Server.PrintToChatAll($"inc grenade instead");
                }
                break;
            case EventSmokegrenadeDetonate smoke:
                player = smoke.Userid;
                weaponName = "weapon_smokegrenade";
                break;
            case EventFlashbangDetonate flash:
                player = flash.Userid;
                weaponName = "weapon_flashbang";
                break;
            case EventDecoyDetonate decoy:
                player = decoy.Userid;
                weaponName = "weapon_decoy";
                break;
        }

        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(player))
            return HookResult.Continue;

        // Server.PrintToChatAll($"giving {weaponName}");
        player.GiveNamedItem(weaponName);
        return HookResult.Continue;
    }

    public List<CCSPlayerController> incGrenades = [];

    public override string GetDescription() => $"When your grenade detonates, you get it back";
    public override string GetDescriptionColored() => $"When your grenade " + NiceText.Red("detonates") + ", you get it back";
}

