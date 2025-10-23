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
            if (kd < cfg_gate)
            {
                TemUtils.GiveMoney(user,cfg_value, $"for having k/d {kd:n2} ({StringHelpers.GetPowerColoredName(this)})");
            }
        }
        return HookResult.Continue;
    }

    public override string GetDescription() => $"At round start, if your k/d is below {cfg_gate}, gain {cfg_value}$";
    public override string GetDescriptionColored() => $"At round start, If your k/d is below " + StringHelpers.Blue(cfg_gate) + ", gain " + StringHelpers.Green(cfg_value) + "$";

    public int cfg_value = 2500;
    public float cfg_gate = 0.9f;
}

