using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class FlashOfDisability : BasePower
{
    public FlashOfDisability() => Triggers = [typeof(EventPlayerBlind)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventPlayerBlind)gameEvent;

        // Server.PrintToChatAll("blind detected");

        var attacker = realEvent.Attacker;
        var victim = realEvent.Userid;

        if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(attacker))
            return HookResult.Continue;

        if (ignore_self_flash && attacker == victim)
            return HookResult.Continue;

        // SuperPowerController.DisablePlayer(victim, (int)(victim.PlayerPawn.Value!.BlindStartTime - victim.PlayerPawn.Value!.BlindUntilTime));
        SuperPowerController.DisablePlayer(victim, (int)victim.PlayerPawn.Value!.FlashDuration * 64);

        return HookResult.Continue;
    }

    public override string GetDescription() => $"enemies have their powers disabled if you flash them";

    private bool ignore_self_flash = true;
}

