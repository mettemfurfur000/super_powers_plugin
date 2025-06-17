using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class BonusArmor : BasePower
{
    public BonusArmor() => Triggers = [typeof(EventRoundStart)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                continue;

            pawn.ArmorValue = value;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

        }
        return HookResult.Continue;
    }

    public override string GetDescription() => $"Obtain {value} armor each round, head armor not included";

    private int value = 250;
}

