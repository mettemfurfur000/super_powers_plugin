using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class ShortFusedBomb : BasePower
{
    public ShortFusedBomb() => Triggers = [typeof(EventBombPlanted)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventBombPlanted)gameEvent;

        if (!Users.Contains(realEvent.Userid!))
            return HookResult.Continue;

        var bombEntity = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
        if (bombEntity != null)
        {
            bombEntity.TimerLength *= 2;
            Server.PrintToChatAll($"set timer to {bombEntity.TimerLength}");
            Utilities.SetStateChanged(bombEntity, "CPlantedC4", "m_flTimerLength");
            // bombEntity.DefuseCountDown = 2;
            // Utilities.SetStateChanged(bombEntity, "CPlantedC4", "m_flDefuseCountDown");
        }

        return HookResult.Continue;
    }



    public int cfg_divisor = 2;
}

