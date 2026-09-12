using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using PanoramaManager;
using SuperPowersPlugin.Utils;
using super_powers_plugin.src.hud;

namespace super_powers_plugin.src;

public class super_powers_plugin : BasePlugin, IPluginConfig<SuperPowerConfig>
{
    public override string ModuleName => "super_powers_plugin";
    public override string ModuleVersion => "0.5.0";
    public override string ModuleAuthor => "tem";
    public SuperPowerConfig Config { get; set; } = new SuperPowerConfig();
    public List<BasePower> checkTransmitTargets = [];
    public static PluginCapability<ISuperPowersController> Capability_SuperPowersController { get; } = new("tem_sp:controllerapi");
    public override void Load(bool hotReload)
    {
        TemUtils.__plugin = this;

        // Pre-load native SQLite library so it's in the process-wide load address space
        // SQLitePCLRaw's P/Invoke needs this but doesn't search the plugin directory.
        var pluginDir = ModuleDirectory;
        var nativeSqlite = Path.Combine(pluginDir,
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "e_sqlite3.dll" : "libe_sqlite3.so");
        try { if (File.Exists(nativeSqlite)) NativeLibrary.Load(nativeSqlite); }
        catch { }

        try
        {
            CustomStorage.InitializeDatabase(Config.DataBaseConnectionString, Config.StandaloneDatabase);
            CustomStorage.LoadAllPlayerDataFromDatabase();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[super_powers_plugin] Database init failed: {ex.GetType().Name} — {ex.Message}");
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
        RegisterEventHandler<EventPlayerSpawn>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventPlayerBlind>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventSmokegrenadeDetonate>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventSmokegrenadeExpired>((@event, info) => SuperPowerController.ExecutePower(@event));

        RegisterEventHandler<EventHegrenadeDetonate>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventMolotovDetonate>((@event, info) => SuperPowerController.ExecutePower(@event));

        RegisterEventHandler<EventFlashbangDetonate>((@event, info) => SuperPowerController.ExecutePower(@event));
        RegisterEventHandler<EventDecoyDetonate>((@event, info) => SuperPowerController.ExecutePower(@event));

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            AsciiOverlay?.Close(@event.Userid!);
            SuperPowerController.SaveAllProgressions(@event.Userid!);
            Server.PrintToConsole(SuperPowerController.RemovePowers(@event.Userid!.PlayerName, "*", CsTeam.None, true, true)); // FIX ME
            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            SuperPowerController.Rejoined(@event.Userid!);
            SuperPowerController.LoadAllProgressions(@event.Userid!);
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

        RegisterEventHandler<EventServerSpawn>((@event, info) =>
        {
            // Server globals are ready here — safe to call Utilities.GetPlayers()
            SuperPowerController.LoadAllConnectedProgressions();
            // SuperPowerController.CleanInvalidUsers();
            Server.PrintToConsole("Server spawned");
            return HookResult.Continue;
        });

        SuperPowerController.RegisterHooks();

        // Initialize Panorama ONCE for the entire plugin
        try
        {
            Panorama.UseGlobalDialogVariables = true;
            Panorama.Init(this);
            Console.WriteLine("[super_powers_plugin] Panorama.Init OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[super_powers_plugin] Panorama.Init FAILED: {ex.Message}");
        }

        try
        {
            AsciiOverlay = new AsciiOverlayManager();
            AsciiOverlay.Init(this);
            Console.WriteLine("[super_powers_plugin] ASCII overlay initialized");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[super_powers_plugin] ASCII overlay init failed: {ex.Message}");
        }

        if (hotReload)
            SuperPowerController.LoadAllConnectedProgressions();
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

        AsciiOverlay?.Shutdown();
        AsciiOverlay = null;

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
        commandInfo.ReplyToCommand($"  sp_ascii <0-12> \t\t\t\t\t - ASCII overlay test suite");
        commandInfo.ReplyToCommand($"  sp_ascii_close \t\t\t\t\t - close the ASCII overlay");
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
    static MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr>? CBaseEntity_SetSizeFunc = null;
    // new("48 8B C4 48 89 58 ? 48 89 68 ? 48 89 70 ? 48 89 78 ? 41 56 48 83 EC ? F2 0F 10 02", Addresses.ServerPath);
    // new("48 81 C1 ? ? ? ? E9 ? ? ? ? CC CC CC CC 48 89 5C 24 ? 55 56 57 48 8D 6C 24", Addresses.ServerPath);


    [ConsoleCommand("sp_test", "todo")]
    [CommandHelper(minArgs: 1, usage: "state", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    // [RequiresPermissions("@css/root")]
    public void OnTest(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        if (caller == null)
            return;

        // CBaseEntity_SetSizeFunc = new("48 81 C1 ? ? ? ? E9 ? ? ? ? CC CC CC CC 48 83 EC ? 4C 8B C2", Addresses.ServerPath);// works with entities
        CBaseEntity_SetSizeFunc = new("48 8B C4 48 89 58 ? 48 89 68 ? 48 89 70 ? 48 89 78 ? 41 56 48 83 EC ? F2 0F 10 02", Addresses.ServerPath); // works with collision properties

        if (CBaseEntity_SetSizeFunc == null)
        {
            Server.PrintToChatAll("not found the function");
            return;
        }

        Vector vector_min_test = new Vector(-8, -8, 0);
        Vector vector_max_test = new Vector(8, 8, 8);

        IntPtr ret = CBaseEntity_SetSizeFunc.Invoke(caller.PlayerPawn.Value!.Collision.Handle, vector_min_test.Handle, vector_max_test.Handle);
        // IntPtr ret = CBaseEntity_SetSizeFunc.Invoke(caller.PlayerPawn.Value.Handle, vector_min_test.Handle, vector_max_test.Handle);

        Server.PrintToChatAll("ret ptr = " + ret);
    }

    [ConsoleCommand("sp_trace", "traces a ray from your eyes along your view, prints where it lands")]
    [CommandHelper(minArgs: 0, usage: "[contents flags (comma separated)] [exclude flags] [self]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    // [RequiresPermissions("@css/root")]
    public void OnTraceTest(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        if (caller == null || !caller.IsValid || caller.PlayerPawn.Value == null || !caller.PlayerPawn.Value.IsValid)
            return;

        var pawn = caller.PlayerPawn.Value;
        var eyePos = pawn.GetEyePosition();
        if (eyePos == null)
            return;

        var angles = pawn.V_angle;

        commandInfo.ReplyToCommand($"eye = ({eyePos.X:F1}, {eyePos.Y:F1}, {eyePos.Z:F1}), view = ({angles.X:F1}, {angles.Y:F1}, {angles.Z:F1})");

        bool ignoreSelf = true; // 'self' as an arg disables this, so the ray is allowed to hit you
        Contents customMask = 0;
        Contents customExclude = 0;
        bool hasCustomMask = false;
        bool hasCustomExclude = false;

        for (int i = 1; i < commandInfo.ArgCount; i++)
        {
            var arg = commandInfo.GetArg(i);

            if (arg.Equals("self", StringComparison.OrdinalIgnoreCase))
            {
                ignoreSelf = false;
                continue;
            }

            bool isExcludeList = arg.StartsWith("-");
            foreach (var rawName in arg.TrimStart('-').Split(','))
            {
                if (!Enum.TryParse<Contents>(rawName.Trim(), true, out var flag))
                {
                    commandInfo.ReplyToCommand($"unknown contents flag '{rawName}', valid: {string.Join(", ", Enum.GetNames<Contents>())}");
                    continue;
                }

                if (isExcludeList)
                {
                    customExclude |= flag;
                    hasCustomExclude = true;
                }
                else
                {
                    customMask |= flag;
                    hasCustomMask = true;
                }
            }
        }

        void RunTrace(string label, Contents mask, Contents exclude, CBaseEntity? ignoreEntity)
        {
            var options = new TraceOptions
            {
                InteractsAs = Contents.Solid,
                InteractsWith = mask,
                InteractsExclude = exclude
            };

            var result = Trace.TraceShape(eyePos, angles, ignoreEntity, options);

            string hitInfo = result.DidHit()
                ? $"HIT frac={result.Fraction:F3} end=({result.EndPos.X:F0}, {result.EndPos.Y:F0}, {result.EndPos.Z:F0}) ent={DescribeEntity(result.HitEntity())}"
                : "NO HIT";

            commandInfo.ReplyToCommand($"[{label}] {hitInfo}");
        }

        void RunTraceEnd(string label, Contents mask, Contents exclude, CBaseEntity? ignoreEntity)
        {
            // same ray as RunTrace but through the segment based overload the powers use
            double pitch = angles.X * Math.PI / 180.0;
            double yaw = angles.Y * Math.PI / 180.0;
            var endPos = new Vector(
                eyePos.X + (float)(Math.Cos(pitch) * Math.Cos(yaw)) * 8192f,
                eyePos.Y + (float)(Math.Cos(pitch) * Math.Sin(yaw)) * 8192f,
                eyePos.Z + (float)(-Math.Sin(pitch)) * 8192f);

            var options = new TraceOptions
            {
                InteractsAs = Contents.Solid,
                InteractsWith = mask,
                InteractsExclude = exclude
            };

            var result = Trace.TraceEndShape(eyePos, endPos, ignoreEntity, options);

            string hitInfo = result.DidHit()
                ? $"HIT frac={result.Fraction:F3} end=({result.EndPos.X:F0}, {result.EndPos.Y:F0}, {result.EndPos.Z:F0}) ent={DescribeEntity(result.HitEntity())}"
                : "NO HIT";

            commandInfo.ReplyToCommand($"[{label}] {hitInfo}");
        }

        static string DescribeEntity(CEntityInstance? entity)
        {
            if (entity == null || !entity.IsValid)
                return "<none>";
            try { return $"{entity.DesignerName}({entity.Index})"; }
            catch { return "<invalid>"; }
        }

        if (hasCustomMask || hasCustomExclude)
        {
            RunTrace("custom", customMask, customExclude, ignoreSelf ? pawn : null);
            RunTraceEnd("custom end-shape", customMask, customExclude, ignoreSelf ? pawn : null);
            return;
        }

        // no args - sweep through common masks to see which ones behave
        RunTrace("solid", Contents.Solid, 0, ignoreSelf ? pawn : null);
        RunTrace("solid|window", Contents.Solid | Contents.Window, 0, ignoreSelf ? pawn : null);
        RunTrace("solid|window|playerclip", Contents.Solid | Contents.Window | Contents.PlayerClip, 0, ignoreSelf ? pawn : null);
        RunTrace("+players", Contents.Solid | Contents.Window | Contents.PlayerClip | Contents.Player, 0, ignoreSelf ? pawn : null);
        RunTrace("everything", (Contents)ulong.MaxValue, 0, ignoreSelf ? pawn : null);
        RunTrace("everything noself-ignore", (Contents)ulong.MaxValue, 0, null);

        // segment based overloads - this is what radiation/supply closet actually call
        RunTraceEnd("end-shape solid|window", Contents.Solid | Contents.Window, 0, ignoreSelf ? pawn : null);
        RunTraceEnd("end-shape everything", (Contents)ulong.MaxValue, 0, ignoreSelf ? pawn : null);
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

    [ConsoleCommand("sp_pstats", "Shows leveling stats for a player's powers")]
    [CommandHelper(minArgs: 1, usage: "<player> [power]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnPowerStats(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var playerPattern = commandInfo.GetArg(1);
        var players = TemUtils.SelectPlayers(playerPattern);
        if (players == null || !players.Any())
        {
            commandInfo.ReplyToCommand("No players found");
            return;
        }

        foreach (var player in players)
        {
            commandInfo.ReplyToCommand($"Levels for {player.PlayerName}:");

            IEnumerable<BasePower> powers;
            if (commandInfo.ArgCount >= 3)
                powers = SuperPowerController.SelectPowers(commandInfo.GetArg(2));
            else
                powers = SuperPowerController.GetPowers();

            foreach (var power in powers)
            {
                if (!power.SupportsLeveling) continue;
                if (power.PlayerProgression.TryGetValue(player.SteamID, out var prog))
                {
                    var name = StringHelpers.GetPowerNameReadable(power);
                    int xpNeeded = (int)(power.cfg_levelUpBase * Math.Pow(power.cfg_levelUpMultiplier, prog.Level));
                    commandInfo.ReplyToCommand($"  {name}: Level {prog.Level}, XP {prog.XP}/{xpNeeded}");
                }
                else
                    commandInfo.ReplyToCommand($"  {StringHelpers.GetPowerNameReadable(power)}: Not yet leveled");
            }
        }
    }

    [ConsoleCommand("sp_setlevel", "Sets level for a player's power (admin override)")]
    [CommandHelper(minArgs: 3, usage: "<player> <power> <level>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnSetLevel(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        var playerPattern = commandInfo.GetArg(1);
        var powerPattern = commandInfo.GetArg(2);
        if (!int.TryParse(commandInfo.GetArg(3), out var level))
        {
            commandInfo.ReplyToCommand("Invalid level value");
            return;
        }

        var players = TemUtils.SelectPlayers(playerPattern);
        if (players == null || !players.Any())
        {
            commandInfo.ReplyToCommand("No players found");
            return;
        }

        var powers = SuperPowerController.SelectPowers(powerPattern);
        if (powers == null || !powers.Any())
        {
            commandInfo.ReplyToCommand("No powers found");
            return;
        }

        foreach (var player in players)
        {
            foreach (var power in powers)
            {
                if (!power.SupportsLeveling) continue;

                if (!power.PlayerProgression.TryGetValue(player.SteamID, out var prog))
                    power.PlayerProgression[player.SteamID] = prog = new PlayerPowerProgression();

                prog.Level = Math.Clamp(level, 0, power.cfg_maxLevel);
                power.SaveProgression(player);

                commandInfo.ReplyToCommand($"Set {StringHelpers.GetPowerNameReadable(power)} level to {prog.Level} for {player.PlayerName}");
            }
        }
    }

    [ConsoleCommand("sp_hud", "Deprecated - use sp_ascii <0-12> for ASCII overlay")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnHudToggle(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        commandInfo.ReplyToCommand("[SP] HUD is deprecated. Use sp_ascii <0-12> for ASCII overlay.");
    }

    public AsciiOverlayManager? AsciiOverlay { get; private set; }

    [ConsoleCommand("sp_ascii", "ASCII overlay test suite. Usage: sp_ascii <test_index>")]
    [CommandHelper(minArgs: 1, usage: "<test_index> (0-12)", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnAsciiTest(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        if (caller == null || !caller.IsValid)
            return;

        if (AsciiOverlay == null)
        {
            commandInfo.ReplyToCommand("[SP-ASCII] AsciiOverlay is not initialized");
            return;
        }

        if (!int.TryParse(commandInfo.GetArg(1), out int testIndex))
        {
            commandInfo.ReplyToCommand("[SP-ASCII] Invalid test index. Usage: sp_ascii <0-12>");
            return;
        }

        AsciiOverlay.Open(caller);
        AsciiOverlay.ClearScreen(caller);

        switch (testIndex)
        {
            case 0:
                RunTest_BasicText(caller);
                break;
            case 1:
                RunTest_DrawingPrimitives(caller);
                break;
            case 2:
                RunTest_ProgressBars(caller);
                break;
            case 3:
                RunTest_Colors(caller);
                break;
            case 4:
                RunTest_BoxWithText(caller);
                break;
            case 5:
                RunTest_FullScreenFill(caller);
                break;
            case 6:
                RunTest_EdgeCases(caller);
                break;
            case 7:
                RunTest_AsciiArt(caller);
                break;
            case 8:
                RunTest_MenuSimulation(caller);
                break;
            case 9:
                RunTest_StatusDisplay(caller);
                break;
            case 10:
                RunTest_TableFormatting(caller);
                break;
            case 11:
                RunTest_AnimationPattern(caller);
                break;
            case 12:
                RunTest_MegaDemo(caller);
                break;
            default:
                commandInfo.ReplyToCommand($"[SP-ASCII] Unknown test index: {testIndex}. Available: 0-12");
                return;
        }

        commandInfo.ReplyToCommand($"[SP-ASCII] Test {testIndex} rendered. Use sp_ascii 99 to close.");
    }

    [ConsoleCommand("sp_ascii_close", "Close the ASCII overlay")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnAsciiClose(CCSPlayerController? caller, CommandInfo commandInfo)
    {
        if (caller == null || !caller.IsValid)
            return;

        AsciiOverlay?.Close(caller);
        commandInfo.ReplyToCommand("[SP-ASCII] Overlay closed");
    }

    private void RunTest_BasicText(CCSPlayerController player)
    {
        var o = AsciiOverlay!;
        o.SetLineColored(player, 0, "TEST 0: BASIC TEXT RENDERING", "gold");
        o.DrawHorizontalLine(player, 0, 1, 160, '-');

        // Simple text at various positions
        o.DrawTextAt(player, 0, 3, "Position (0,3): Left-aligned text");
        o.DrawTextAt(player, 50, 4, "Position (50,4): Middle of screen");
        o.DrawTextAt(player, 100, 5, "Position (100,5): Right side");

        // Centered text
        o.DrawCenteredText(player, 7, "CENTERED TEXT AT ROW 7");
        o.DrawCenteredText(player, 8, "Another centered line");

        // Single characters
        o.DrawCharAt(player, 0, 10, 'A');
        o.DrawCharAt(player, 79, 10, 'M'); // Middle
        o.DrawCharAt(player, 159, 10, 'Z'); // End

        // Vertical lines
        o.DrawVerticalLine(player, 40, 3, 10, '|');
        o.DrawVerticalLine(player, 120, 3, 10, '|');

        // Labels
        o.SetLineColored(player, 12, "Single chars: A at (0,10), M at (79,10), Z at (159,10)", "green");
        o.SetLineColored(player, 13, "Vertical lines drawn at columns 40 and 120", "green");
    }

    private void RunTest_DrawingPrimitives(CCSPlayerController player)
    {
        var o = AsciiOverlay!;
        o.SetLineColored(player, 0, "TEST 1: DRAWING PRIMITIVES", "gold");
        o.DrawHorizontalLine(player, 0, 1, 160, '-');

        // Horizontal lines
        o.DrawHorizontalLine(player, 5, 3, 40, '-');
        o.DrawHorizontalLine(player, 5, 4, 40, '=');
        o.DrawHorizontalLine(player, 5, 5, 40, '#');
        o.SetLineColored(player, 6, "Horizontal lines: '-', '=', '#'", "green");

        // Vertical lines
        o.DrawVerticalLine(player, 5, 8, 8, '|');
        o.DrawVerticalLine(player, 10, 8, 8, '!');
        o.DrawVerticalLine(player, 15, 8, 8, '+');
        o.SetLineColored(player, 17, "Vertical lines: '|', '!', '+'", "green");

        // Box outline
        o.DrawBox(player, 50, 3, 30, 10, '*');
        o.DrawTextAt(player, 52, 5, "BOX INSIDE");
        o.DrawTextAt(player, 52, 7, "Text at (52,7)");
        o.SetLineColored(player, 14, "Box outline at (50,3) 30x10 with '*' border", "green");

        // Filled rectangle
        o.DrawRect(player, 90, 3, 30, 10, '.');
        o.SetLineColored(player, 15, "Filled rectangle at (90,3) 30x10 with '.' fill", "green");

        // Another box style
        o.DrawBox(player, 50, 16, 70, 8, '@');
        o.DrawTextAt(player, 52, 18, "Different border char '@'");
        o.DrawTextAt(player, 52, 20, "This box is 70 wide, 8 tall");
    }

    private void RunTest_ProgressBars(CCSPlayerController player)
    {
        var o = AsciiOverlay!;
        o.SetLineColored(player, 0, "TEST 2: PROGRESS BARS", "gold");
        o.DrawHorizontalLine(player, 0, 1, 160, '-');

        // Various progress levels
        float[] progresses = [0f, 0.1f, 0.25f, 0.33f, 0.5f, 0.66f, 0.75f, 0.9f, 1f];

        for (int i = 0; i < progresses.Length; i++)
        {
            int y = 3 + i;
            o.DrawTextAt(player, 0, y, $"{progresses[i],5:P0}");
            o.DrawProgressBar(player, 8, y, 50, progresses[i]);
        }

        o.SetLineColored(player, 13, "Progress bars from 0% to 100%", "green");

        // Different bar styles
        o.SetLineColored(player, 15, "Custom bar styles:", "green");
        o.DrawProgressBar(player, 5, 16, 40, 0.75f, '=', '-');
        o.DrawTextAt(player, 48, 16, "Default [=------]");

        o.DrawProgressBar(player, 5, 17, 40, 0.75f, '#', ' ');
        o.DrawTextAt(player, 48, 17, "Solid [#######   ]");

        o.DrawProgressBar(player, 5, 18, 40, 0.75f, '*', '.');
        o.DrawTextAt(player, 48, 18, "Dotted [***.......]");

        // Wide bar
        o.DrawProgressBar(player, 5, 20, 80, 0.65f);
        o.SetLineColored(player, 21, "Wide bar (80 chars) at 65%", "green");

        // Narrow bar
        o.DrawProgressBar(player, 5, 22, 20, 0.45f);
        o.SetLineColored(player, 23, "Narrow bar (20 chars) at 45%", "green");
    }

    private void RunTest_Colors(CCSPlayerController player)
    {
        var o = AsciiOverlay!;
        o.SetLineColored(player, 0, "TEST 3: COLOR VARIANTS", "gold");
        o.DrawHorizontalLine(player, 0, 1, 160, '-');

        // Standard colors
        string[] colors = ["green", "red", "blue", "gold", "purple", "white", "gray"];
        for (int i = 0; i < colors.Length; i++)
        {
            o.SetLineColored(player, 3 + i, $"Color: {colors[i].ToUpper()} - The quick brown fox jumps over the lazy dog", colors[i]);
        }

        // Bright variants
        o.SetLineColored(player, 11, "BRIGHT VARIANTS:", "white");
        string[] brightColors = ["green", "red", "blue", "gold"];
        for (int i = 0; i < brightColors.Length; i++)
        {
            o.SetLineColored(player, 12 + i, $"Bright: {brightColors[i].ToUpper()} - ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789", $"bright-{brightColors[i]}");
        }

        // Mixed colors in one line (using DrawCharAt)
        o.SetLineColored(player, 17, "Mixed colors (char by char):", "white");
        o.DrawCharAt(player, 0, 18, 'R');
        o.SetLineColored(player, 18, new string(' ', 160), null); // Reset line first
        o.DrawCharAt(player, 0, 18, 'R');

        // Color swatches
        o.SetLineColored(player, 20, "COLOR SWATCHES (block characters):", "white");
        string blockChars = "\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588"; // 8 full blocks
        for (int i = 0; i < colors.Length; i++)
        {
            int x = 2 + (i * 20);
            o.DrawTextAt(player, x, 21, blockChars);
        }
    }

    private void RunTest_BoxWithText(CCSPlayerController player)
    {
        var o = AsciiOverlay!;
        o.SetLineColored(player, 0, "TEST 4: BOXES WITH TEXT", "gold");
        o.DrawHorizontalLine(player, 0, 1, 160, '-');

        // Simple box
        string[] text1 = ["Hello!", "This is", "inside a box"];
        o.DrawTextBox(player, 5, 3, 25, 7, text1);

        // Box with border char
        string[] text2 = ["Custom", "Border", "Character"];
        o.DrawTextBox(player, 35, 3, 25, 7, text2, '#');

        // Wide box
        string[] text3 = ["This is a wider box with more text inside", "It spans multiple lines and is 60 characters wide"];
        o.DrawTextBox(player, 65, 3, 60, 6, text3, '+');

        // Empty box
        o.DrawBox(player, 5, 12, 30, 8, '-');
        o.SetLineColored(player, 21, "Empty box at (5,12) 30x8", "green");

        // Box with text that overflows
        string[] longText = ["This text is definitely longer than the box width and should be truncated"];
        o.DrawTextBox(player, 40, 12, 30, 5, longText, '*');
        o.SetLineColored(player, 18, "Box with overflow text (truncated)", "green");

        // Nested boxes
        o.DrawBox(player, 5, 24, 50, 12, '@');
        o.DrawBox(player, 8, 26, 44, 8, '#');
        o.DrawTextAt(player, 10, 28, "Nested boxes!");
        o.SetLineColored(player, 37, "Nested boxes (outer '@', inner '#')", "green");
    }

    private void RunTest_FullScreenFill(CCSPlayerController player)
    {
        var o = AsciiOverlay!;
        o.SetLineColored(player, 0, "TEST 5: FULL SCREEN FILL", "gold");

        // Fill entire screen with a pattern
        for (int row = 0; row < 40; row++)
        {
            var sb = new System.Text.StringBuilder(160);
            for (int col = 0; col < 160; col++)
            {
                // Create a checkerboard pattern
                if ((row + col) % 2 == 0)
                    sb.Append('\u2588'); // Full block
                else
                    sb.Append(' ');
            }
            o.SetLine(player, row, sb.ToString());
        }

        // Draw title in the center
        o.DrawCenteredText(player, 19, "FULL SCREEN CHECKERBOARD PATTERN");
        o.DrawCenteredText(player, 20, "160x40 grid coverage test");
    }

    private void RunTest_EdgeCases(CCSPlayerController player)
    {
        var o = AsciiOverlay!;
        o.SetLineColored(player, 0, "TEST 6: EDGE CASES", "gold");
        o.DrawHorizontalLine(player, 0, 1, 160, '-');

        // Empty string
        o.SetLine(player, 3, "");
        o.SetLineColored(player, 4, "Empty string at row 3 (should be blank)", "green");

        // Single character
        o.SetLine(player, 6, "X");
        o.SetLineColored(player, 7, "Single char 'X' at start of row 6", "green");

        // Very long string (should be truncated)
        o.SetLine(player, 9, new string('A', 200));
        o.SetLineColored(player, 10, "200 chars 'A' truncated to 160 at row 9", "green");

        // Out of bounds positions (should be ignored)
        o.DrawCharAt(player, -1, 12, 'X');
        o.DrawCharAt(player, 200, 12, 'X');
        o.DrawTextAt(player, -10, 13, "This should not appear");
        o.DrawTextAt(player, 170, 14, "This should not appear");
        o.SetLineColored(player, 15, "Out-of-bounds draws were ignored", "green");

        // Box at screen edge
        o.DrawBox(player, 150, 12, 20, 8, '+');
        o.SetLineColored(player, 21, "Box at edge (150,12) - should clip", "green");

        // Box completely off-screen
        o.DrawBox(player, 200, 25, 10, 5, '*');
        o.SetLineColored(player, 22, "Box at (200,25) - off-screen, nothing visible", "green");

        // Unicode characters
        o.DrawTextAt(player, 5, 24, "Unicode: \u2588\u2591\u2592\u2593 \u2190\u2191\u2192\u2193 \u2605\u2606");
        o.SetLineColored(player, 25, "Unicode block chars and arrows at row 24", "green");

        // Spaces in text
        o.DrawTextAt(player, 5, 27, "T   e   s   t       w   i   t   h       s   p   a   c   e   s");
        o.SetLineColored(player, 28, "Text with intentional spacing at row 27", "green");
    }

    private void RunTest_AsciiArt(CCSPlayerController player)
    {
        var o = AsciiOverlay!;
        o.SetLineColored(player, 0, "TEST 7: ASCII ART", "gold");
        o.DrawHorizontalLine(player, 0, 1, 160, '-');

        // Cat ASCII art
        string[] cat = [
            "    /\\_/\\  ",
            "   ( o.o ) ",
            "    > ^ <  ",
            "   /|   |\\",
            "  (_|   |_)",
        ];
        o.SetLines(player, 3, cat);
        o.SetLineColored(player, 9, "ASCII Cat at (0,3)", "green");

        // House ASCII art
        string[] house = [
            "       /\\       ",
            "      /  \\      ",
            "     /    \\     ",
            "    /      \\    ",
            "   /________\\   ",
            "   |  ____  |   ",
            "   | |    | |   ",
            "   | |____| |   ",
            "   |________|   ",
        ];
        o.SetLines(player, 3, house);
        o.SetLineColored(player, 13, "ASCII House at (0,3)", "blue");

        // Robot face
        string[] robot = [
            "  _____  ",
            " |     | ",
            " | o o | ",
            " |  ^  | ",
            " | \\_/ | ",
            " |_____| ",
        ];
        o.SetLines(player, 3, robot);
        o.SetLineColored(player, 10, "ASCII Robot at (0,3)", "red");

        // Arrow
        string[] arrow = [
            "      |      ",
            "      |      ",
            "      |      ",
            "  ____|____  ",
            " |         | ",
            " |  FWD -> | ",
            " |_________| ",
        ];
        o.SetLines(player, 15, arrow);
        o.SetLineColored(player, 23, "ASCII Arrow at (0,15)", "gold");
    }

    private void RunTest_MenuSimulation(CCSPlayerController player)
    {
        var o = AsciiOverlay!;
        o.SetLineColored(player, 0, "TEST 8: MENU SIMULATION", "gold");
        o.DrawHorizontalLine(player, 0, 1, 160, '-');

        // Main menu box
        o.DrawBox(player, 30, 3, 100, 30, '+');

        // Title
        o.DrawCenteredText(player, 5, "SUPER POWERS - MAIN MENU");
        o.DrawHorizontalLine(player, 31, 6, 98, '=');

        // Menu items
        string[] menuItems = [
            "[1] Blood Fury      - Kills grant stacking damage/speed bonuses",
            "[2] Radiation       - 1 dmg/s to enemies in sight",
            "[3] Invisibility    - You are nearly invisible",
            "[4] Regeneration    - Regenerate 10 HP/s while below 75",
            "[5] Bitcoin Miner   - Random money ticks",
            "[6] Speedy Fella    - Increased walking speed",
            "[7] Golden Bullet   - Kill reward when using last bullet",
            "[8] Evil Aura       - Slowly harm nearby enemies",
        ];

        for (int i = 0; i < menuItems.Length; i++)
        {
            o.DrawTextAt(player, 33, 8 + i, menuItems[i]);
        }

        // Highlight selected item
        o.SetLineColored(player, 10, menuItems[2], "bright-green");

        // Footer
        o.DrawHorizontalLine(player, 31, 17, 98, '=');
        o.DrawTextAt(player, 33, 19, "Press 1-8 to select");
        o.DrawTextAt(player, 33, 20, "Press ESC to cancel");

        // Status bar
        o.DrawBox(player, 30, 25, 100, 5, '#');
        o.DrawTextAt(player, 32, 27, "Health: 100  Armor: 100  Money: $16000");
        o.DrawTextAt(player, 32, 28, "Current Power: None");
    }

    private void RunTest_StatusDisplay(CCSPlayerController player)
    {
        var o = AsciiOverlay!;
        o.SetLineColored(player, 0, "TEST 9: STATUS DISPLAY", "gold");
        o.DrawHorizontalLine(player, 0, 1, 160, '-');

        // Player status panel
        o.DrawBox(player, 2, 3, 50, 15, '|');
        o.DrawTextAt(player, 4, 4, "PLAYER STATUS");
        o.DrawHorizontalLine(player, 3, 5, 48, '-');

        o.DrawTextAt(player, 4, 7, "Name: Tem");
        o.DrawTextAt(player, 4, 8, "Health:");
        o.DrawProgressBar(player, 12, 8, 30, 0.75f);
        o.DrawTextAt(player, 44, 8, "75/100");

        o.DrawTextAt(player, 4, 9, "Armor:");
        o.DrawProgressBar(player, 12, 9, 30, 0.5f);
        o.DrawTextAt(player, 44, 9, "50/100");

        o.DrawTextAt(player, 4, 10, "Money: $12,500");
        o.DrawTextAt(player, 4, 11, "Kills: 5  Deaths: 3");
        o.DrawTextAt(player, 4, 12, "K/D: 1.67");

        // Power status panel
        o.DrawBox(player, 55, 3, 50, 15, '|');
        o.DrawTextAt(player, 57, 4, "POWER STATUS");
        o.DrawHorizontalLine(player, 56, 5, 48, '-');

        o.DrawTextAt(player, 57, 7, "Active Power: Radiation");
        o.DrawTextAt(player, 57, 8, "Level: 3");
        o.DrawTextAt(player, 57, 9, "XP Progress:");
        o.DrawProgressBar(player, 57, 10, 30, 0.45f);
        o.DrawTextAt(player, 89, 10, "45%");

        o.DrawTextAt(player, 57, 12, "Damage Dealt: 234");
        o.DrawTextAt(player, 57, 13, "Damage Taken: 89");

        // Round info panel
        o.DrawBox(player, 108, 3, 50, 15, '|');
        o.DrawTextAt(player, 110, 4, "ROUND INFO");
        o.DrawHorizontalLine(player, 109, 5, 48, '-');

        o.DrawTextAt(player, 110, 7, "Round: 12/30");
        o.DrawTextAt(player, 110, 8, "Score: CT 8 - T 4");
        o.DrawTextAt(player, 110, 9, "Time Left: 1:23");
        o.DrawTextAt(player, 110, 11, "Bomb: Not planted");
        o.DrawTextAt(player, 110, 12, "Players: 10/10");

        // Bottom bar
        o.DrawBox(player, 2, 20, 156, 4, '#');
        o.DrawCenteredText(player, 21, "SUPER POWERS v0.4.0 | Server: My Server | Map: de_dust2");
        o.DrawCenteredText(player, 22, "Type !help for commands | Type !powers for your powers");
    }

    private void RunTest_TableFormatting(CCSPlayerController player)
    {
        var o = AsciiOverlay!;
        o.SetLineColored(player, 0, "TEST 10: TABLE FORMATTING", "gold");
        o.DrawHorizontalLine(player, 0, 1, 160, '-');

        // Table header
        o.DrawTextAt(player, 2, 3, "+----+------------------+--------+--------+--------+");
        o.DrawTextAt(player, 2, 4, "| #  | Power            | Price  | Rarity | Status |");
        o.DrawTextAt(player, 2, 5, "+----+------------------+--------+--------+--------+");

        // Table rows
        string[][] rows = [
            ["1", "Blood Fury", "$7000", "Rare", "ACTIVE"],
            ["2", "Radiation", "$7000", "Rare", "ACTIVE"],
            ["3", "Invisibility", "$8000", "Legend", "OFF"],
            ["4", "Regeneration", "$5000", "Uncom", "ACTIVE"],
            ["5", "Bitcoin Miner", "$5000", "Uncom", "OFF"],
            ["6", "Speedy Fella", "$3000", "Common", "ACTIVE"],
            ["7", "Golden Bullet", "$4000", "Uncom", "OFF"],
            ["8", "Evil Aura", "$9500", "Rare", "OFF"],
        ];

        for (int i = 0; i < rows.Length; i++)
        {
            string row = $"| {rows[i][0],2} | {rows[i][1],-16} | {rows[i][2],-6} | {rows[i][3],-6} | {rows[i][4],-6} |";
            o.DrawTextAt(player, 2, 6 + i, row);
        }

        o.DrawTextAt(player, 2, 14, "+----+------------------+--------+--------+--------+");

        // Summary
        o.SetLineColored(player, 16, "Total powers: 8 | Active: 3 | Total cost: $45,000", "green");
    }

    private void RunTest_AnimationPattern(CCSPlayerController player)
    {
        var o = AsciiOverlay!;
        o.SetLineColored(player, 0, "TEST 11: ANIMATION PATTERN", "gold");
        o.DrawHorizontalLine(player, 0, 1, 160, '-');

        // Create a wave pattern
        for (int row = 3; row < 20; row++)
        {
            var sb = new System.Text.StringBuilder(160);
            for (int col = 0; col < 160; col++)
            {
                double wave = Math.Sin((col + row * 5) * 0.1);
                if (wave > 0.5)
                    sb.Append('\u2588'); // Full block
                else if (wave > 0)
                    sb.Append('\u2592'); // Medium shade
                else if (wave > -0.5)
                    sb.Append('\u2591'); // Light shade
                else
                    sb.Append(' ');
            }
            o.SetLine(player, row, sb.ToString());
        }

        // Create a diagonal pattern
        for (int row = 22; row < 35; row++)
        {
            var sb = new System.Text.StringBuilder(160);
            for (int col = 0; col < 160; col++)
            {
                int diag = (col + row) % 16;
                if (diag == 0)
                    sb.Append('#');
                else if (diag < 4)
                    sb.Append('=');
                else if (diag < 8)
                    sb.Append('-');
                else if (diag < 12)
                    sb.Append('.');
                else
                    sb.Append(' ');
            }
            o.SetLine(player, row, sb.ToString());
        }

        o.SetLineColored(player, 37, "Wave pattern (top) and diagonal gradient (bottom)", "green");
        o.SetLineColored(player, 38, "This demonstrates unicode block character density variations", "green");
    }

    private void RunTest_MegaDemo(CCSPlayerController player)
    {
        var o = AsciiOverlay!;
        o.ClearScreen(player);

        // Title bar
        o.DrawBox(player, 0, 0, 160, 3, '=');
        o.SetLineColored(player, 1, "ASCII OVERLAY MEGA DEMO - 160x40 GRID - ALL FEATURES COMBINED", "bright-gold");

        // Left panel - ASCII Art
        o.DrawBox(player, 1, 4, 40, 20, '+');
        o.SetLineColored(player, 5, " ASCII ART GALLERY", "bright-blue");
        o.DrawHorizontalLine(player, 2, 6, 38, '-');

        string[] cat = [
            "    /\\_/\\  ",
            "   ( o.o ) ",
            "    > ^ <  ",
            "   /|   |\\",
            "  (_|   |_)",
        ];
        o.SetLines(player, 7, cat);

        string[] heart = [
            "  ****  ****  ",
            "  **********  ",
            "   ********   ",
            "    ******    ",
            "     ****     ",
            "      **      ",
        ];
        o.SetLines(player, 13, heart);

        // Center panel - Status
        o.DrawBox(player, 42, 4, 76, 20, '|');
        o.SetLineColored(player, 5, " SYSTEM STATUS", "bright-green");
        o.DrawHorizontalLine(player, 43, 6, 74, '-');

        o.DrawTextAt(player, 44, 8, "CPU Usage:");
        o.DrawProgressBar(player, 56, 8, 40, 0.65f);
        o.DrawTextAt(player, 98, 8, "65%");

        o.DrawTextAt(player, 44, 9, "Memory:");
        o.DrawProgressBar(player, 56, 9, 40, 0.42f);
        o.DrawTextAt(player, 98, 9, "42%");

        o.DrawTextAt(player, 44, 10, "Network:");
        o.DrawProgressBar(player, 56, 10, 40, 0.78f);
        o.DrawTextAt(player, 98, 10, "78%");

        o.DrawTextAt(player, 44, 12, "Players Online: 24/32");
        o.DrawTextAt(player, 44, 13, "Uptime: 3d 14h 22m");
        o.DrawTextAt(player, 44, 14, "Map: de_dust2");
        o.DrawTextAt(player, 44, 15, "Mode: Super Powers (Random)");

        // Progress bars for various stats
        o.DrawTextAt(player, 44, 17, "Round Progress:");
        o.DrawProgressBar(player, 44, 18, 60, 0.75f);
        o.DrawTextAt(player, 106, 18, "75%");

        o.DrawTextAt(player, 44, 20, "Bomb Timer:");
        o.DrawProgressBar(player, 44, 21, 60, 0.33f, '!', ' ');
        o.DrawTextAt(player, 106, 21, "33%");

        // Right panel - Menu
        o.DrawBox(player, 119, 4, 40, 20, '#');
        o.SetLineColored(player, 5, " QUICK MENU", "bright-red");
        o.DrawHorizontalLine(player, 120, 6, 38, '-');

        o.DrawTextAt(player, 121, 8, "[1] Buy Equipment");
        o.DrawTextAt(player, 121, 9, "[2] Team Chat");
        o.DrawTextAt(player, 121, 10, "[3] Scoreboard");
        o.DrawTextAt(player, 121, 11, "[4] Settings");

        o.SetLineColored(player, 13, ">", "bright-green");
        o.DrawTextAt(player, 122, 13, "Select option:");

        // Bottom status bar
        o.DrawBox(player, 0, 25, 160, 14, '*');
        o.SetLineColored(player, 26, " BOTTOM STATUS BAR", "bright-purple");
        o.DrawHorizontalLine(player, 1, 27, 158, '-');

        // Stats grid
        o.DrawTextAt(player, 2, 29, "Health:");
        o.DrawProgressBar(player, 10, 29, 25, 0.85f);
        o.DrawTextAt(player, 37, 29, "85%");

        o.DrawTextAt(player, 2, 30, "Armor:");
        o.DrawProgressBar(player, 10, 30, 25, 0.60f);
        o.DrawTextAt(player, 37, 30, "60%");

        o.DrawTextAt(player, 50, 29, "Ammo: 30/90");
        o.DrawTextAt(player, 50, 30, "Money: $8,500");

        o.DrawTextAt(player, 80, 29, "Kills: 12");
        o.DrawTextAt(player, 80, 30, "Deaths: 5");

        // Color swatches
        o.DrawTextAt(player, 100, 29, "Colors:");
        string[] colors = ["green", "red", "blue", "gold", "purple"];
        for (int i = 0; i < colors.Length; i++)
        {
            o.DrawCharAt(player, 108 + (i * 4), 29, '\u2588');
        }

        // Footer text
        o.DrawCenteredText(player, 32, "SUPER POWERS PLUGIN v0.4.0 | ASCII OVERLAY DEMO");
        o.DrawCenteredText(player, 33, "Use sp_ascii <0-12> for individual tests | sp_ascii_close to close");

        // Decorative border
        o.DrawHorizontalLine(player, 0, 35, 160, '=');
        o.SetLineColored(player, 36, "This demonstrates the full capabilities of the 160x40 ASCII overlay system.", "white");
        o.SetLineColored(player, 37, "Features: Text, Boxes, Progress Bars, Colors, Unicode, ASCII Art, Tables", "white");
        o.SetLineColored(player, 38, "All rendering is driven by the server via dialog variables - no client scripts.", "white");
        o.SetLineColored(player, 39, "Semi-transparent background allows game scene to be visible behind the overlay.", "white");
    }

    public void OnConfigParsed(SuperPowerConfig config)
    {
        Config = config;

        SuperPowerController.FeedTheConfig(Config);

        // Persist merged config back to disk so stale entries are cleaned up
        var configDir = Path.Combine(ModuleDirectory, "..", "..", "configs", "plugins", ModuleName);
        var configPath = Path.GetFullPath(Path.Combine(configDir, $"{ModuleName}.json"));
        try
        {
            Directory.CreateDirectory(configDir);
            var json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[super_powers_plugin] Failed to save merged config: {ex.Message}");
        }
    }

    public FakeConVar<bool> silent = new("sp_silent", "", false);
}