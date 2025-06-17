using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using super_powers_plugin.src;

// TODO:
// - add dependency list thing for powers
// - allow multiple powers to affect the same variable (add by a value or mult by a value)

// abscaraftlks

public class BasePower
{
    public List<Type> Triggers = [];                                    // ! Put types of events that trigger this power logic here
    public List<CCSPlayerController> Users = [];                        // ! Active users live here
    public List<Tuple<CCSPlayerController, int>> UsersDisabled = [];    // Temporary disabled users live here, useful when powers disable other powers
    public List<ulong> UsersSteamIDs = [];                              // Active but offline users, useful when someone disconnects mid-game
    public List<string> NeededResources = [];                           // ! Put custom assets or models that need to be precached

    public CsTeam teamReq = CsTeam.None;                                // ! If not none, only specified team will be able to use this power

    public List<Type> Incompatibilities = [];                           // Unimplemented
    private bool enabled = true;                                        // Disabled powers wont show up anywhere

    public void SetDisabled() { enabled = false; }
    public bool IsDisabled() => !enabled;

    public virtual string GetDescription() => "";                       // ! Set custom description here

    public virtual HookResult Execute(GameEvent gameEvent)
    {
        return HookResult.Continue;                                     // ! Put custom logic here
    }
    public virtual void Update() { }                                    // ! Update will be executed every tick, do not put heavy operations in here if possible
    public virtual void ParseCfg(Dictionary<string, string> cfg) { TemUtils.ParseConfigReflectiveRecursive(this, this.GetType(), cfg); }
    public virtual bool IsUser(CCSPlayerController player) { return Users.Contains(player); }

    public virtual void OnRemovePower(CCSPlayerController? player) { }  // ! Put custom logic that is needed to rever player to its original state

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
        if (teamReq != CsTeam.None && player.TeamNum != (byte)teamReq && forced == false)
            return false;

        if (!SuperPowerController.IsPowerCompatible(player, this))
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

    public virtual void RegisterHooks() { }     // Custom hooks go here, but i dont use them much
    public virtual void UnRegisterHooks() { }   //
}
