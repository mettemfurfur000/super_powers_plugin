using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class GoldenBullet : BasePower
{
    public GoldenBullet() => Triggers = [typeof(EventPlayerHurt)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        EventPlayerHurt realEvent = (EventPlayerHurt)gameEvent;

        var shooter = realEvent.Attacker!;

        if (!Users.Contains(shooter))
            return HookResult.Continue;

        var weapon = shooter.PlayerPawn.Value!.WeaponServices!.ActiveWeapon;

        if (weapon.Value!.Clip1 == 1)
        {
            // Server.PrintToChatAll("last");

            var victim = realEvent.Userid!;
            var pawn = victim.PlayerPawn.Value!;

            pawn.Health -= realEvent.DmgHealth * mult;
            pawn.ArmorValue -= realEvent.DmgArmor * mult;

            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Last bullet in your chamber always hits x{mult} damage";
    private int mult = 4;
}

