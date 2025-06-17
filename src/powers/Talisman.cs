using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class Talisman : BasePower
{
    public Talisman() => Triggers = [typeof(EventRoundStart)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        foreach (var user in Users)
        {
            if (user.ActionTrackingServices!.MatchStats!.Deaths == 0)
                continue;
            float kd = user.ActionTrackingServices!.MatchStats!.Kills / user.ActionTrackingServices!.MatchStats!.Deaths;
            if (kd < gate)
            {
                user.InGameMoneyServices!.Account += value;
                Utilities.SetStateChanged(user, "CCSPlayerController", "m_pInGameMoneyServices");

                user.PrintToCenter($"Talisman gives you a bonus\n of {value}$ based on your k/d {kd}");
            }
        }
        return HookResult.Continue;
    }

    public override string GetDescription() => $"if k/d is below {gate}, gain {value}$";
    private int value = 2500;
    private float gate = 1.0f;
}

