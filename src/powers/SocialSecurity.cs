using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class SocialSecurity : BasePower
{
    public SocialSecurity()
    {
        Triggers = [typeof(EventRoundStart)];
        Price = 3500;
        Rarity = "Common";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        foreach (var user in Users)
        {
            if (user.ActionTrackingServices!.MatchStats!.Deaths == 0)
                continue;
            float kd = user.ActionTrackingServices!.MatchStats!.Kills / user.ActionTrackingServices!.MatchStats!.Deaths;
            if (kd < gate)
            {
                TemUtils.GiveMoney(user,value, $"for having k/d {kd:n2} ({NiceText.GetPowerColoredName(this)})");
            }
        }
        return HookResult.Continue;
    }

    public override string GetDescription() => $"At round start, if your k/d is below {gate}, gain {value}$";
    public override string GetDescriptionColored() => $"At round start, If your k/d is below " + NiceText.Blue(gate) + ", gain " + NiceText.Green(value) + "$";

    private int value = 2500;
    private float gate = 0.9f;
}

