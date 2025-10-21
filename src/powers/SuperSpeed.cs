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
        TemUtils.PowerApplySpeed(Users, value);
        return HookResult.Continue;
    }

    public override void Update()
    {
        if (Server.TickCount % period != 0) return;
        TemUtils.PowerApplySpeed(Users, value);
    }

    public override void OnRemovePower(CCSPlayerController? player)
    {
        TemUtils.PowerRemoveSpeedModifier(Users, player);
    }

    public override string GetDescription() => $"Increased walking speed ({(float)value / default_velocity_max}X faster)";
    public override string GetDescriptionColored() => "Increased walking speed (" + StringHelpers.Blue((float)value / default_velocity_max) + "X faster)";

    private int value = 700;
    private int period = 4;
    public const int default_velocity_max = 250;
}

