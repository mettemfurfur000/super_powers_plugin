using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class GoldenBullet : BasePower
{
    public GoldenBullet() => Triggers = [typeof(EventPlayerDeath)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        EventPlayerDeath realEvent = (EventPlayerDeath)gameEvent;

        var shooter = realEvent.Attacker!;

        if (!Users.Contains(shooter))
            return HookResult.Continue;

        var weapon = shooter.PlayerPawn.Value!.WeaponServices!.ActiveWeapon;

        if (weapon.Value!.Clip1 == 0)
        {
            float timesMult = 1.0f;
            Server.PrintToChatAll("last");

            if (headshotMultEnabled && realEvent.Headshot)
            {
                Server.PrintToChatAll("headshot");
                timesMult = multOnHeadshot;
            }

            shooter.InGameMoneyServices!.Account += (int)(killReward * timesMult);
            Utilities.SetStateChanged(shooter, "CCSPlayerController", "m_pInGameMoneyServices");

            // var victim = realEvent.Userid!;
            // var pawn = victim.PlayerPawn.Value!;

            // pawn.Health -= realEvent.DmgHealth * mult;
            // pawn.ArmorValue -= realEvent.DmgArmor * mult;

            // Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            // Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
        }

        return HookResult.Continue;
    }

    // public override string GetDescription() => $"Last bullet in your chamber always hits x{mult} damage";
    public override string GetDescription() => $"Kill with a last bullet gives you ${killReward}{(headshotMultEnabled ? $", X{multOnHeadshot} for headshots" : "")}";

    private int killReward = 1250;
    private float multOnHeadshot = 1.5f;
    private bool headshotMultEnabled = true;
}

