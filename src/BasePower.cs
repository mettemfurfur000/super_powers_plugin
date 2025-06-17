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
    public List<Type> Triggers = [];
    public List<CCSPlayerController> Users = [];
    public List<Tuple<CCSPlayerController, int>> UsersDisabled = [];
    public List<ulong> UsersSteamIDs = [];
    public List<string> NeededResources = [];

    public CsTeam teamReq = CsTeam.None;

    public List<Type> Incompatibilities = [];
    private bool disabled = false;

    public void SetDisabled() { disabled = true; }
    public bool IsDisabled() => disabled;

    public virtual string GetDescription() => "";

    public virtual HookResult Execute(GameEvent gameEvent) { return HookResult.Continue; }
    public virtual void Update() { }
    public virtual void ParseCfg(Dictionary<string, string> cfg) { TemUtils.ParseConfigReflectiveRecursive(this, this.GetType(), cfg); }
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

    public virtual void RegisterHooks() { }
    public virtual void UnRegisterHooks() { }
}
