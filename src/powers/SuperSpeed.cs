using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class SuperSpeed : BasePower
{
    public SuperSpeed()
    {
        Triggers = [typeof(EventRoundStart)];
        Price = 3500;
        Rarity = "Common";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        TemUtils.PowerApplySpeed(Users, cfg_value);
        return HookResult.Continue;
    }

    public override void Update()
    {
        if (Server.TickCount % cfg_period != 0) return;
        TemUtils.PowerApplySpeed(Users, cfg_value);
    }

    public override void OnRemovePower(CCSPlayerController? player)
    {
        TemUtils.PowerRemoveSpeedModifier(Users, player);
    }

    public override string GetDescription() => $"Increased walking speed ({(float)cfg_value / default_velocity_max}X faster)";
    public override string GetDescriptionColored() => "Increased walking speed (" + StringHelpers.Blue((float)cfg_value / default_velocity_max) + "X faster)";

    public int cfg_value = 700;
    public int cfg_period = 4;
    public const int default_velocity_max = 250;
}

