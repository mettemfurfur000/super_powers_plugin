using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using SuperPowersPlugin.Utils;

namespace super_powers_plugin.src;

public class super_powers_plugin : BasePlugin, IPluginConfig<SuperPowerConfig>
{
    public override string ModuleName => "super_powers_plugin";
    public override string ModuleVersion => "0.3.1";
    public override string ModuleAuthor => "tem";
    public SuperPowerConfig Config { get; set; } = new SuperPowerConfig();
    public List<BasePower> checkTransmitTargets = [];
    public static PluginCapability<ISuperPowersController> Capability_SuperPowersController { get; } = new("tem_sp:controllerapi");
    public override void Load(bool hotReload)
    {
        TemUtils.__plugin = this;
        try
        {
            CustomStorage.InitializeDatabase(Config.DataBaseConnectionString);
            CustomStorage.LoadAllPlayerDataFromDatabase();
        }
        catch (Exception e)
        {
            var _ = e.Data;
            Console.WriteLine("Failed to load database, oh dingle!");
            Console.WriteLine("Not that big of a deal, just configure the connection string in super_powers_plugin.json");
            Console.WriteLine("Doesn do anything at the moment");
        }

        // Register our capability

        Capabilities.RegisterPluginCapability(Capability_SuperPowersController, () => new SuperPowerController.CapabilityHandler());

        RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);

        RegisterEventHandler<EventRoundStart>((@event, info) =>
        {
            var theshopper = SuperPowerController.GetPowersByName("the_shopper");

            if (SuperPowerController.GetMode() == "random")
                Server.PrintToConsole(SuperPowerController.AddPowerRandomlyToEveryone(Config));
            if (SuperPowerController.GetMode() == "shop")
                SuperPowerController.EnsureEveryoneHasPower(theshopper);
            Server.PrintToConsole($"Round started, mode: {SuperPowerController.GetMode()}");

            return SuperPowerController.ExecutePower(@event);
        });

        RegisterEventHandler<EventNextlevelChanged>((@event, info) =>
        {
            SuperPowerController.CleanInvalidUsers();
            return HookResult.Continue;
        });
        // converting these keys into my special commands
        // requires some work on the client side though

        List<string> keys_considered = [
        // "uparrow",
        // "downarrow",
        // "leftarrow",
        // "rightarrow",
        ];

        keys_considered.ForEach(key =>
            AddCommand("sp_" + key, "Captures a key being pressed", (player, info) =>
                OnSignalFull(player, info)
            )
        );

        AddCommand("b", "Shopper command", (player, info) => OnSignalFull(player, info));

        // RegisterEventHandler<EventPlayerSpawned>((@event, info) => SuperPowerController.ExecutePower(@event));
        // RegisterEventHandler<EventBulletDamage>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventWeaponReload>((@event, info) => SuperPowerController.ExecutePower(@event));

        RegisterEventHandler<EventRoundStart>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventRoundEnd>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventBombBegindefuse>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventBombBeginplant>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventBombPlanted>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventWeaponFire>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventGrenadeThrown>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventItemPickup>((@event, info) => SuperPowerController.ExecutePower(@event));
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
            // might be expensive
            if (Server.TickCount % 32 == 0)
                SuperPowerController.CleanInvalidUsers();
            SuperPowerController.Update();
        });

        checkTransmitTargets = SuperPowerController.GetCheckTransmitEnabled();

        RegisterListener<Listeners.CheckTransmit>(infoList =>
        {
            if (checkTransmitTargets.Count == 0)
                return;

            foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
            {
                if (player == null || !player.IsValid) // if player is not real he can see the models
                    continue;

                foreach (BasePower power in checkTransmitTargets)
                {
                    var hiddenEntites = power.GetHiddenEntities(player);
                    if (hiddenEntites == null)
                        continue;
                    foreach (var entity in hiddenEntites)
                    {
                        info.TransmitEntities.Remove(entity);
                        // if (power.Name != "wallhacks")
                        //     Server.PrintToggleableAll($"power {power.Name} requested to hide {entity.DesignerName}");
                    }
                }
            }
        });

        SuperPowerController.RegisterHooks();
    }

    private void OnServerPrecacheResources(ResourceManifest manifest)
    {
        SuperPowerController.PrecachePowers(manifest);
    }

    public override void Unload(bool hotReload)
    {
        TemUtils.SetGlobalPlayerHull(1.0f); // reset scaling!

        SuperPowerController.UnRegisterHooks();

        checkTransmitTargets.Clear();

        CustomStorage.CloseDatabase();

        Console.WriteLine("Super Powers Plugin unloaded.");
    }

    [ConsoleCommand("sp_help", "should help in most cases")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnHelp(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        /* this is kinda silly but iduno what ConsoleCommand's description argument even does */

        /* mayber i need a helper function dat woud collect all the command details and assemple this help command automaticaly... yeah very likely
        
        TODO */

        const string player_format = "<player>";
        const string steamid_format = "<steamid64>";
        const string pw_format = "<power>";
        // const string team_format = "[t,ct]";
        commandInfo.ReplyToCommand($"<player> format supports: wildcards (*), team selection (#t, #ct) and steamid64 (@76561199020654675)");
        commandInfo.ReplyToCommand($"<power> format supports: wildcards (*), multiple powers separated by comma (,)");
        commandInfo.ReplyToCommand($"Availiable commands:");
        commandInfo.ReplyToCommand($"  sp_help \t\t\t\t\t\t - should help in most cases");
        commandInfo.ReplyToCommand($"  sp_add {player_format} {pw_format} (now) \t\t\t - adds power to player");
        commandInfo.ReplyToCommand($"  sp_add_offline {steamid_format} {pw_format} \t\t\t - adds power to an offline player");
        commandInfo.ReplyToCommand($"  sp_remove {player_format} {pw_format} \t\t\t - removes power from player");
        commandInfo.ReplyToCommand($"  sp_list {player_format}  \t\t\t\t\t - lists availiable powers");
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

    [ConsoleCommand("sp_add_offline", "Adds a superpower to offline player, SteamID only")]
    [CommandHelper(minArgs: 2, usage: "[steamId] [power]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerAddOffline(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var steamIdString = commandInfo.GetArg(1);
        var powerNamePattern = commandInfo.GetArg(2);

        commandInfo.ReplyToCommand(SuperPowerController.AddPowerOffline(steamIdString, powerNamePattern));
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

    [ConsoleCommand("sp_mode", "todo")]
    [CommandHelper(minArgs: 1, usage: "[mode] - normal, random", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerMode(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var mode = commandInfo.GetArg(1);

        SuperPowerController.SetMode(mode);

        commandInfo.ReplyToCommand($"Mode {mode} set");
    }

    public bool MakeHiddenKnife(CBasePlayerWeapon weapon, bool do_hide)
    {
        List<string> knifes = [
            "weapon_knife",
            // TODO: add all the other knives
        ];

        if (knifes.Contains(weapon.DesignerName))
        {
            // weapon.RenderMode = do_hide ? RenderMode_t.kRenderNone : RenderMode_t.kRenderNormal; 
            // weapon.Render?
            // weapon.AcceptInput("Alpha", null, null, $"{(do_hide ? 0 : 255)}");
            // weapon.AcceptInput("SetModelScale", null, null, $"{(do_hide ? 0 : 1)}");

            // weapon.Render = System.Drawing.Color.FromArgb((do_hide ? 0 : 255), 255, 255, 255);
            // Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_clrRender");
            // none of these work ^^^

            weapon.SetModel("poopmodel");
            return true;
        }

        return false;
    }

    [ConsoleCommand("sp_test", "todo")]
    [CommandHelper(minArgs: 1, usage: "state", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    // [RequiresPermissions("@css/root")]
    public void OnTest(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var do_hide = commandInfo.GetArg(1).ToLower() == "1" || commandInfo.GetArg(1).ToLower() == "true";
        // var manual_group = commandInfo.GetArg(1);
        var manual_group = do_hide ? "2" : "0";
        var player = caller;

        if (player == null || !player.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return;

        var pawn = player.PlayerPawn.Value!;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return;

        // var activeWeapon = weaponServices.ActiveWeapon.Value!;
        // activeWeapon.AcceptInput("SetBodygroup", null, null, $"body,{manual_group}");

        // player.PrintToChat($"Set active weapon bodygroup to {manual_group}");

        var myWeapons = weaponServices.MyWeapons;
        if (myWeapons == null)
            return;

        foreach (var gun in myWeapons)
        {
            if (gun.Value == null || !gun.IsValid || gun.Value.OwnerEntity == null || !gun.Value.OwnerEntity.IsValid)
                continue;

            var entity = gun.Value;

            if (MakeHiddenKnife(entity, do_hide) == false)
            {
                player.PrintToChat($"Setting bodygroup for {entity.DesignerName} to {manual_group}");
                entity.AcceptInput("SetBodyGroup", null, null, $"body,{manual_group}");
            }
            else
                player.PrintToChat($"Setting knife thing for {entity.DesignerName} to {(do_hide ? "hidden" : "normal")}");
        }
    }

    [ConsoleCommand("sp_db_set", "todo")]
    [CommandHelper(minArgs: 1, usage: "state", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    // [RequiresPermissions("@css/root")]
    public void OnDatabaseSetTest(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var value = commandInfo.GetArg(1).ToLower();

        if (caller == null || !caller.IsValid || caller.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return;

        var PlayerData = CustomStorage.GetOrCreatePlayerData(caller)!;

        // CustomStorage.set(steamId, "test_key", state).Wait();

        PlayerData.SetAttribute("test_key", value);

        caller.PrintToChat($"Set \"{value}\"");
    }

    [ConsoleCommand("sp_db_get", "todo")]
    [CommandHelper(minArgs: 0, usage: "state", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    // [RequiresPermissions("@css/root")]
    public void OnDatabaseGetTest(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        if (caller == null || !caller.IsValid || caller.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return;

        var PlayerData = CustomStorage.GetOrCreatePlayerData(caller)!;

        // PlayerData.SetAttribute("test_key", state);

        var value = PlayerData.GetAttribute("test_key");

        caller.PrintToChat($"Got \"{value}\"");
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
                commandInfo.ReplyToCommand($"\t{StringHelpers.GetSnakeName(power.GetType())}\t{power.GetDescriptionPlain()}"
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
            resp += $"Set [{key}] to [{value}]" + (i < commandInfo.ArgCount - 2 ? ", " : "");
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
                commandInfo.ReplyToCommand(StringHelpers.GetPowerNameReadable(power) + ":\n" + power_field_values);
            else
                commandInfo.ReplyToCommand(StringHelpers.GetPowerNameReadable(power) + ": No info");
        }
    }

    [ConsoleCommand("sp_force_signal")]
    [CommandHelper(minArgs: 1, usage: "player [power-specific input args]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnForceSignal(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var playerNamePattern = commandInfo.GetArg(1);
        var players = TemUtils.SelectPlayers(playerNamePattern);

        List<string> args = [];

        const int arg_offset = 1;

        for (int i = arg_offset; i < commandInfo.ArgCount; i++)
            args.Add(commandInfo.GetArg(i));

        foreach (var player in players)
        {
            string ret = SuperPowerController.Signal(player, args);
            if (ret.Length != 0)
                commandInfo.ReplyToCommand(ret);
        }
    }

    [ConsoleCommand("sp_signal")]
    [CommandHelper(minArgs: 1, usage: "[power-specific input args]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnSignalFull(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        List<string> args = [];

        for (int i = 0; i < commandInfo.ArgCount; i++)
            args.Add(commandInfo.GetArg(i));

        string ret = SuperPowerController.Signal(caller, args);
        if (ret.Length != 0)
            commandInfo.ReplyToCommand(ret);
    }

    public void OnConfigParsed(SuperPowerConfig config)
    {
        Config = config;

        SuperPowerController.FeedTheConfig(Config);
    }

    public FakeConVar<bool> silent = new("sp_silent", "", false);
}