#define DON_DO_INVIS

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
namespace super_powers_plugin.src;

public class super_powers_plugin : BasePlugin, IPluginConfig<SuperPowerConfig>
{
    public override string ModuleName => "super_powers_plugin";
    public override string ModuleVersion => "0.2.2";
    public override string ModuleAuthor => "tem";

    public SuperPowerConfig Config { get; set; } = new SuperPowerConfig();

    public void smwprint(CCSPlayerController? player, string s)
    {
        if (player == null)
            TemUtils.Print(s, ChatColors.Default);
        else
            player.PrintToConsole(s);
    }

    public override void Load(bool hotReload)
    {
        TemUtils.__plugin = this;

        RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);

        RegisterEventHandler<EventRoundStart>((@event, info) =>
        {
            if (SuperPowerController.GetMode() == "random")
                SuperPowerController.AddPowerRandomlyToEveryone(Config);

            // if (SuperPowerController.GetMode() == "roguelike")
            //     SuperPowerController.AddMenuViewerPowerToEveryone();

            smwprint(null, $"Round started, mode: {SuperPowerController.GetMode()}");

            return SuperPowerController.ExecutePower(@event);
        });

        // surely theres a better way of doing this
        // until then, i dont care

        RegisterEventHandler<EventBombBegindefuse>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventBombBeginplant>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventWeaponFire>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventGrenadeThrown>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventPlayerHurt>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventPlayerSound>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventPlayerJump>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventPlayerDeath>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventBulletImpact>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventItemEquip>((@event, info) => SuperPowerController.ExecutePower(@event));

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            //SuperPowerUsersStorage.OnPlayerDisconnected(@event.Userid!);
            // Server.PrintToConsole($"guy disconencted - {@event.Userid!.PlayerName}");
            // Server.PrintToConsole($"userid disconencted - {@event.Userid!.SteamID}");
            Server.PrintToConsole(SuperPowerController.RemovePowers(@event.Userid!.PlayerName, "*", CsTeam.None, true, true)); // FIX ME
            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            //Server.PrintToConsole($"guy joined - {@event.Userid!.PlayerName}");
            SuperPowerController.Rejoined(@event.Userid!);
            //SuperPowerUsersStorage.OnPlayerConnected(@event.Userid!);
            return HookResult.Continue;
        });

        RegisterListener<Listeners.OnTick>(() =>
        {
            SuperPowerController.Update();
        });

        // temp
        // VirtualFunctions.CCSPlayerPawnBase_PostThinkFunc.Hook(OnPostThink, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
    }

    private void OnServerPrecacheResources(ResourceManifest manifest)
    {
        SuperPowerController.PrecachePowers(manifest);
    }

    public override void Unload(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
        // VirtualFunctions.CCSPlayerPawnBase_PostThinkFunc.Unhook(OnPostThink, HookMode.Pre);
    }

    [ConsoleCommand("sp_help", "should help in most cases")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnHelp(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        const string player_select_format = "<player>";
        const string power_select_format = "<power>";
        const string team_format = "[t,ct]";
        smwprint(caller, $"Availiable commands:");
        smwprint(caller, $"  sp_help \t\t\t\t\t\t - should help in most cases");
        smwprint(caller, $"  sp_add {player_select_format} {power_select_format} (now) \t\t\t - adds power to player");
        smwprint(caller, $"  sp_add_team {team_format} {power_select_format} (now)  \t\t\t - adds power to all players of team");
        smwprint(caller, $"  sp_remove {player_select_format} {power_select_format} (now) \t\t\t - removes power from player");
        smwprint(caller, $"  sp_remove_team {team_format}  {power_select_format} (now) \t\t - removes power from all players of team");
        smwprint(caller, $"  sp_list {player_select_format}  \t\t\t\t\t - lists powers of player");
        smwprint(caller, $"  sp_mode [normal, random] \t\t\t\t - sets a special gamemode");
        smwprint(caller, $"flag 'now' triggers the power immediaty");
        smwprint(caller, $"Advanced commands:");
        smwprint(caller, $"  sp_status \t\t\t\t\t\t - prints status of all powers and its users");
        smwprint(caller, $"  sp_inspect {power_select_format} \t\t\t\t\t - prints info about power and its parameters");
        smwprint(caller, $"  sp_reconfigure {power_select_format} {power_select_format} [key1] [value1] ... \t - reconfigures power");
    }

    [ConsoleCommand("sp_add", "Adds a superpower to specified player, supports wildcards")]
    [CommandHelper(minArgs: 2, usage: "[player] [power] optional: (now)", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerAdd(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var playerNamePattern = commandInfo.GetArg(1);
        var powerNamePattern = commandInfo.GetArg(2);
        var now_flag = false;
        if (commandInfo.ArgCount >= 4)
            now_flag = commandInfo.GetArg(3).ToLower().Contains("now");

        smwprint(caller, SuperPowerController.AddPowers(playerNamePattern, powerNamePattern, now_flag));
    }

    [ConsoleCommand("sp_add_team", "Adds a superpower to specified team, supports wildcards")]
    [CommandHelper(minArgs: 2, usage: "[ct/t] [power/*] optional: (now)", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerAddTeam(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var teamStr = commandInfo.GetArg(1);
        var powerNamePattern = commandInfo.GetArg(2);
        var now_flag = false;
        if (commandInfo.ArgCount >= 4)
            now_flag = commandInfo.GetArg(3).ToLower().Contains("now");

        CsTeam csteam = TemUtils.ParseTeam(teamStr);
        if (csteam == CsTeam.None)
            smwprint(caller, $"Unrecognized option: {teamStr}");
        else
            smwprint(caller, SuperPowerController.AddPowers("unused", powerNamePattern, now_flag, csteam));
    }

    [ConsoleCommand("sp_remove", "Removes a superpower from specified player, supports wildcards")]
    [CommandHelper(minArgs: 2, usage: "[player/*] [power/*]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerRemove(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var playerNamePattern = commandInfo.GetArg(1);
        var powerNamePattern = commandInfo.GetArg(2);
        smwprint(caller, SuperPowerController.RemovePowers(playerNamePattern, powerNamePattern));
    }

    [ConsoleCommand("sp_remove_team", "Removes a superpower from specified team, supports wildcards")]
    [CommandHelper(minArgs: 2, usage: "[ct/t] [power/*]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerRemoveTeam(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var teamStr = commandInfo.GetArg(1);
        var powerNamePattern = commandInfo.GetArg(2);

        CsTeam csteam = TemUtils.ParseTeam(teamStr);
        if (csteam == CsTeam.None)
            smwprint(caller, $"Unrecognized option: {teamStr}");
        else
            smwprint(caller, SuperPowerController.RemovePowers("unused", powerNamePattern, csteam));
    }

    [ConsoleCommand("sp_mode", "todo")]
    [CommandHelper(minArgs: 1, usage: "[mode] - normal, random", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerMode(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var mode = commandInfo.GetArg(1);

        SuperPowerController.SetMode(mode);

        smwprint(caller, $"Mode {mode} set");
    }



    [ConsoleCommand("sp_list", "lists all posibl powers")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerList(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var powers = SuperPowerController.GetPowerList();
        var types = SuperPowerController.GetPowerTriggerEvents();
        var out_table = "";
        if (powers != null && types != null)
            for (int i = 0; i < powers.Count; i++)
            {
                out_table += $"\n\t{powers[i]}\t";
                foreach (var type in types[i])
                    out_table += $"{TemUtils.GetSnakeName(type)}, ";
            }

        smwprint(player, $"\tsuperpowers\ttriggers\n{out_table}");
    }

    [ConsoleCommand("sp_status", "lists all users of certain powers")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerStatus(CCSPlayerController? player, CommandInfo commandInfo)
    {
        smwprint(player, $"{SuperPowerController.GetUsersTable()}");
    }

    [ConsoleCommand("sp_reconfigure", "parses your input as a config and applies it")]
    [CommandHelper(minArgs: 2, usage: "[power] [key1] [value1] [key2] [value2] ...", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnReconfigure(CCSPlayerController? player, CommandInfo commandInfo)
    {
        Dictionary<string, string> forced_cfg = [];
        for (int i = 2; i < commandInfo.ArgCount; i += 2) // iterate over all args, except 0 and 1, which is just the name of the command and name of power
        {
            var key = commandInfo.GetArg(i);
            var value = commandInfo.GetArg(i + 1);
            forced_cfg[key] = value;
        }
        SuperPowerController.Reconfigure(forced_cfg, commandInfo.GetArg(1));
        Config.args = SuperPowerController.GenerateDefaultConfig();
        TemConfigExtensions.Update(Config);
        smwprint(player, "Reconfigured!");
    }

    [ConsoleCommand("sp_inspect", "reflects on a power class and dumps its values")]
    [CommandHelper(minArgs: 1, usage: "[power]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnInspect(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var powerNamePattern = commandInfo.GetArg(1);

        var powers = SuperPowerController.SelectPowers(powerNamePattern);
        if (powers == null)
        {
            smwprint(player, $"No powers found for {powerNamePattern}");
            return;
        }

        foreach (var power in powers)
        {
            string? power_field_values = TemUtils.InspectPowerReflective(power, power.GetType());
            if (power_field_values != null)
                smwprint(player, TemUtils.GetPowerNameReadable(power) + ":\n" + power_field_values);
        }
    }

    public void OnConfigParsed(SuperPowerConfig config)
    {
        Config = config;

        SuperPowerController.FeedTheConfig(Config);

        // MenuManager.Config = config;
    }

    private static HookResult OnTakeDamage(DynamicHook hook)
    {
        return HookResult.Continue;
    }

    // private static HookResult OnPostThink(DynamicHook hook)
    // {
    //     CCSPlayerPawnBase? pawnBase = hook.GetParam<CCSPlayerPawnBase>(0);
    //     if (pawnBase == null)
    //         return HookResult.Continue;

    //     pawnBase.ViewOffset.Z = 10;
    //     Utilities.SetStateChanged(pawnBase, "CBaseModelEntity", "m_vecViewOffset");
    //     // player.PrintToConsole("maggot");

    //     Server.PrintToConsole("maggot");

    //     return HookResult.Continue;
    // }

}