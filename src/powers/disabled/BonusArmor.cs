using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class BonusArmor : BasePower
{
    public BonusArmor()
    {
        Triggers = [typeof(EventRoundStart)];
        SetDisabled();
    }
    
    public override HookResult Execute(GameEvent gameEvent)
    {
        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                continue;

            pawn.ArmorValue = cfg_value;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

        }
        return HookResult.Continue;
    }



    public int cfg_value = 250;
}

