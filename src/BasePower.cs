using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using super_powers_plugin.src;

// TODO:
// - add dependency list thing for powers
// - allow multiple powers to affect the same variable (add by a value or mult by a value)

// abscaraftlks

// ! Private fields will be exposed to user in an automaticaly generated config, leave fields public if you dont want them to be configurable

public class BasePower : ShopPower
{
    public string Name => NiceText.GetPowerName(this);
    public List<Type> Triggers = [];                                    // ! Put types of events that trigger this power logic here
    public List<CCSPlayerController> Users = [];                        // ! Active users live here
    public List<Tuple<CCSPlayerController, int>> UsersDisabled = [];    // Temporary disabled users live here, useful when powers disable other powers
    public List<ulong> UsersSteamIDs = [];                              // Active but offline users, useful when someone disconnects mid-game
    public List<string> NeededResources = [];                           // ! Put custom assets or models that need to be precached

    public CsTeam teamReq = CsTeam.None;                                // ! If not none, only specified team will be able to use this power
    public int priority = 0;                                            // internal use, list of powers will be sorted by priority, so 
                                                                        // additive powers activate before multiplicative

    public List<Type> Incompatibilities = [];                           // ! Holds types of powers that are criticaly incompatible with this one

    // Check transmit listener will ask every power for all hidden entities
    // return null if player is not affected by any check transmit thingies
    public virtual List<CBaseModelEntity>? GetHiddenEntities(CCSPlayerController player) { return null; }
    public bool checkTransmitListenerEnabled = false;    // but only if enabled

    private bool enabled = true;                                        // Disabled powers wont show up anywhere

    public void SetDisabled() { enabled = false; }
    public bool IsDisabled() => !enabled;

    public virtual string GetDescription() => "";                       // ! Set custom description here
    public virtual string GetDescriptionColored() => "";                // ! Colored version for the shopper

    public virtual HookResult Execute(GameEvent gameEvent)
    {
        return HookResult.Continue;                                     // ! Put custom logic here
    }
    public virtual void Update() { }                                    // ! Update will be executed every tick, do not put heavy operations in here if possible
    public virtual void ParseCfg(Dictionary<string, string> cfg) { TemUtils.ParseConfigReflectiveRecursive(this, this.GetType(), cfg); } // no touching
    public virtual bool IsUser(CCSPlayerController player) { return Users.Contains(player); }

    public virtual void OnRemovePower(CCSPlayerController? player) { }  // ! Put custom logic that is needed to rever player to its original state

    // public virtual Tuple<SIGNAL_STATUS, string> OnSignal(CCSPlayerController? player, List<string> args) { return Tuple.Create(SIGNAL_STATUS.IGNORED, ""); }
    public virtual Tuple<SIGNAL_STATUS, string> OnSignal(CCSPlayerController? player, List<string> args) { return SuperPowerController.ignored_signal; }
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
