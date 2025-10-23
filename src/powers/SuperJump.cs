using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class SuperJump : BasePower
{
    public SuperJump()
    {
        Triggers = [typeof(EventPlayerJump)];
        Price = 2500;
        Rarity = "Common";
    }
    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() != Triggers[0])
            return HookResult.Continue;

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
                pawn.AbsVelocity.Z *= cfg_multiplier;
                if (pawn.AbsVelocity.Z < 290 * cfg_multiplier)
                    Server.NextFrame(() =>
                    {
                        pawn.AbsVelocity.Z *= cfg_multiplier;
                    });
            });

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Look up and jump to get {cfg_multiplier} times higher";
    public override string GetDescriptionColored() => "Look up and jump to get " + StringHelpers.Blue(cfg_multiplier) + " times higher";

    public float cfg_multiplier = 2;
}

