using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace super_powers_plugin.src;

public class SuperJump : ISuperPower
{
    public SuperJump() => Triggers = [typeof(EventPlayerJump)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        EventPlayerJump realEvent = (EventPlayerJump)gameEvent;
        var player = realEvent.Userid;
        if (player == null)
            return HookResult.Continue;

        if (!Users.Where(p => p.UserId == player.UserId).Any())
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return HookResult.Continue;

        if (pawn.V_angle.X < -55)
            Server.NextFrame(() =>
            {
                pawn.AbsVelocity.Z *= multiplier;
                if (pawn.AbsVelocity.Z < 290 * multiplier)
                    Server.NextFrame(() =>
                    {
                        pawn.AbsVelocity.Z *= multiplier;
                    });
            });

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Look up and jump to get {multiplier} times higher";

    private float multiplier = 2;
}

