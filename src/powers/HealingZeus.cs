using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class HealingZeus : BasePower
{
    public HealingZeus()
    {
        Triggers = [typeof(EventPlayerHurt)];
        Price = 2500;
        Rarity = "Common";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() != Triggers[0])
            return HookResult.Continue;

        var realEvent = (EventPlayerHurt)gameEvent;
        var attacker = realEvent.Attacker;
        var victim = realEvent.Userid;

        if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(attacker))
            return HookResult.Continue;

        var victim_pawn = victim.PlayerPawn.Value;
        if (victim_pawn == null || !victim_pawn.IsValid)
            return HookResult.Continue;

        if (victim_pawn.TeamNum != attacker.TeamNum)
            return HookResult.Continue;

        victim_pawn.Health = cfg_health_set;

        Utilities.SetStateChanged(victim_pawn, "CBaseEntity", "m_iHealth");
        return HookResult.Continue;
    }

    public override string GetDescription() => $"Zap your teammates to set their health to {cfg_health_set}";
    public override string GetDescriptionColored() => $"Zap your " + StringHelpers.Green("teammates") + " to set their health to " + StringHelpers.Green(cfg_health_set);

    public int cfg_health_set = 75;
}

