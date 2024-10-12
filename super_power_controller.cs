using System.Reflection;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using Microsoft.VisualBasic;

namespace super_powers_plugin;
// im not sure if i use the right thing here, not familiar with da oop u know big bro... i usualy write in C
public interface ISuperPower
{
    List<Type> Triggers { get; }
    List<CCSPlayerController> Users { get; set; }
    HookResult Execute(GameEvent gameEvent);
    void Update();
    void ParseCfg(Dictionary<string, string> cfg)
    {
        TemUtils.ParseConfigReflective(this, this.GetType(), cfg);
    }
    void OnRemove(CCSPlayerController? player)
    {

    }
}

public static class SuperPowerController
{
    private static HashSet<ISuperPower> Powers = new HashSet<ISuperPower>();
    private static string mode = "normal";

    public static IEnumerable<ISuperPower> SelectPowers(string pattern)
    {
        string r_pattern = TemUtils.WildCardToRegular(pattern);

        return Powers.Where(p => Regex.IsMatch(TemUtils.GetPowerName(p), r_pattern));
    }

    static SuperPowerController()
    {
        Powers.Add(new StartHealth());
        Powers.Add(new StartArmor());
        Powers.Add(new InstantDefuse());
        Powers.Add(new InstantPlant());
        Powers.Add(new FoodSpawner());
        Powers.Add(new InfiniteAmmo());
        Powers.Add(new SonicSpeed());
        Powers.Add(new HeadshotImmunity());
        Powers.Add(new InfiniteMoney());
        Powers.Add(new NukeNades());
        Powers.Add(new EvilAura());
        Powers.Add(new DormantPower());
        Powers.Add(new GlassCannon());
        Powers.Add(new Vampirism());
        Powers.Add(new SuperJump());
        // unable to make it work for now, wait for this https://github.com/roflmuffin/CounterStrikeSharp/pull/608
        // Powers.Add(new Invisibility()); 
    }

    public static void SetMode(string _mode)
    {
        mode = _mode;
    }

    public static string GetMode()
    {
        return mode;
    }

    public static HashSet<ISuperPower> GetPowers()
    {
        return Powers;
    }

    public static ISuperPower GetPowerUsers(string name_pattern)
    {
        var found_powers = SelectPowers(name_pattern);
        return found_powers.First();
    }

    public static List<string> GetPowerList()
    {
        List<string> list = new List<string>();
        foreach (var p in Powers)
            list.Add(TemUtils.GetPowerName(p));
        return list;
    }

    public static List<List<Type>> GetPowerTriggerEvents()
    {
        List<List<Type>> list = [];
        foreach (var p in Powers)
            list.Add(p.Triggers);
        return list;
    }

    public static string GetUsersTable()
    {
        string out_string = "";
        foreach (var p in Powers)
        {
            out_string += TemUtils.GetPowerName(p) + ":\n";
            var users = p.Users;
            foreach (var user in users)
            {
                out_string += $"\t{user.PlayerName}\n";
            }
        }
        return out_string;
    }

    public static void Update()
    {
        foreach (var power in Powers)
            power.Update();
    }

    public static void FeedTheConfig(SuperPowerConfig cfg)
    {
        foreach (var power in Powers)
            try { power.ParseCfg(cfg.args[TemUtils.GetPowerName(power)]); }
            catch { }
    }

    public static void Reconfigure(Dictionary<string, string> configuration, string power_name_pattern)
    {
        var powers = SelectPowers(power_name_pattern);
        foreach (var power in powers)
            power.ParseCfg(configuration);
    }

    public static HookResult ExecutePower(GameEvent gameEvent)
    {
        HookResult ret = HookResult.Continue;
        Type type = gameEvent.GetType();
        foreach (var power in Powers)
            if (power.Triggers.Contains(type) || type == typeof(GameEvent))
                if (power.Execute(gameEvent) == HookResult.Stop)
                    ret = HookResult.Stop;

        return ret;
    }

    public static void AddNewPower(ISuperPower power)
    {
        Powers.Add(power);
    }

    public static void CleanPowers()
    {
        // clear all powers first
        foreach (var p in Powers)
        {
            p.OnRemove(null);
            p.Users.Clear();
        }
    }

    public static string AddPowerRandomlyToEveryone()
    {
        CleanPowers();

        var players = Utilities.GetPlayers();
        if (!players.Any())
            return "Error: No players found";

        List<string> blacklist = ["dormant_power", "food_spawner", "nuke_nades"];
        List<string> ct_black_list = ["instant_plant"];
        List<string> t_black_list = ["instant_defuse"];

        foreach (var player in players)
        {
            var power = Powers.ElementAt(new Random().Next(Powers.Count));

            if (player.Team == CounterStrikeSharp.API.Modules.Utils.CsTeam.Terrorist)
                if (t_black_list.Contains(TemUtils.GetPowerName(power)))
                    continue;

            if (player.Team == CounterStrikeSharp.API.Modules.Utils.CsTeam.CounterTerrorist)
                if (ct_black_list.Contains(TemUtils.GetPowerName(power)))
                    continue;

            if (blacklist.Contains(TemUtils.GetPowerName(power)))
                continue;

            power.Users.Add(player);
            player.PrintToCenterAlert($"You have been given a random power: {TemUtils.GetPowerName(power)}");
            player.PrintToChat($"You have been given a random power: {TemUtils.GetPowerName(power)}");
            player.ExecuteClientCommand("play sounds/diagnostics/bell.vsnd");

            try
            { power.Execute(new GameEvent(0)); }
            catch { }
        }

        return "Successfully added random powers to everyone";
    }

    public static string AddPowers(string player_name_pattern, string power_name_pattern, bool now = false)
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
                var now_tip = now ? ", Effects will be applied now" : "";
                player.PrintToChat($"You have been given {powerName} by the server{now_tip}!");
                player.ExecuteClientCommand("play sounds/diagnostics/bell.vsnd");
            }

            if (now)
                try
                { power.Execute(new GameEvent(0)); }
                catch { status_message += $"Something bad happened while triggering {powerName}, ignoring it\n"; }
        }

        return status_message;
    }

    public static string RemovePowers(string player_name_pattern, string power_name_pattern)
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

    public static Dictionary<string, Dictionary<string, string>> GenerateDefaultConfig()
    {
        Dictionary<string, Dictionary<string, string>> args = new Dictionary<string, Dictionary<string, string>>();
        foreach (var power in Powers)
        {
            var power_name = TemUtils.GetPowerName(power);
            if (power_name == null)
            {
                continue;
            }
            //Server.PrintToConsole($"{power_name}:");
            var fields = power.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            args.Add(power_name, new Dictionary<string, string>());
            foreach (var property in fields)
            {
                if (property.IsPublic) continue;
                if (property.Name.Contains("Triggers")) continue;
                if (property.Name.Contains("Users")) continue;

                var property_name = property.Name;
                var property_value = property.GetValue(power);

                //Server.PrintToConsole($"\tField {property_name}");

                if (property_value != null)
                {
                    args[power_name].Add(property_name, property_value.ToString() ?? "null");
                }
            }
        }
        return args;
    }
}
