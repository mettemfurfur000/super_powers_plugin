using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace super_powers_plugin.src;

public class EternalNade : ISuperPower
{
    public EternalNade()
    {
        Triggers = [    typeof(EventHegrenadeDetonate),
                                            typeof(EventMolotovDetonate),
                                            typeof(EventSmokegrenadeDetonate),
                                            typeof(EventFlashbangDetonate),
                                            typeof(EventDecoyDetonate),
                                            typeof(EventGrenadeThrown),

    ];
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        CCSPlayerController? player = null;
        Tuple<CCSPlayerController, string>? nadeRecord = null;

        switch (gameEvent)
        {
            case EventGrenadeThrown thrown:
                nadeRecord = new Tuple<CCSPlayerController, string>(thrown.Userid!, thrown.Weapon);
                // Server.PrintToChatAll($"thrown {thrown.Userid!}, {thrown.Weapon}");
                break;
            case EventHegrenadeDetonate he:
                player = he.Userid;
                break;
            case EventMolotovDetonate molly:
                player = molly.Userid;
                break;
            case EventSmokegrenadeDetonate smoke:
                player = smoke.Userid;
                break;
            case EventFlashbangDetonate flash:
                player = flash.Userid;
                break;
            case EventDecoyDetonate decoy:
                player = decoy.Userid;
                break;
        }

        if (nadeRecord != null)
        {
            nadesThrown.Add(nadeRecord);
            return HookResult.Continue;
        }

        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(player))
            return HookResult.Continue;

        string weaponName = "";

        foreach (var nade in nadesThrown)
            if (nade.Item1 == player)
            {
                nadeRecord = nade;
                weaponName = "weapon_" + nade.Item2;
                break;
            }

        if (nadeRecord == null)
            return HookResult.Continue;

        // Server.PrintToChatAll($"removing {nadeRecord}, giving {weaponName}");

        nadesThrown.Remove(nadeRecord);
        player.GiveNamedItem(weaponName);

        return HookResult.Continue;
    }

    public List<Tuple<CCSPlayerController, string>> nadesThrown = [];

    public override string GetDescription() => $"Once your grenade detonates, you get it back";
}

