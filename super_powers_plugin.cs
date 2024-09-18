using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
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
    public override string ModuleVersion => "0.0.3";
    public override string ModuleAuthor => "tem";
    public SuperPowerConfig Config { get; set; } = new();
    public SuperPowerController? controller;
    public void smwprint(CCSPlayerController? player, string s)
    {
        if (player == null)
            Console.WriteLine(s);
        else
            player.PrintToChat(s);
    }

    private CCSPlayerController? FindPlayer(string name)
    {
        var players = Utilities.GetPlayers().Where(p => p.PlayerName.Contains(name)).ToList();
        if (players.Count != 1)
            return null;
        return players.First();
    }
    public override void Load(bool hotReload)
    {
        controller = new SuperPowerController(Config);

        RegisterEventHandler<EventRoundStart>((@event, info) =>
        {
            controller.ExecutePower(@event);
            return HookResult.Continue;
        });

        RegisterEventHandler<EventBombBegindefuse>((@event, info) =>
        {
            controller.ExecutePower(@event);
            return HookResult.Continue;
        });

        RegisterEventHandler<EventGrenadeThrown>((@event, info) =>
        {
            return HookResult.Continue;
        });



        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            controller.RemoveUser(@event.Userid);
            return HookResult.Continue;
        });
    }

    public override void Unload(bool hotReload)
    {
    }

    // Commands can also be registered using the `Command` attribute.
    [ConsoleCommand("sp_add", "Adds a superpower to specified player. both name of player and superpower have autocompletion if theres only 1 option")]
    // The `CommandHelper` attribute can be used to provide additional information about the command.
    [CommandHelper(minArgs: 2, usage: "[player] [power]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    //[RequiresPermissions("@css/cvar")]
    public void OnPowerAdd(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var playerName = commandInfo.GetArg(1);
        var powerName = commandInfo.GetArg(2);

        var player = FindPlayer(playerName);
        if (player == null)
        {
            smwprint(caller, "Player not found or found multiple players matching the same pattern");
            return;
        }

        if (controller?.AddUser(player, powerName) == 0)
            smwprint(caller, $"Added {powerName} to {playerName}");
        else
            smwprint(caller, $"Failed to add {powerName} to {playerName}");
    }

    // Commands can also be registered using the `Command` attribute.
    [ConsoleCommand("sp_list", "lists all posibl powers")]
    // The `CommandHelper` attribute can be used to provide additional information about the command.
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    //[RequiresPermissions("@css/cvar")]
    public void OnPowerList(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var powers = controller?.GetPowerList();
        if (powers != null)
            foreach (var item in powers)
                smwprint(player, $"{item}");
    }

    // Commands can also be registered using the `Command` attribute.
    [ConsoleCommand("sp_trigger", "triggers power right now")]
    // The `CommandHelper` attribute can be used to provide additional information about the command.
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    //[RequiresPermissions("@css/cvar")]
    public void OnDebugForcePower(CCSPlayerController? player, CommandInfo commandInfo)
    {
        // var pawn = player.Pawn.Value;

        // if (pawn != null)
        // {
        //     //pawn.MaxHealth = 500;
        //     //Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
        //     pawn.Health = 500;
        //     Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        // }
        // else
        // {
        //     smwprint(player, "No pawn!");
        // }
    }
    public void OnConfigParsed(SuperPowerConfig config)
    {
        if (config.Version < Config.Version)
        {
            // Logger.LogWarning("Update plugin config. New version: {Version}", Config.Version);
        }

        Config = config;
    }
}