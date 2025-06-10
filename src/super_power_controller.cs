using System.Reflection;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace super_powers_plugin.src;

public enum SIGNAL_STATUS
{
    IGNORED,
    ACCEPTED,
    ERROR
};

// TODO:
// - add dependency list thing for powers
// - add incompabiliti list for powers
// - allow multiple powers to affect the same variable (add by a value or mult by a value)

// abscaraftlks

public abstract class ISuperPower
{
    public List<Type> Triggers = [];
    public List<CCSPlayerController> Users = [];
    public List<Tuple<CCSPlayerController, int>> UsersDisabled = [];
    public List<ulong> UsersSteamIDs = [];
    public List<string> NeededResources = [];

    public CsTeam teamNumUsedOnly = CsTeam.None;

    public List<ISuperPower> Incompatibilities = [];
    private bool disabled = false;

    public void setDisabled() { disabled = true; }
    public bool IsDisabled() => disabled;

    public virtual string GetDescription() => "";

    public virtual HookResult Execute(GameEvent gameEvent) { return HookResult.Continue; }
    public virtual void Update() { }
    public virtual void ParseCfg(Dictionary<string, string> cfg) { TemUtils.ParseConfigReflective(this, this.GetType(), cfg); }
    public virtual bool IsUser(CCSPlayerController player) { return Users.Contains(player); }

    public virtual void OnRemovePower(CCSPlayerController? player) { }
    public virtual Tuple<SIGNAL_STATUS, string> OnSignal(CCSPlayerController? player, List<string> args) { return Tuple.Create(SIGNAL_STATUS.IGNORED, ""); }
    public virtual void OnRemoveUser(CCSPlayerController? player, bool reasonDisconnect) // called each time player leaves the server
    {
        if (player == null) // clear all
        {
            Users.Clear();
            UsersSteamIDs.Clear();
            return;
        }

        Users.Remove(player);
        if (reasonDisconnect == false)
            UsersSteamIDs.Remove(player.SteamID);
    }

    public virtual bool OnAdd(CCSPlayerController player, bool forced = false) // called to add player to power
    {
        if (teamNumUsedOnly != CsTeam.None && player.TeamNum != (byte)teamNumUsedOnly && forced == false)
            return false;

        Users.Add(player);
        UsersSteamIDs.Add(player.SteamID);

        return true;
    }

    public virtual void OnRejoin(CCSPlayerController player) // called each time player joins to check if player has this power
    {
        if (UsersSteamIDs.Contains(player.SteamID))
            Users.Add(player);
    }

    public virtual void RegisterHooks() { }
    public virtual void UnRegisterHooks() { }
}

public static class SuperPowerController
{
    //private static Dictionary<ulong, List<ISuperPower>> backup_powers = new Dictionary<ulong, List<ISuperPower>>();
    private static HashSet<ISuperPower> Powers = new HashSet<ISuperPower>();
    private static string mode = "normal";

    public static IEnumerable<ISuperPower> SelectPowers(string pattern)
    {
        string r_pattern = TemUtils.WildCardToRegular(pattern);

        return Powers.Where(p => Regex.IsMatch(TemUtils.GetPowerName(p), r_pattern));
    }

    static SuperPowerController()
    {
        RegisterPower(new DormantPower(), null, CsTeam.None, true); // utilities
        RegisterPower(new BotDisguise(), null, CsTeam.None, true);
        RegisterPower(new BotGuesser(), null, CsTeam.None, true);
        RegisterPower(new Banana(), null, CsTeam.None, true);

        RegisterPower(new BonusHealth()); // powers
        RegisterPower(new BonusArmor());
        RegisterPower(new InstantDefuse(), null, CsTeam.CounterTerrorist);
        RegisterPower(new InstantPlant(), null, CsTeam.Terrorist);
        RegisterPower(new InfiniteAmmo());
        RegisterPower(new SuperSpeed());
        RegisterPower(new HeadshotImmunity());
        RegisterPower(new InfiniteMoney());
        RegisterPower(new NukeNades());
        RegisterPower(new EvilAura());
        RegisterPower(new DamageBonus());
        RegisterPower(new Vampirism());
        RegisterPower(new SuperJump());
        RegisterPower(new Invisibility());
        RegisterPower(new ExplosionUponDeath());
        RegisterPower(new Regeneration());
        RegisterPower(new WarpPeek());
        RegisterPower(new Snowballing());
        RegisterPower(new ChargeJump());
        RegisterPower(new RageMode()); // TODO: rename
        RegisterPower(new HealingZeus());
        RegisterPower(new FlashOfDisability());
        RegisterPower(new PoisonedSmoke());
        RegisterPower(new DamageLoss());
        RegisterPower(new InstantNades());
        RegisterPower(new Pacifism());
        RegisterPower(new Rebirth());
        RegisterPower(new TheSacrifice());
        RegisterPower(new Talisman());
        RegisterPower(new BiocodedWeapons());
        RegisterPower(new EternalNade());

        // cant implement rn
        //Powers.Add(new SmallSize()); // hull size vector is stored as a static variable and all players share the same size
        // should look for da pattern in le memory or somethin idk
        // Powers.Add(new WallHack()); // hav to wrtite check transmit "subsystem" so evry other power coud use it without much headache
        // Powers.Add(new WeaponMaster()); // no recoil?
        // Powers.Add(new Builder()); // needa find models for blocks good enough to make it work
        // Powers.Add(new ShortFusedBomb()); // no luck
    }

    public static void RegisterPower(ISuperPower item, List<ISuperPower>? incompat = null, CsTeam teamReq = 0, bool disable = false)
    {
        if (incompat != null)
            item.Incompatibilities = incompat;

        item.teamNumUsedOnly = teamReq;
        if (disable)
            item.setDisabled();
        Powers.Add(item);
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

    public static ISuperPower GetPowersByName(string name_pattern)
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
        string out_string = "Active:";
        foreach (var p in Powers)
            if (p.Users.Count != 0)
            {
                out_string += "\n\t" + TemUtils.GetPowerName(p) + ":";
                foreach (var user in p.Users)
                    out_string += $"\t{user.PlayerName} ";
            }

        out_string += "\nSaved:";
        foreach (var p in Powers)
            if (p.UsersSteamIDs.Count != 0)
            {
                out_string += "\n\t" + TemUtils.GetPowerName(p) + ":";
                foreach (var id in p.UsersSteamIDs)
                    out_string += $"\t{id} ";
            }

        return out_string;
    }

    public static void Update()
    {
        if (Server.TickCount % 32 == 0)
            CheckDisabled();
        foreach (var power in Powers)
            power.Update();
    }

    public static string Signal(CCSPlayerController? player, List<string> args)
    {
        string ret = "";
        foreach (var power in Powers)
        {
            var sig_ret = power.OnSignal(player, args);
            switch (sig_ret.Item1)
            {
                case SIGNAL_STATUS.ERROR:
                    ret += $"{TemUtils.GetPowerNameReadable(power)} Failed to process the signal: {sig_ret}";
                    break;
                case SIGNAL_STATUS.ACCEPTED:
                    // ret += $"{TemUtils.GetPowerNameReadable(power)} Accepted the signal";
                    break;
            }
        }
        return ret;
    }

    public static void Rejoined(CCSPlayerController player)
    {
        foreach (var power in Powers)
            power.OnRejoin(player);
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
        int cur_tick = Server.TickCount;

        HookResult ret = HookResult.Continue;
        Type type = gameEvent.GetType();

        foreach (var power in Powers)
            if (power.Triggers.Contains(type) || type == typeof(GameEvent))
                if (power.Execute(gameEvent) == HookResult.Stop)
                    ret = HookResult.Stop;

        return ret;
    }

    public static void RegisterHooks()
    {
        foreach (var power in Powers)
            power.RegisterHooks();
    }

    public static void UnRegisterHooks()
    {
        foreach (var power in Powers)
            power.UnRegisterHooks();
    }

    public static void CheckDisabled()
    {
        int cur_tick = Server.TickCount;

        List<CCSPlayerController> forRemoval = [];

        foreach (var power in Powers)
        {
            foreach (var u_tuple in power.UsersDisabled)
                if (u_tuple.Item2 >= cur_tick)
                {
                    forRemoval.Add(u_tuple.Item1);
                    power.OnAdd(u_tuple.Item1); // ignore return code

                    if (!power.Triggers.Contains(typeof(EventRoundStart))) // ignore start round triggered events
                        try
                        { power.Execute(new GameEvent(0)); }
                        catch { }
                }

            foreach (var item in forRemoval)
                power.UsersDisabled.RemoveAll(t => t.Item1 == item);

            forRemoval.Clear();
        }
    }

    public static void DisablePlayer(CCSPlayerController player, int ticks)
    {
        // TemUtils.Log($"Disabling powers for {ticks} ticks");
        int future_tick = Server.TickCount + ticks;
        foreach (var power in Powers)
            if (power.Users.Contains(player))
            {
                power.UsersDisabled.Add(Tuple.Create(player, future_tick));
                power.OnRemovePower(player);
                power.OnRemoveUser(player, false);
            }
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
            p.OnRemovePower(null);
            p.OnRemoveUser(null, false);
            p.Users.Clear();
        }
    }

    public static List<ISuperPower> GetPlayablePowers(SuperPowerConfig cfg, CCSPlayerController player)
    {
        List<ISuperPower> ret = [];

        Powers.ToList().ForEach((power) =>
        {
            if (power.teamNumUsedOnly != CsTeam.None && player.Team == power.teamNumUsedOnly)
                ret.Add(power);
        });

        return ret;
    }

    public static string AddPowerRandomlyToEveryone(SuperPowerConfig cfg, bool silent = true)
    {
        CleanPowers();

        var players = Utilities.GetPlayers();
        if (players.Count == 0)
            return "Error: No players found";

        foreach (var player in players)
        {
        again:
            ISuperPower power = Powers.ElementAt(new Random().Next(Powers.Count));

            if (power.OnAdd(player) == false)
                goto again;
            string alert = $"Your power for this round:\n{ChatColors.Blue}{TemUtils.GetPowerNameReadable(power)}";
            player.PrintToChat(alert);
            string description = $"Power description:\n{ChatColors.Blue}{power.GetDescription()}";
            player.PrintToChat(description);
            if (!silent)
                player.ExecuteClientCommand("play sounds/diagnostics/bell.vsnd");

            try
            { power.Execute(new GameEvent(0)); }
            catch { }
        }

        return "Successfully added random powers to everyone";
    }

    public static string AddPowers(string player_name_pattern, string power_name_pattern, bool now = false, CsTeam team = CsTeam.None, bool silent = true, bool forced = false)
    {
        var status_message = "";
        IEnumerable<CCSPlayerController>? players = null;

        if (team != CsTeam.None)
            players = TemUtils.SelectTeam(team);
        else
            players = TemUtils.SelectPlayers(player_name_pattern);

        if (players == null)
            return "Error: Player(s) not found";

        var powers = SelectPowers(power_name_pattern);
        if (powers == null)
            return "Error: Power(s) not found";

        int added_powers = 0;

        foreach (var player in players)
        {
            string added_powers_feedback = "Server added the following powers to you:\n";
            foreach (var power in powers)
            {
                var powerName = TemUtils.GetPowerName(power);
                bool added = false;

                if (power.Users.Contains(player))
                {
                    status_message += $"{player.PlayerName} already has {powerName}\n";
                    continue;
                }

                try
                {
                    //power.Users.Add(player);
                    if (power.IsDisabled() == false)
                        if (power.OnAdd(player, forced) == true)
                        {
                            added_powers_feedback += $" {ChatColors.Blue}{TemUtils.GetPowerNameReadable(power)}{ChatColors.White},";
                            added_powers++;
                            added = true;
                        }
                }
                catch { status_message += $"Something bad happened while adding {powerName} to {player.PlayerName}, ignoring it\n"; }

                var now_tip = now ? ", now" : "";

                if (added)
                    if (now)
                        try
                        {
                            power.Execute(new GameEvent(0));
                            status_message += $"Added {powerName} to {player.PlayerName}{now_tip}\n";
                            added_powers_feedback += $"{ChatColors.Green}(NOW)";
                        }
                        catch (Exception e)
                        {
                            status_message += $"Something bad happened while triggering {powerName}, ignoring it\n";
                            added_powers_feedback += $"{ChatColors.Red}(FAILED)";
                            Server.PrintToConsole(e.ToString());
                        }
            }

            if (added_powers != 0)
            {
                added_powers_feedback = added_powers_feedback.TrimEnd(',');

                player.PrintToChat(added_powers_feedback);
                if (!silent)
                    player.ExecuteClientCommand("play sounds/diagnostics/bell.vsnd");
            }
        }

        return status_message;
    }

    public static string RemovePowers(string player_name_pattern, string power_name_pattern, CsTeam team = CsTeam.None, bool silent = true, bool reasonDisconnect = false)
    {
        var status_message = "";
        IEnumerable<CCSPlayerController>? players = null;

        if (team != CsTeam.None)
            players = TemUtils.SelectTeam(team);
        else
            players = TemUtils.SelectPlayers(player_name_pattern);

        if (players == null)
            return "Error: Player(s) not found";

        var powers = SelectPowers(power_name_pattern);
        if (powers == null)
            return "Error: Power(s) not found";

        int removed_powers = 0;

        foreach (var player in players)
        {
            string removed_powers_feedback = "Server removed the following powers from you:\n";
            foreach (var power in powers)
            {
                var powerName = TemUtils.GetPowerName(power);

                if (power.Users.Contains(player))
                {
                    try
                    {
                        //power.Users.Remove(player);
                        power.OnRemovePower(player);
                        power.OnRemoveUser(player, reasonDisconnect);
                        removed_powers_feedback += $" {ChatColors.Red}{TemUtils.GetPowerNameReadable(power)}{ChatColors.White},";
                        status_message += $"Removed {powerName} from {player.PlayerName}\n";
                        removed_powers++;
                    }
                    catch { status_message += $"Something bad happened while removing {powerName} from {player.PlayerName}, ignoring it\n"; }
                }
            }

            removed_powers_feedback = removed_powers_feedback.TrimEnd(',');

            if (removed_powers != 0 && reasonDisconnect == false)
            {
                player.PrintToChat(removed_powers_feedback);
                if (!silent)
                    player.ExecuteClientCommand("play sounds/diagnostics/bell.vsnd");
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

    public static void PrecachePowers(ResourceManifest manifest)
    {
        foreach (var power in Powers)
        {
            var model_list = power.NeededResources;
            foreach (var model in model_list)
            {
                manifest.AddResource(model);
                TemUtils.Log("precaching " + model);
            }
        }
    }
}
