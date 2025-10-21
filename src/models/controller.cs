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

public static class SuperPowerController
{
    //private static Dictionary<ulong, List<BasePower>> backup_powers = new Dictionary<ulong, List<BasePower>>();
    private static List<BasePower> Powers = new List<BasePower>();
    private static string mode = "normal";
    public static Tuple<SIGNAL_STATUS, string> ignored_signal = Tuple.Create(SIGNAL_STATUS.IGNORED, "");

    public static IEnumerable<BasePower> SelectPowers(string pattern)
    {
        string r_pattern = TemUtils.WildCardToRegular(pattern);

        return Powers.Where(p => Regex.IsMatch(StringHelpers.GetPowerName(p), r_pattern));
    }

    static SuperPowerController()
    {
        Powers.Add(new DormantPower()); // utilities
        Powers.Add(new BotDisguise());
        Powers.Add(new BotGuesser());
        Powers.Add(new Banana());
        Powers.Add(new BonusHealth()); // powers
        Powers.Add(new BonusArmor());
        Powers.Add(new InstantDefuse());
        Powers.Add(new InstantPlant());
        Powers.Add(new InfiniteAmmo());
        Powers.Add(new SuperSpeed());
        Powers.Add(new HeadshotImmunity());
        Powers.Add(new BitcoinMiner());
        Powers.Add(new NukeNades());
        Powers.Add(new EvilAura());
        Powers.Add(new DamageBonus());
        Powers.Add(new Vampirism());
        Powers.Add(new SuperJump());
        Powers.Add(new Invisibility());
        Powers.Add(new ExplosionUponDeath());
        Powers.Add(new Regeneration());
        Powers.Add(new WarpPeek());
        Powers.Add(new Snowballing());
        Powers.Add(new ChargeJump());
        Powers.Add(new BloodFury());
        Powers.Add(new HealingZeus());
        Powers.Add(new FlashOfDisability());
        Powers.Add(new PoisonedSmoke());
        Powers.Add(new DamageLoss());
        Powers.Add(new InstantNades());
        Powers.Add(new Pacifism());
        Powers.Add(new Rebirth());
        Powers.Add(new TheSacrifice());
        Powers.Add(new SocialSecurity());
        Powers.Add(new BiocodedWeapons());
        Powers.Add(new EternalNade());
        Powers.Add(new GoldenBullet());
        Powers.Add(new RandomLoadout());
        Powers.Add(new FakePassport());
        Powers.Add(new TheShopper());
        Powers.Add(new SmallSize()); // hull size vector is stored as a static variable and all players share the same size
        Powers.Add(new Wallhacks());
        Powers.Add(new SpeedyFella());

        // cant implement rn
        // Powers.Add(new ConcreteSmoke()); // voxel data is so mystical...
        // should look for da pattern in le memory or somethin idk
        // Powers.Add(new WallHack()); // hav to wrtite check transmit "subsystem" so evry other power coud use it without much headache
        // Powers.Add(new WeaponMaster()); // no recoil?
        // Powers.Add(new Builder()); // needa find models for blocks good enough to make it work
        // Powers.Add(new ShortFusedBomb()); // no luck

        Powers.Sort((a, b) => a.priority.CompareTo(b.priority)); // sort by priority
        // Powers.Reverse(); // reverse to have highest priority first
    }

    public static void SetMode(string _mode)
    {
        mode = _mode;
    }

    public static string GetMode()
    {
        return mode;
    }

    public static HashSet<BasePower> GetPowers()
    {
        return Powers.ToHashSet();
    }

    public static BasePower GetPowersByName(string name_pattern)
    {
        var found_powers = SelectPowers(name_pattern);
        return found_powers.First();
    }

    public static List<string> GetPowerList()
    {
        List<string> list = new List<string>();
        foreach (var p in Powers)
            list.Add(StringHelpers.GetPowerName(p));
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
                out_string += "\n\t" + StringHelpers.GetPowerName(p) + ":";
                foreach (var user in p.Users)
                    out_string += $"\t{user.PlayerName} ";
            }

        out_string += "\nSaved:";
        foreach (var p in Powers)
            if (p.UsersSteamIDs.Count != 0)
            {
                out_string += "\n\t" + StringHelpers.GetPowerName(p) + ":";
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
                    // ret += $"{TemUtils.GetPowerNameReadable(power)} Failed to process the signal: {sig_ret}";
                    ret += sig_ret.Item2;
                    break;
                case SIGNAL_STATUS.ACCEPTED:
                    // ret += $"{TemUtils.GetPowerNameReadable(power)} Accepted the signal, response:";
                    ret += sig_ret.Item2;
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
            try { power.ParseCfg(cfg.args[StringHelpers.GetPowerName(power)]); }
            catch { }
    }

    public static void Reconfigure(Dictionary<string, string> configuration, string power_name_pattern)
    {
        var powers = SelectPowers(power_name_pattern);
        foreach (var power in powers)
            power.ParseCfg(configuration);
    }

    public static void CleanInvalidUsers()
    {
        foreach (var power in Powers)
            power.CleanInvalidUsers();
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

    public static bool IsPowerCompatible(CCSPlayerController player, BasePower power)
    {
        foreach (var p in Powers)
            if (p.Users.Contains(player))
            {
                if (p.Incompatibilities.Contains(p.GetType()))
                    return false;
                if (power.Incompatibilities.Contains(p.GetType()))
                    return false;
            }

        return true;
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

    public static List<BasePower> GetCheckTransmitEnabled()
    {
        return Powers.FindAll(p => p.checkTransmitListenerEnabled);
    }

    public static void CheckDisabled()
    {
        int cur_tick = Server.TickCount;

        List<CCSPlayerController> forRemoval = [];

        foreach (var power in Powers)
        {
            foreach (var u_tuple in power.UsersDisabled)
                if (u_tuple.Item2 <= cur_tick)
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

    public static void AddNewPower(BasePower power)
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

    public static List<BasePower> GetPlayablePowers(SuperPowerConfig cfg, CCSPlayerController player)
    {
        List<BasePower> ret = [];

        Powers.ToList().ForEach((power) =>
        {
            if (power.teamReq != CsTeam.None && player.Team == power.teamReq)
                ret.Add(power);
        });

        return ret;
    }

    public static BasePower? GetRandomPower(string rarity)
    {
        var powers = Powers.Where(p => p.Rarity == rarity).ToList();
        if (powers.Count == 0)
            return null;

        return powers.ElementAt(new Random().Next(powers.Count));
    }

    public static bool IsPowerPlayable(CCSPlayerController player, BasePower power)
    {
        if (power.teamReq != CsTeam.None && player.TeamNum != (byte)power.teamReq)
            return false;

        if (!SuperPowerController.IsPowerCompatible(player, power))
            return false;
        return true;
    }

    public static string EnsureEveryoneHasPower(BasePower power)
    {
        var players = Utilities.GetPlayers();
        if (players.Count == 0)
            return "Error: No players found";

        players.ForEach((user) =>
        {
        again:
            if (power.OnAdd(user) == false)
                goto again;
        });

        return $"Successfully added {power.Name} to everyone";
    }

    public static string AddPowerRandomlyToEveryone(SuperPowerConfig cfg, bool silent = true)
    {
        CleanPowers();

        var players = Utilities.GetPlayers();
        if (players.Count == 0)
            return "Error: No players found";

        foreach (var player in players)
        {
            BasePower? power;

            do
            {
                power = Powers.ElementAt(new Random().Next(Powers.Count));
            } while (power.IsDisabled() || power.OnAdd(player) == false);

            // BasePower power = Powers.ElementAt(new Random().Next(Powers.Count));

            // if (power.OnAdd(player) == false)
            //     goto again;
            string alert = $"Your power for this round: {ChatColors.Blue}{StringHelpers.GetPowerNameReadable(power)}";
            player.PrintToChat(alert);
            string description = $"Power description: {ChatColors.Blue}{power.GetDescription()}";
            player.PrintToChat(description);
            if (!silent)
                player.ExecuteClientCommand("play sounds/diagnostics/bell.vsnd");

            // may trigger multiple times
            // try
            // { power.Execute(new GameEvent(0)); } 
            // catch { }
        }

        return "Successfully added random powers to everyone";
    }

    public static string AddPowerOffline(string steam_id_string, string power_name_pattern)
    {
        if (!steam_id_string.StartsWith("@"))
            return "Invalid steamid string";

        ulong steamid64 = ulong.Parse(steam_id_string.Replace("@", ""));

        var powers = SelectPowers(power_name_pattern);
        if (powers == null)
            return "Error: Power(s) not found";

        UInt32 count = 0;
        foreach (var power in powers)
        {
            power.UsersSteamIDs.Add(steamid64);
            count++;
        }

        return "Added to " + count + " powers";
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
                var powerName = StringHelpers.GetPowerName(power);
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
                            added_powers_feedback += $" {ChatColors.Blue}{StringHelpers.GetPowerNameReadable(power)}{ChatColors.White},";
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
                            // Server.PrintToConsole($"debug: adding {powerName} now");
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
                var powerName = StringHelpers.GetPowerName(power);

                if (power.Users.Contains(player))
                {
                    try
                    {
                        //power.Users.Remove(player);
                        power.OnRemovePower(player);
                        power.OnRemoveUser(player, reasonDisconnect);
                        removed_powers_feedback += $" {ChatColors.Red}{StringHelpers.GetPowerNameReadable(power)}{ChatColors.White},";
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
            var power_name = StringHelpers.GetPowerName(power);
            if (power_name == null)
            {
                continue;
            }

            args.Add(power_name, new Dictionary<string, string>());

            AppendFieldsRecursive(args[power_name], power, power.GetType());
        }
        return args;
    }

    private static void AppendFieldsRecursive(Dictionary<string, string> dest, object instance, Type type)
    {
        var iter = type;

        do
        {
            var fields = iter.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var property in fields)
            {
                if (property.IsPublic) continue;

                var property_name = property.Name;
                var property_value = property.GetValue(instance);

                if (property_value != null)
                    dest.Add(property_name, property_value.ToString() ?? "null");
            }

            iter = iter.BaseType;
        } while (iter != null);
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
