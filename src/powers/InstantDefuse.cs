using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace super_powers_plugin.src;

public class InstantDefuse : ISuperPower
{
    public InstantDefuse()
    {
        Triggers = [typeof(EventBombBegindefuse)];
        teamReq = CsTeam.CounterTerrorist;
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventBombBegindefuse)gameEvent;
        var player = realEvent.Userid;
        if (player != null && player.IsValid && player.PawnIsAlive)
        {
            if (!Users.Contains(player))
                return HookResult.Continue;

            var bomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").ToList().FirstOrDefault();
            if (bomb == null)
            {
                return HookResult.Continue; ;
            }
            Server.NextFrame(() =>
            {
                bomb.DefuseCountDown = 0;
                Utilities.SetStateChanged(bomb, "CPlantedC4", "m_flDefuseCountDown");

            });
        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Defuse bombs instantly (even withot defuse kit)";
}

