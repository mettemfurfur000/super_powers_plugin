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
        Rarity = PowerRarity.Uncommon;
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        EventPlayerDeath realEvent = (EventPlayerDeath)gameEvent;

        var shooter = realEvent.Attacker!;

        if (!Users.Contains(shooter))
            return HookResult.Continue;

        var weapon = shooter.PlayerPawn.Value!.WeaponServices!.ActiveWeapon;

        if (weapon.Value!.Clip1 == 1)
        {
            float timesMult = 1.0f;
            Server.PrintToChatAll("last");

            if (headshotMultEnabled && realEvent.Headshot)
            {
                Server.PrintToChatAll("headshot");
                timesMult = multOnHeadshot;
            }

            TemUtils.GiveMoney(shooter, (int)(killReward * timesMult), $"for killing an enemy with a last bullet ({NiceText.GetPowerColoredName(this)})");
        }

        return HookResult.Continue;
    }

    // public override string GetDescription() => $"Last bullet in your chamber always hits x{mult} damage";
    public override string GetDescription() => $"Kill with a last bullet gives you ${killReward}{(headshotMultEnabled ? $", X{multOnHeadshot} for headshots" : "")}";
    public override string GetDescriptionColored() => "Kill with a last bullet gives you $" + NiceText.Green(killReward) + (headshotMultEnabled ? $", X{multOnHeadshot} for headshots" : "");

    private int killReward = 3000;
    private float multOnHeadshot = 1.5f;
    private bool headshotMultEnabled = false;
}

