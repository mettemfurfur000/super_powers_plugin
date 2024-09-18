using System.Reflection.Metadata.Ecma335;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;

namespace super_powers_plugin;

public class StartHealth : ISuperPower
{
    public Type TriggerEventType => typeof(EventRoundStart);
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
    public string PowerName => "start_health";
    private int value = 404;
    public void Execute(GameEvent gameEvent)
    {
        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                continue;

            //Server.PrintToChatAll($"Processing {pawn} for {value} HP");
            //Server.PrintToChatAll($"Bonus tips: {pawn?.Controller.Value?.PlayerName} {pawn?.IsValid}");
            pawn.Health = value;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
    }
    public StartHealth(Dictionary<string, Dictionary<string, string>> cfg)
    {
        value = int.Parse(cfg[PowerName]["value"]);
    }
}

public class StartArmor : ISuperPower
{
    public Type TriggerEventType => typeof(EventRoundStart);
    public string PowerName => "start_armor";
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
    private int value = 404;
    public void Execute(GameEvent gameEvent)
    {
        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                continue;

            pawn.ArmorValue = value;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

        }
    }
    public StartArmor(Dictionary<string, Dictionary<string, string>> cfg)
    {
        value = int.Parse(cfg[PowerName]["value"]);
    }
}

public class InstantDefuse : ISuperPower
{
    public Type TriggerEventType => typeof(EventBombBegindefuse);
    public string PowerName => "instant_defuse";
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
    public void Execute(GameEvent gameEvent)
    {
        EventBombBegindefuse realEvent = (EventBombBegindefuse)gameEvent;
        var player = realEvent.Userid;
        if (player != null && player.IsValid && player.PawnIsAlive)
        {
            if (!Users.Where(p => p.UserId == player.UserId).Any())
            {
                Server.PrintToChatAll("player is not in list");
            }
            var bomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").ToList().FirstOrDefault();
            if (bomb == null)
            {
                player.PrintToCenter("Failed to find bomb for you, buddy... my bad!");
                return;
            }
            Server.NextFrame(() =>
            {
                bomb.DefuseCountDown = 0;
                Utilities.SetStateChanged(bomb, "CPlantedC4", "m_flDefuseCountDown");
                Server.PrintToChatAll($"Successful instant defuse by {player.PlayerName}");
            });
        }
        else
        {
            Server.PrintToChatAll("player is null or not valid");
        }
    }
    public InstantDefuse(Dictionary<string, Dictionary<string, string>> cfg)
    {
    }
}