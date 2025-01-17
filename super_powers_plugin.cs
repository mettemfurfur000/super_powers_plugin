#define DON_DO_INVIS

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
namespace super_powers_plugin;

/*
[ ] - Custom Health / Shield
[ ] - Changing Player POZ
[ ] - Custom Weapon Data ( Needs more work )
[ ] - Changing Hitbox Size ( so like tiny head or no head hitbox )
[ ] - Ammo / Utility ( Unlimited or increased )
[ ] - Speed / Movement ( Can be changed )
[ ] - 100% accuracy ( even when moving )
[ ] - Instant Bomb plant / defuse
[ ] - Decrease Weapon price (buggy) or Increased Money 
[ ] - Firerate of certain Weapons (all weapons)
[ ] - Auto Bhop / No Jump Fatigue 
[ ] - Score Flipping ( so if it is 12/0 then it will be 0/12 etc )
[ ] - Gain more Health/Shield the more or less you move
[ ] - HealthShots (Full HP & Boost)
[ ] - HE grenades 10x damage & explosion radius
[ ] - Poison gas / Proximity ( Closer to the player the more damage you take )
*/

public class super_powers_plugin : BasePlugin, IPluginConfig<SuperPowerConfig>
{
    public override string ModuleName => "super_powers_plugin";
    public override string ModuleVersion => "0.2.1";
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
        RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);

        RegisterEventHandler<EventRoundStart>((@event, info) =>
        {
            if (SuperPowerController.GetMode() == "random")
                SuperPowerController.AddPowerRandomlyToEveryone(Config);

            smwprint(null, $"Round started, mode: {SuperPowerController.GetMode()}");

            return SuperPowerController.ExecutePower(@event);
        });

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
    }

    private void OnServerPrecacheResources(ResourceManifest manifest)
    {
        /*
        models/props/de_nuke/crate_extrasmall.
        */
        manifest.AddResource("models/food/pizza/pizza_1.vmdl");
        manifest.AddResource("models/food/fruits/banana01a.vmdl");
    }

    public override void Unload(bool hotReload)
    {
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

    [ConsoleCommand("sp_remove", "Removes a superpower from specified player, supports wildcards")]
    [CommandHelper(minArgs: 2, usage: "[player/*] [power/*]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerRemove(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var playerNamePattern = commandInfo.GetArg(1);
        var powerNamePattern = commandInfo.GetArg(2);
        smwprint(caller, SuperPowerController.RemovePowers(playerNamePattern, powerNamePattern));
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
            Server.PrintToConsole(commandInfo.GetArg(i));
            var key = commandInfo.GetArg(i);
            var value = commandInfo.GetArg(i + 1);
            forced_cfg[key] = value;
        }
        SuperPowerController.Reconfigure(forced_cfg, commandInfo.GetArg(1));
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
    }
}