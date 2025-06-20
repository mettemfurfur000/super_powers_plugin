using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class DamageLoss : BasePower
{
    public DamageLoss() => Triggers = [typeof(EventPlayerHurt)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventPlayerHurt)gameEvent;
        var victim = realEvent.Userid;

        if (victim == null || !victim.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(victim))
            return HookResult.Continue;

        if (Random.Shared.NextSingle() < probabilityPercentage / 100.0f)
            return HookResult.Continue;

        var victim_pawn = victim.PlayerPawn.Value;
        if (victim_pawn == null || !victim_pawn.IsValid)
            return HookResult.Continue;

        victim_pawn.Health += realEvent.DmgHealth;
        victim_pawn.ArmorValue += realEvent.DmgArmor;

        Utilities.SetStateChanged(victim_pawn, "CBaseEntity", "m_iHealth");
        Utilities.SetStateChanged(victim_pawn, "CCSPlayerPawn", "m_ArmorValue");

        return HookResult.Continue;
    }

    public override string GetDescription() => $"{probabilityPercentage}% chance to ignore incoming damage event";

    private int probabilityPercentage = 50;
}

