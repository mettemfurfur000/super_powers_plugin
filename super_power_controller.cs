using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using Microsoft.VisualBasic;

namespace super_powers_plugin;
public interface ISuperPower
{
    Type TriggerEventType { get; }
    List<CCSPlayerController> Users { get; set; }
    HookResult Execute(GameEvent gameEvent);
    void Update();
    void ParseCfg(Dictionary<string, string> cfg);
}

public class SuperPowerController
{
    private HashSet<ISuperPower> Powers = new HashSet<ISuperPower>();

    public SuperPowerController(SuperPowerConfig cfg)
    {
        Powers.Add(new StartHealth());
        Powers.Add(new StartArmor());
        Powers.Add(new InstantDefuse());
        Powers.Add(new InstantPlant());
        Powers.Add(new FoodSpawner());
        Powers.Add(new InfiniteAmmo());
        Powers.Add(new SonicSpeed());
        Powers.Add(new SteelHead());

        FeedTheConfig(cfg);
    }

    public IEnumerable<ISuperPower> SelectPowers(string pattern)
    {
        string r_pattern = TemUtils.WildCardToRegular(pattern);

        return Powers.Where(p => Regex.IsMatch(TemUtils.GetPowerName(p), r_pattern));
    }

    public List<string> GetPowerList()
    {
        List<string> list = new List<string>();
        foreach (var p in Powers)
            list.Add(TemUtils.GetPowerName(p));
        return list;
    }

    public List<Type> GetPowerTriggerEvents()
    {
        List<Type> list = new List<Type>();
        foreach (var p in Powers)
            list.Add(p.TriggerEventType);
        return list;
    }

    public void Update()
    {
        foreach (var power in Powers)
            power.Update();
    }

    public void FeedTheConfig(SuperPowerConfig cfg)
    {
        foreach (var power in Powers)
            try { power.ParseCfg(cfg.args[TemUtils.GetPowerName(power)]); }
            catch { }
    }

    public HookResult ExecutePower(GameEvent gameEvent)
    {
        HookResult ret = HookResult.Continue;
        Type type = gameEvent.GetType();
        foreach (var power in Powers)
            if (power.TriggerEventType == type || type == typeof(GameEvent))
                if (power.Execute(gameEvent) == HookResult.Stop)
                    ret = HookResult.Stop;

        return ret;
    }

    public void AddNewPower(ISuperPower power)
    {
        Powers.Add(power);
    }

    public string AddPowers(string player_name_pattern, string power_name_pattern, bool now = false)
    {
        var status_message = "";

        var players = TemUtils.SelectPlayers(player_name_pattern);
        if (players == null)
            return "Error: Player(s) not found";

        var powers = SelectPowers(power_name_pattern);
        if (powers == null)
            return "Error: Power(s) not found";

        foreach (var power in powers)
        {
            var powerName = TemUtils.GetPowerName(power);
            foreach (var player in players)
            {
                if (power.Users.Contains(player))
                {
                    status_message += $"{player.PlayerName} already has {powerName}\n";
                    continue;
                }
                try { power.Users.Add(player); }
                catch { status_message += $"Something bad happened while adding {powerName} to {player.PlayerName}, ignoring it\n"; }

                status_message += $"Added {powerName} to {player.PlayerName}\n";
            }

            if (now)
                try
                { power.Execute(new GameEvent(-1)); }
                catch { status_message += $"Something bad happened while triggering {powerName}, ignoring it\n"; }
        }

        return status_message;
    }

    public string RemovePowers(string player_name_pattern, string power_name_pattern)
    {
        var status_message = "";

        var players = TemUtils.SelectPlayers(player_name_pattern);
        if (players == null)
            return "Error: Player(s) not found";

        var powers = SelectPowers(power_name_pattern);
        if (powers == null)
            return "Error: Power(s) not found";

        foreach (var power in powers)
        {
            var powerName = TemUtils.GetPowerName(power);
            foreach (var player in players)
            {
                if (power.Users.Contains(player))
                {
                    try { power.Users.Remove(player); }
                    catch { status_message += $"Something bad happened while removing {powerName} from {player.PlayerName}, ignoring it\n"; }
                    status_message += $"Removed {powerName} from {player.PlayerName}\n";
                }
            }
        }
        return status_message;
    }
}
