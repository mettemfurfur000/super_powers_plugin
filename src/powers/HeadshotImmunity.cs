using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class HeadshotImmunity : BasePower
{
    public HeadshotImmunity() => Triggers = [typeof(EventPlayerHurt)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventPlayerHurt)gameEvent;
        var player = realEvent.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(player))
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return HookResult.Continue;

        if ((HitGroup_t)realEvent.Hitgroup == HitGroup_t.HITGROUP_HEAD)
        {
            pawn.Health = pawn.Health + realEvent.DmgHealth;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            pawn.ArmorValue += realEvent.DmgArmor;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Calcels all headshots, landed on your head";
}

