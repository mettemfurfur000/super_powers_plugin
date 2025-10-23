using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class GoldenBullet : BasePower
{
    public GoldenBullet()
    {
        Triggers = [typeof(EventPlayerDeath)];
        Price = 4000;
        Rarity = "Uncommon";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() != Triggers[0])
            return HookResult.Continue;

        EventPlayerDeath realEvent = (EventPlayerDeath)gameEvent;

        var shooter = realEvent.Attacker!;

        if (!Users.Contains(shooter))
            return HookResult.Continue;

        var weapon = shooter.PlayerPawn.Value!.WeaponServices!.ActiveWeapon;

        if (weapon.Value!.Clip1 == 1)
        {
            float timesMult = 1.0f;
            // Server.PrintToChatAll("last");

            if (cfg_headshotMultEnabled && realEvent.Headshot)
            {
                // Server.PrintToChatAll("headshot");
                timesMult = cfg_multOnHeadshot;
            }

            TemUtils.GiveMoney(shooter, (int)(cfg_killReward * timesMult), $"for killing an enemy with a last bullet ({StringHelpers.GetPowerColoredName(this)})");
        }

        return HookResult.Continue;
    }

    // public override string GetDescription() => $"Last bullet in your chamber always hits x{mult} damage";
    public override string GetDescription() => $"Kill with a last bullet gives you ${cfg_killReward}{(cfg_headshotMultEnabled ? $", X{cfg_multOnHeadshot} for headshots" : "")}";
    public override string GetDescriptionColored() => "Kill with a last bullet gives you $" + StringHelpers.Green(cfg_killReward) + (cfg_headshotMultEnabled ? $", X{cfg_multOnHeadshot} for headshots" : "");
    public int cfg_killReward = 3000;
    public float cfg_multOnHeadshot = 1.5f;
    public bool cfg_headshotMultEnabled = false;
}

