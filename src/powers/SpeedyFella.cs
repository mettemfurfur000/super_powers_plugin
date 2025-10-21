using System.Diagnostics.Contracts;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;

using super_powers_plugin.src;

public class SpeedyFella : BasePower
{
    public SpeedyFella()
    {
        Triggers = [];
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        return HookResult.Continue;
    }

    public override void Update()
    {
        Users.ForEach((user) =>
        {
            if (!user.IsValid || user == null)
                return;

            if ((int)(user.Buttons & PlayerButtons.Forward) == 0 &&
                (int)(user.Buttons & PlayerButtons.Moveright) == 0 &&
                (int)(user.Buttons & PlayerButtons.Moveleft) == 0 &&
                (int)(user.Buttons & PlayerButtons.Back) == 0)
            {
                UpdateAcceleration(user, true);
                return;
            }

            if (user.PlayerPawn.Value == null || !user.PlayerPawn.IsValid)
            {
                UpdateAcceleration(user, true);
                return;
            }

            UpdateAcceleration(user, user.PlayerPawn.Value.AbsVelocity.Length() <= minimalSpeed);
        });
    }

    public void UpdateAcceleration(CCSPlayerController user, bool stopAccelerating)
    {
        if (user.PlayerPawn.Value == null || !user.PlayerPawn.IsValid)
            return;

        var pawn = user.PlayerPawn.Value;
        pawn.VelocityModifier = (float)(stopAccelerating ? 1 : pawn.VelocityModifier * factor);
        pawn.MovementServices!.Maxspeed = (float)(stopAccelerating ? 320 : pawn.MovementServices!.Maxspeed * factor);
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
        Utilities.SetStateChanged(pawn, "CPlayer_MovementServices", "m_flMaxspeed");
    }

    public override string GetDescription() => $"";
    public override string GetDescriptionColored() => $"";
    private int minimalSpeed = 250;
    private float factor = 1.01f;
}