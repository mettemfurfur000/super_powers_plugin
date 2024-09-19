using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
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
    public override string ModuleVersion => "0.1.0";
    public override string ModuleAuthor => "tem";

    public SuperPowerConfig Config { get; set; } = new();
    public SuperPowerController? controller;

    public void smwprint(CCSPlayerController? player, string s)
    {
        if (player == null)
            Console.WriteLine(s);
        else
            player.PrintToConsole(s);
    }

    public override void Load(bool hotReload)
    {
        controller = new SuperPowerController(Config)!;

        RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);

        RegisterEventHandler<EventRoundStart>((@event, info) => controller.ExecutePower(@event));
        RegisterEventHandler<EventBombBegindefuse>((@event, info) => controller.ExecutePower(@event));
        RegisterEventHandler<EventBombBeginplant>((@event, info) => controller.ExecutePower(@event));
        RegisterEventHandler<EventWeaponFire>((@event, info) => controller.ExecutePower(@event));
        RegisterEventHandler<EventGrenadeThrown>((@event, info) => controller.ExecutePower(@event));
        RegisterEventHandler<EventPlayerHurt>((@event, info) => controller.ExecutePower(@event));

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            controller.RemovePowers(@event.Userid!.PlayerName, "*");
            return HookResult.Continue;
        });

        RegisterListener<Listeners.OnTick>(() =>
        {
            controller.Update();
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

    [ConsoleCommand("sp_add", "Adds a superpower to specified player. both name of player and superpower have autocompletion if theres only 1 option. \nflag \"now\" will trigger the power instantly")]
    [CommandHelper(minArgs: 2, usage: "[player] [power] optional: (now)", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    //[RequiresPermissions("@css/cvar")]
    public void OnPowerAdd(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var playerNamePattern = commandInfo.GetArg(1);
        var powerNamePattern = commandInfo.GetArg(2);
        var now_flag = false;
        if (commandInfo.ArgCount == 4)
            now_flag = commandInfo.GetArg(3).ToLower().Contains("now");

        smwprint(caller, controller!.AddPowers(playerNamePattern, powerNamePattern, now_flag));
    }

    [ConsoleCommand("sp_remove", "Removes a superpower from specified player. both name of player and superpower have autocompletion if theres only 1 option.")]
    [CommandHelper(minArgs: 2, usage: "[player/*] [power/*]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    //[RequiresPermissions("@css/cvar")]
    public void OnPowerRemove(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var playerNamePattern = commandInfo.GetArg(1);
        var powerNamePattern = commandInfo.GetArg(2);
        smwprint(caller, controller!.RemovePowers(playerNamePattern, powerNamePattern));
    }

    [ConsoleCommand("sp_list", "lists all posibl powers")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    //[RequiresPermissions("@css/cvar")]
    public void OnPowerList(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var powers = controller?.GetPowerList();
        var types = controller?.GetPowerTriggerEvents();
        var out_table = "";
        if (powers != null && types != null)
            for (int i = 0; i < powers.Count; i++)
                out_table += $"\n\t{powers[i]}\t{types[i].ToString().Split(".").Last()}";
        smwprint(player, $"\tsuperpowers\ttriggers\n{out_table}");
    }

    [ConsoleCommand("sp_status", "lists all users of certain powers")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    //[RequiresPermissions("@css/cvar")]
    public void OnPowerStatus(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var powers = controller?.GetPowerList();
        var types = controller?.GetPowerTriggerEvents();
        var out_table = "";
        smwprint(player, $"{out_table}");
    }

    public void OnConfigParsed(SuperPowerConfig config)
    {
        Config = config;
    }
}