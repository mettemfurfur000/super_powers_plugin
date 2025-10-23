using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class DamageBonus : BasePower
{
    public DamageBonus()
    {
        Triggers = [typeof(EventPlayerHurt)]; // too rewarding for nothing
        SetDisabled();
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventPlayerHurt)gameEvent;
        var attacker = realEvent.Attacker;

        if (attacker == null || !attacker.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(attacker))
            return HookResult.Continue;

        var victim = realEvent.Userid;
        if (victim == null || !victim.IsValid)
            return HookResult.Continue;

        var pawn = victim.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        pawn.Health = pawn.Health - realEvent.DmgHealth * cfg_damage_multiplier;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        return HookResult.Continue;
    }

    public override string GetDescription() => $"All your damage is multiplied by {cfg_damage_multiplier}";

    public int cfg_damage_multiplier = 2;
}

