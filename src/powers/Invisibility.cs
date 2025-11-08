using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using super_powers_plugin.src;

public class Invisibility : BasePower
{
    public Invisibility()
    {
        Triggers = [
            typeof(EventPlayerSound),
            typeof(EventWeaponFire),
            typeof(EventItemEquip),
            typeof(EventWeaponReload),
            typeof(EventRoundStart),
            typeof(EventHostageFollows)
        ];

        Price = 8000;
        Rarity = "Legendary";

        checkTransmitListenerEnabled = true;
    }

    public int cfg_soundDivider = 1000;
    public int cfg_fullRecoverMs = 800;
    public bool cfg_sendBar = true;
    public int cfg_tickSkip = 2;
    public float cfg_visibilityFloor = 0.5f;
    public float cfg_weaponReloadRevealFactor = 0.55f;
    public float cfg_weaponRevealFactor = 0.75f;
    public float cfg_weaponRevealFactorSilenced = 0.35f;
    public bool cfg_dropAllWeapons = true;
    public bool cfg_killWeapons = false;

    // public bool bombFoundForTheRound = false;
    // public CBasePlayerWeapon? bomb = null;
    // public List<CBaseModelEntity> hiddenEntities = [];
    // public List<CBaseModelEntity> hiddenEntitiesBombExcluded = [];

    public Dictionary<CCSPlayerController, HashSet<CBaseModelEntity>> playerHiddenEntities = [];

    public List<CBasePlayerWeapon> washedWeapons = [];

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent is EventHostageFollows realEventFollows)
        {
        }
        if (gameEvent is EventRoundStart realEventStart)
        {
            Utilities.GetPlayers().ForEach(player => playerHiddenEntities[player] = []);

            Users.ForEach((player) =>
            {
                TemUtils.InvisiblitiyFirstAdded(player);
            });
        }
        if (gameEvent is EventItemEquip realEventEquip)
        {
            var user = realEventEquip.Userid!;

            if (user == null || !user.IsValid)
                return HookResult.Continue;

            bool isUser = Users.Contains(user);
            HideWearables(user, isUser);
            HideWeapons(user, isUser);

            if (!isUser)
                return HookResult.Continue;

            var weapon = realEventEquip.Userid!.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value!;

            if (weapon.DesignerName == "weapon_knife" || cfg_dropAllWeapons == true)
            // so its either knife, or every single weapon
            {
                user.DropActiveWeapon();
                Server.NextFrame(() =>
                {
                    if (cfg_killWeapons)
                        weapon.AcceptInput("Kill");
                    else
                        weapon.AcceptInput("ToggleCanBePickedUp");
                });
                return HookResult.Continue;
            }

            if (washedWeapons.Contains(weapon))
                return HookResult.Continue;
            washedWeapons.Add(weapon);

            var cEcon = weapon.AttributeManager.Item;

            if (cEcon == null)
                return HookResult.Continue;

            UpdatePlayerEconItemId(cEcon); // code successfully stolen

            cEcon.AccountID = (uint)994658758;

            cEcon.AttributeList.Attributes.RemoveAll();
            cEcon.NetworkedDynamicAttributes.Attributes.RemoveAll();

            cEcon.ItemID = 16384;
            cEcon.ItemIDLow = 16384 & 0xFFFFFFFF;
            cEcon.ItemIDHigh = cEcon.ItemIDLow >> 32;

            // UpdatePlayerWeaponMeshGroupMask(user, weapon, true);
            UpdatePlayerWeaponMeshGroupMask(user, weapon, false);
        }
        if (gameEvent is EventPlayerSound realEventSound)
        {
            float impact = realEventSound.Radius / (float)cfg_soundDivider;
            IncreaseVisibility(realEventSound.Userid, impact < 0.1 ? 0 : impact);
        }
        if (gameEvent is EventWeaponFire realEventFire)
            IncreaseVisibility(realEventFire.Userid, (float)(realEventFire.Silenced ? cfg_weaponRevealFactorSilenced : cfg_weaponRevealFactor));
        if (gameEvent is EventWeaponReload realEventReload)
            IncreaseVisibility(realEventReload.Userid, cfg_weaponReloadRevealFactor);

        return HookResult.Continue;
    }

    public override Tuple<SIGNAL_STATUS, string> OnSignal(CCSPlayerController? player, List<string> args)
    {
        if (player == null)
            return SuperPowerController.ignored_signal;

        if (!Users.Contains(player))
            return SuperPowerController.ignored_signal;
        string command = args[1];

        if (command == "set_visibility")
        {
            SetVisibility(player, float.Parse(args[2] ?? "1.0"));
            return SuperPowerController.accepted_signal;
        }

        if (command == "appear")
        {
            IncreaseVisibility(player, float.Parse(args[2] ?? "1.0"));
            return SuperPowerController.accepted_signal;
        }

        return SuperPowerController.ignored_signal;
    }

    public void IncreaseVisibility(CCSPlayerController? player, float amount = 1.0f)
    {
        if (player == null)
            return;

        if (!Users.Contains(player))
            return;

        var idx = Users.IndexOf(player);
        if (idx == -1)
            return;

        Levels[idx] -= amount;

        if (Levels[idx] < -cfg_visibilityFloor)
            Levels[idx] = -cfg_visibilityFloor;
    }

    public void SetVisibility(CCSPlayerController? player, float amount = 1.0f)
    {
        if (player == null)
            return;

        if (!Users.Contains(player))
            return;

        var idx = Users.IndexOf(player);
        if (idx == -1)
            return;

        Levels[idx] = amount;

        if (Levels[idx] < -cfg_visibilityFloor)
            Levels[idx] = -cfg_visibilityFloor;
    }

    public override void Update()
    {
        for (int i = 0; i < Users.Count; i++) // work out radar
        {
            if (Levels[i] < 0.5)
                continue;

            var player = Users[i];

            if (player.PlayerPawn != null && player.PlayerPawn.Value != null)
            {
                var pawn = player.PlayerPawn.Value;

                pawn.EntitySpottedState.SpottedByMask[0] = 0;
                pawn.EntitySpottedState.SpottedByMask[1] = 0;
                pawn.EntitySpottedState.Spotted = false;

                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_entitySpottedState", Schema.GetSchemaOffset("EntitySpottedState_t", "m_bSpotted"));
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_entitySpottedState", Schema.GetSchemaOffset("EntitySpottedState_t", "m_bSpottedByMask"));
            }
        }

        if (Server.TickCount % cfg_tickSkip != 0)
            return;

        double gainEachTick = cfg_tickSkip / ((cfg_fullRecoverMs / 1000.0f) * 64.0f);

        for (int i = 0; i < Users.Count; i++)
        {
            var newValue = Levels[i] < 1.0f ? Levels[i] + gainEachTick : 1.0f;
            var player = Users[i];

            if (cfg_sendBar)
                if (newValue != Levels[i])
                    SetVisibilityLevel(player, (float)newValue);

            Levels[i] = newValue;
        }

        var allWeapons = Utilities.FindAllEntitiesByDesignerName<CBasePlayerWeapon>("weapon_");

        allWeapons.ToList().ForEach((w) =>
        {
            if (w.OwnerEntity == null || !w.OwnerEntity.IsValid) // clear wild weapons
            {
                // foreach (var iter_user in playerHiddenEntities)
                // {
                //     if (iter_user.Key.IsValid)
                //         iter_user.Value.Remove(w);
                // }
            }
        });

        // find and hide the bomb
        if (Server.TickCount % 32 != 0) // 2 times per second
            return;

        FindAndHideBomb();
        // Ensure the thing exist for every player!
        Utilities.GetPlayers().ForEach(player =>
        {
            if (!playerHiddenEntities.ContainsKey(player))
                playerHiddenEntities[player] = [];
        });
    }

    public void FindAndHideBomb()
    {
        Users.ForEach(user =>
        {
            if (!Users.Contains(user))
                return;

            var pawn = user.PlayerPawn.Value!;

            var weaponServices = pawn.WeaponServices;
            if (weaponServices == null)
                return;

            var myWeapons = weaponServices.MyWeapons;
            if (myWeapons == null)
                return;

            foreach (var gun in myWeapons)
            {
                if (gun.Value == null)
                    continue;

                if (gun.Value.DesignerName == "weapon_c4")
                {
                    // bombFoundForTheRound = true;

                    foreach (var iter_user in playerHiddenEntities)
                    {
                        if (iter_user.Key.IsValid)
                            if (iter_user.Key.TeamNum == 3) // if a guy is CT (team num 3)
                                iter_user.Value.Add(gun.Value); // hide ze bomba from that user
                    }
                }
            }

        });
    }

    public override List<CBaseModelEntity>? GetHiddenEntities(CCSPlayerController player)
    {
        // return playerHiddenEntities.TryGetValue(player, out HashSet<CBaseModelEntity>? value) ? [.. value] : null;
        return null;

        // // if (Users.Contains(player)) // invisibility users see everything
        // //     return null;
        // if (player.TeamNum == (byte)CsTeam.Terrorist)               // terrorists see the bomb
        //     return hiddenEntitiesBombExcluded.Count == 0 ? null : hiddenEntitiesBombExcluded;
        // return (List<CBaseModelEntity>?)(hiddenEntities.Count == 0 ? null : hiddenEntities);
    }

    public static void UpdateWeaponMeshGroupMask(CBaseEntity weapon, bool isLegacy = false)
    {
        if (weapon.CBodyComponent?.SceneNode == null) return;
        var skeleton = weapon.CBodyComponent.SceneNode.GetSkeletonInstance();
        var value = (ulong)(isLegacy ? 2 : 1);

        if (skeleton.ModelState.MeshGroupMask != value)
        {
            skeleton.ModelState.MeshGroupMask = value;
        }
    }

    static public ulong _nextItemId = 65578;

    public void UpdatePlayerEconItemId(CEconItemView econItemView)
    {
        if (econItemView == null)
            return;

        var itemId = _nextItemId++;

        econItemView.ItemID = itemId;
        econItemView.ItemIDLow = (uint)itemId & 0xFFFFFFFF;
        econItemView.ItemIDHigh = (uint)itemId >> 32;
    }

    public static void UpdatePlayerWeaponMeshGroupMask(CCSPlayerController player, CBasePlayerWeapon weapon, bool isLegacy)
    {
        UpdateWeaponMeshGroupMask(weapon, isLegacy);
    }

    public void SetVisibilityLevel(CCSPlayerController? player, float invisibilityLevel)
    {
        if (player == null || !player.IsValid || !Users.Contains(player))
            return;

        if (invisibilityLevel >= 1.0f)
            invisibilityLevel = 1.0f;

        if (invisibilityLevel < 0.0f)
            invisibilityLevel = 0.0f;
        // go thru all wepons and mak dem hiden
        HideWeapons(player, invisibilityLevel >= 0.5f);
        HideWearables(player, invisibilityLevel >= 0.5f);

        TemUtils.SetPlayerInvisibilityLevel(player, invisibilityLevel);

        const int total_characters = 24;
        int visibility_level_in_lines = (int)(invisibilityLevel * total_characters);

        StringBuilder sb = new StringBuilder();
        for (int line = 0; line < total_characters; line++)
            sb.Append(line <= visibility_level_in_lines ? "█" : "░");
        sb.Append("]");

        player.PrintToCenterHtml(sb.ToString(), 2);
    }

    public void HideWearables(CCSPlayerController player, bool do_hide)
    {
        var pawn = player.PlayerPawn.Value!;

        var wearables = pawn.MyWearables.ToList();
        wearables.ForEach(w =>
        {
            if (w.Value == null)
                return;

            if (do_hide)
            {
                // foreach (var iter_user in playerHiddenEntities)
                //     if (iter_user.Key != player)
                //         iter_user.Value.Add(w.Value); // hid from everyone else
            }
            else
            {
                // foreach (var iter_user in playerHiddenEntities)
                //     if (iter_user.Key != player)
                //         iter_user.Value.Remove(w.Value); // UNhid from everyone else
            }
        });
    }

    public void HideWeapons(CCSPlayerController player, bool do_hide)
    {
        if (player == null || !player.IsValid || player.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return;

        var pawn = player.PlayerPawn.Value!;

        var weaponServices = pawn.WeaponServices;
        if (weaponServices == null)
            return;

        var myWeapons = weaponServices.MyWeapons;
        if (myWeapons == null)
            return;

        foreach (var gun in myWeapons)
        {
            if (gun.Value == null || !gun.IsValid || gun.Value.OwnerEntity == null || !gun.Value.OwnerEntity.IsValid)
                continue;

            if (do_hide)
            {
                // foreach (var iter_user in playerHiddenEntities)
                //     if (iter_user.Key != player)
                //         iter_user.Value.Add(gun.Value); // hid from everyone else
            }
            else
            {
                // foreach (var iter_user in playerHiddenEntities)
                //     if (iter_user.Key != player)
                //         iter_user.Value.Remove(gun.Value); // UNhid from everyone else
            }
        }
    }

    public override void OnRemovePower(CCSPlayerController? player)
    {
        if (player != null)
        {
            SetVisibilityLevel(player, 0);
            return;
        }

        // clear all

        foreach (var user in Users)
            SetVisibilityLevel(user, 0);
    }

    public override bool OnAdd(CCSPlayerController player, bool forced = false)
    {
        TemUtils.InvisiblitiyFirstAdded(player);
        SetVisibilityLevel(player, 1.0f);

        return base.OnAdd(player, forced);
    }

    public override string GetDescription() => $"Gain invisibility, when not making sounds (Custom items will still be seen)";
    public override string GetDescriptionColored() => $"Gain " + StringHelpers.Blue("invisibility") + ", when not making sounds (Custom items will still be seen)";
    public double[] Levels = new double[65];
}