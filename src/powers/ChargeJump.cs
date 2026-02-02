using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

using super_powers_plugin.src;
using SuperPowersPlugin.Utils;

public class ChargeJump : BasePower
{
    public ChargeJump()
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

        if (!Users.Contains(player))
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return HookResult.Continue;

        if ((player.Buttons & PlayerButtons.Duck) != 0)
        {
            // get the player's look angle
            var look_angle = new QAngle(pawn.V_angle.X, pawn.V_angle.Y, pawn.V_angle.Z);
            var out_forward = new Vector();

            NativeAPI.AngleVectors(look_angle.Handle, out_forward.Handle, 0, 0);
            // amplify the forward vector
            out_forward *= cfg_jump_force;
            // add the forward vector to the player's velocity
            Server.NextFrame(() =>
                pawn.AbsVelocity.Add(out_forward)
            );
        }

        return HookResult.Continue;
    }


    public override string GetDescriptionColored() => StringHelpers.Blue("Jump while crouching") + " to make a " + StringHelpers.Blue("leap forward");

    public float cfg_jump_force = 600f;
}

