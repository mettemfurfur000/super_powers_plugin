using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

using super_powers_plugin.src;

public class InstantDefuse : BasePower
{
    public InstantDefuse()
    {
        Triggers = [typeof(EventBombBegindefuse)];
        teamReq = CsTeam.CounterTerrorist;

        Price = 2500;
        Rarity = "Common";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() != Triggers[0])
            return HookResult.Continue;
            
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

    public override string GetDescription() => $"Defuse bombs instantly";
    public override string GetDescriptionColored() => NiceText.Blue("Defuse") + " bombs instantly";
}

