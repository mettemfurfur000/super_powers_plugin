using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using Microsoft.VisualBasic;

namespace super_powers_plugin;
public interface ISuperPower
{
    string PowerName { get; }
    Type TriggerEventType { get; }
    List<CCSPlayerController> Users { get; set; }
    void Execute(GameEvent gameEvent);
}

public class SuperPowerController
{
    private HashSet<ISuperPower> Powers = new HashSet<ISuperPower>();
    public SuperPowerController(SuperPowerConfig cfg)
    {
        Powers.Add(new StartHealth(cfg.args));
        Powers.Add(new StartArmor(cfg.args));
        Powers.Add(new InstantDefuse(cfg.args));
    }
    public List<string> GetPowerList()
    {
        List<string> list = new List<string>();
        foreach (var p in Powers)
            list.Add(p.PowerName);
        return list;
    }
    public void ExecutePower(GameEvent gameEvent)
    {
        Type type = gameEvent.GetType();
        foreach (var power in Powers)
            if (power.TriggerEventType == type || type == typeof(GameEvent))
            {
                Server.PrintToChatAll($"Executing {power.PowerName} for {type}");
                power.Execute(gameEvent);
            }
    }
    public void AddPower(ISuperPower power)
    {
        Powers.Add(power);
    }
    public int AddUser(CCSPlayerController? player, string type)
    {
        if (player == null)
            return -1;

        var power = Powers.FirstOrDefault(p => p.PowerName.Contains(type));
        if (power == null)
            return -1;

        if (power.Users.Contains(player))
            return 0;

        power.Users.Add(player);
        return 0;
    }
    public int RemoveUser(CCSPlayerController? player)
    {
        if (player == null)
            return -1;

        foreach (var p in Powers)
            p.Users.Remove(player);

        return 0;
    }
}
