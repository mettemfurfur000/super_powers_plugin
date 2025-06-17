using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
namespace super_powers_plugin.src;

public class super_powers_plugin : BasePlugin, IPluginConfig<SuperPowerConfig>
{
    public override string ModuleName => "super_powers_plugin";
    public override string ModuleVersion => "0.2.3";
    public override string ModuleAuthor => "tem";

    public SuperPowerConfig Config { get; set; } = new SuperPowerConfig();

    public override void Load(bool hotReload)
    {
        TemUtils.__plugin = this;

        RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);

        RegisterEventHandler<EventRoundStart>((@event, info) =>
        {
            if (SuperPowerController.GetMode() == "random")
                SuperPowerController.AddPowerRandomlyToEveryone(Config);
            Server.PrintToConsole($"Round started, mode: {SuperPowerController.GetMode()}");

            return SuperPowerController.ExecutePower(@event);
        });

        // surely theres a better way of doing this
        // until then, i dont care

        // there is, but i don care

        // RegisterEventHandler<EventPlayerSpawned>((@event, info) => SuperPowerController.ExecutePower(@event));
        // RegisterEventHandler<EventBulletDamage>((@event, info) => SuperPowerController.ExecutePower(@event));
        
        RegisterEventHandler<EventRoundEnd>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventBombBegindefuse>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventBombBeginplant>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventBombPlanted>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventWeaponFire>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventGrenadeThrown>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventPlayerHurt>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventPlayerSound>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventPlayerJump>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventPlayerDeath>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventBulletImpact>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventItemEquip>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventPlayerBlind>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventSmokegrenadeDetonate>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventSmokegrenadeExpired>((@event, info) => SuperPowerController.ExecutePower(@event));

        RegisterEventHandler<EventHegrenadeDetonate>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventMolotovDetonate>((@event, info) => SuperPowerController.ExecutePower(@event));

        RegisterEventHandler<EventFlashbangDetonate>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventDecoyDetonate>((@event, info) => SuperPowerController.ExecutePower(@event));

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            Server.PrintToConsole(SuperPowerController.RemovePowers(@event.Userid!.PlayerName, "*", CsTeam.None, true, true)); // FIX ME
            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            SuperPowerController.Rejoined(@event.Userid!);
            return HookResult.Continue;
        });

        RegisterListener<Listeners.OnTick>(() =>
        {
            SuperPowerController.Update();
        });

        SuperPowerController.RegisterHooks();
    }

    private void OnServerPrecacheResources(ResourceManifest manifest)
    {
        SuperPowerController.PrecachePowers(manifest);
    }

    public override void Unload(bool hotReload)
    {
        SuperPowerController.UnRegisterHooks();
    }

    [ConsoleCommand("sp_help", "should help in most cases")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnHelp(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        const string pl_format = "<player>";
        const string pw_format = "<power>";
        const string team_format = "[t,ct]";
        commandInfo.ReplyToCommand($"Availiable commands:");
        commandInfo.ReplyToCommand($"  sp_help \t\t\t\t\t\t - should help in most cases");
        commandInfo.ReplyToCommand($"  sp_add {pl_format} {pw_format} (now) \t\t\t - adds power to player");
        commandInfo.ReplyToCommand($"  sp_add_team {team_format} {pw_format} (now)  \t\t\t - adds power to all players of team");
        commandInfo.ReplyToCommand($"  sp_remove {pl_format} {pw_format} \t\t\t - removes power from player");
        commandInfo.ReplyToCommand($"  sp_remove_team {team_format}  {pw_format} \t\t - removes power from all players of team");
        commandInfo.ReplyToCommand($"  sp_list {pl_format}  \t\t\t\t\t - lists availiable powers");
        commandInfo.ReplyToCommand($"  sp_mode [normal, random] \t\t\t\t - sets a special gamemode");
        commandInfo.ReplyToCommand($"flag 'now' triggers the power immediaty");
        commandInfo.ReplyToCommand($"Advanced commands:");
        commandInfo.ReplyToCommand($"  sp_status \t\t\t\t\t\t - prints status of all powers and its users");
        commandInfo.ReplyToCommand($"  sp_inspect {pw_format} \t\t\t\t\t - prints info about power and its parameters");
        commandInfo.ReplyToCommand($"  sp_reconfigure {pw_format} {pw_format} [key1] [value1] ... \t - reconfigures power");
        commandInfo.ReplyToCommand($"Special:");
        commandInfo.ReplyToCommand($"  sp_signal / signal / s <any input> - pass a signal of arbitrary data to the plugin system");
    }

    [ConsoleCommand("sp_add", "Adds a superpower to specified player, supports wildcards")]
    [CommandHelper(minArgs: 2, usage: "[player] [power] optional: (now)", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerAdd(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var playerNamePattern = commandInfo.GetArg(1);
        var powerNamePattern = commandInfo.GetArg(2);
        var now_flag = false;
        var force_flag = false;
        if (commandInfo.ArgCount >= 4)
        {
            now_flag = commandInfo.GetArg(3).ToLower().Contains("now");
            force_flag = commandInfo.GetArg(3).ToLower().Contains("force");
        }

        commandInfo.ReplyToCommand(SuperPowerController.AddPowers(playerNamePattern, powerNamePattern, now_flag, CsTeam.None, true, force_flag));
    }

    [ConsoleCommand("sp_add_team", "Adds a superpower to specified team, supports wildcards")]
    [CommandHelper(minArgs: 2, usage: "[ct/t] [power/*] optional: (now)", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerAddTeam(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var teamStr = commandInfo.GetArg(1);
        var powerNamePattern = commandInfo.GetArg(2);
        var now_flag = false;
        var force_flag = false;
        if (commandInfo.ArgCount >= 4)
        {
            now_flag = commandInfo.GetArg(3).ToLower().Contains("now");
            force_flag = commandInfo.GetArg(3).ToLower().Contains("force");
        }

        CsTeam csteam = TemUtils.ParseTeam(teamStr);
        if (csteam == CsTeam.None)
            commandInfo.ReplyToCommand($"Unrecognized option: {teamStr}");
        else
            commandInfo.ReplyToCommand(SuperPowerController.AddPowers("unused", powerNamePattern, now_flag, csteam, true, force_flag));
    }

    [ConsoleCommand("sp_remove", "Removes a superpower from specified player, supports wildcards")]
    [CommandHelper(minArgs: 2, usage: "[player/*] [power/*]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerRemove(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var playerNamePattern = commandInfo.GetArg(1);
        var powerNamePattern = commandInfo.GetArg(2);
        commandInfo.ReplyToCommand(SuperPowerController.RemovePowers(playerNamePattern, powerNamePattern));
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
            commandInfo.ReplyToCommand($"Unrecognized option: {teamStr}");
        else
            commandInfo.ReplyToCommand(SuperPowerController.RemovePowers("unused", powerNamePattern, csteam));
    }

    [ConsoleCommand("sp_mode", "todo")]
    [CommandHelper(minArgs: 1, usage: "[mode] - normal, random", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerMode(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var mode = commandInfo.GetArg(1);

        SuperPowerController.SetMode(mode);

        commandInfo.ReplyToCommand($"Mode {mode} set");
    }

    [ConsoleCommand("sp_list", "lists all posibl powers")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerList(CCSPlayerController? player, CommandInfo commandInfo)
    {
        var powers = SuperPowerController.GetPowers();
        commandInfo.ReplyToCommand($"\tsuperpowers\n");

        if (powers != null)
            foreach (var power in powers)
                commandInfo.ReplyToCommand($"\t{TemUtils.GetSnakeName(power.GetType())}\t{power.GetDescription()}"
                + (power.IsDisabled() ? "\t(Disabled)" : "")
                + (power.teamReq != CsTeam.None ? (
                    power.teamReq == CsTeam.Terrorist ? "\t(T Only)" : "\t(CT Only)"
                ) : "") + "\n");

        // commandInfo.ReplyToCommand($"\tsuperpowers\ttriggers\n{out_table}");
    }

    [ConsoleCommand("sp_status", "lists all users of certain powers")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerStatus(CCSPlayerController? player, CommandInfo commandInfo)
    {
        commandInfo.ReplyToCommand($"{SuperPowerController.GetUsersTable()}");
    }

    [ConsoleCommand("sp_reconfigure", "parses your input as a config and applies it")]
    [CommandHelper(minArgs: 2, usage: "[power] [key1] [value1] [key2] [value2] ...", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnReconfigure(CCSPlayerController? player, CommandInfo commandInfo)
    {
        Dictionary<string, string> forced_cfg = [];
        string resp = "";
        for (int i = 2; i < commandInfo.ArgCount; i += 2) // iterate over all args, except 0 and 1, which is just the name of the command and name of power
        {
            var key = commandInfo.GetArg(i);
            var value = commandInfo.GetArg(i + 1);
            forced_cfg[key] = value;
            resp += $"Set [{key}] to [{value}]";
        }
        SuperPowerController.Reconfigure(forced_cfg, commandInfo.GetArg(1));
        commandInfo.ReplyToCommand("Reconfigured!\n" + resp);
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
            commandInfo.ReplyToCommand($"No powers found for {powerNamePattern}");
            return;
        }

        foreach (var power in powers)
        {
            string? power_field_values = TemUtils.InspectPowerReflective(power, power.GetType());
            if (power_field_values != null)
                commandInfo.ReplyToCommand(TemUtils.GetPowerNameReadable(power) + ":\n" + power_field_values);
        }
    }


    [ConsoleCommand("sp_signal")]
    [CommandHelper(minArgs: 2, usage: "[power-specific input args]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnSignalFull(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        List<string> args = [];

        for (int i = 1; i < commandInfo.ArgCount; i++)
            args.Add(commandInfo.GetArg(i));

        string ret = SuperPowerController.Signal(caller, args);
        if (ret.Length != 0)
            commandInfo.ReplyToCommand(ret);
    }

    [ConsoleCommand("signal")]
    [CommandHelper(minArgs: 2, usage: "[power-specific input args]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnSignal(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        OnSignalFull(caller, commandInfo);
    }

    [ConsoleCommand("s")]
    [CommandHelper(minArgs: 2, usage: "[power-specific input args]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnSignalShort(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        OnSignalFull(caller, commandInfo);
    }


    public void OnConfigParsed(SuperPowerConfig config)
    {
        Config = config;

        SuperPowerController.FeedTheConfig(Config);
    }
}