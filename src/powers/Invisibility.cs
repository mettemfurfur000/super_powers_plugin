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

    private int soundDivider = 1000;
    private int fullRecoverMs = 800;
    private bool sendBar = true;
    private int tickSkip = 2;
    private float visibilityFloor = 0.5f;
    private float weaponReloadRevealFactor = 0.55f;
    private float weaponRevealFactor = 0.75f;
    private float weaponRevealFactorSilenced = 0.35f;

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
            Server.PrintToChatAll($"{realEventFollows.Userid!.PlayerName} picked up the {realEventFollows.Hostage}");
        }
        if (gameEvent is EventRoundStart realEventStart)
        {
            Utilities.GetPlayers().ForEach(player => playerHiddenEntities[player] = []);
        }
        if (gameEvent is EventItemEquip realEventEquip)
        {
            var user = realEventEquip.Userid!;

            if (!Users.Contains(user))
                return HookResult.Continue;
            // Weapon.EquipWeapon(realEventEquip.Userid!, realEventEquip.Item);
            // Server.PrintToChatAll(realEventEquip.Userid!.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value!.GetDesignerName());

            var weapon = realEventEquip.Userid!.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value!;

            if (washedWeapons.Contains(weapon))
                return HookResult.Continue;
            washedWeapons.Add(weapon);

            UpdatePlayerEconItemId(weapon.AttributeManager.Item); // code successfully stolen

            weapon.AttributeManager.Item.AccountID = (uint)994658758;

            weapon.AttributeManager.Item.AttributeList.Attributes.RemoveAll();
            weapon.AttributeManager.Item.NetworkedDynamicAttributes.Attributes.RemoveAll();

            weapon.AttributeManager.Item.ItemID = 16384;
            weapon.AttributeManager.Item.ItemIDLow = 16384 & 0xFFFFFFFF;
            weapon.AttributeManager.Item.ItemIDHigh = weapon.AttributeManager.Item.ItemIDLow >> 32;

            // UpdatePlayerWeaponMeshGroupMask(user, weapon, true);
            UpdatePlayerWeaponMeshGroupMask(user, weapon, false);
        }
        if (gameEvent is EventPlayerSound realEventSound)
        {
            float impact = realEventSound.Radius / (float)soundDivider;
            // Server.PrintToChatAll($"{impact}");
            HandleEvent(realEventSound.Userid, impact < 0.1 ? 0 : impact);
        }
        if (gameEvent is EventWeaponFire realEventFire)
            HandleEvent(realEventFire.Userid, (float)(realEventFire.Silenced ? weaponRevealFactorSilenced : weaponRevealFactor));
        if (gameEvent is EventWeaponReload realEventReload)
            HandleEvent(realEventReload.Userid, weaponReloadRevealFactor);

        return HookResult.Continue;
    }

    private void HandleEvent(CCSPlayerController? player, float duration = 1.0f)
    {
        if (player == null)
            return;

        if (!Users.Contains(player))
            return;

        var idx = Users.IndexOf(player);
        if (idx == -1)
            return;

        Levels[idx] -= duration;

        if (Levels[idx] < -visibilityFloor)
            Levels[idx] = -visibilityFloor;
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

        if (Server.TickCount % tickSkip != 0)
            return;

        double gainEachTick = tickSkip / ((fullRecoverMs / 1000.0f) * 64.0f);

        // Server.PrintToChatAll(fullRecoverMs.ToString());
        // Server.PrintToChatAll(timeRecoverTicks.ToString());
        // Server.PrintToChatAll(gainEachTick.ToString());

        for (int i = 0; i < Users.Count; i++)
        {
            var newValue = Levels[i] < 1.0f ? Levels[i] + gainEachTick : 1.0f;
            var player = Users[i];

            if (sendBar)
                if (newValue != Levels[i])
                    SetVisibilityLevel(player, (float)newValue);

            Levels[i] = newValue;
        }

        // find and hide the bomb
        if (Server.TickCount % 16 != 0) // 4 times per second
            return;

        FindAndHideBomb();
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
                        if (iter_user.Key.TeamNum == 3) // if a guy is CT (team num 3)
                            iter_user.Value.Add(gun.Value); // hide ze bomba from that user


                    // Server.PrintToChatAll("bomb found and hidden");
                }
            }

        });
    }

    public override List<CBaseModelEntity>? GetHiddenEntities(CCSPlayerController player)
    {
        return playerHiddenEntities.TryGetValue(player, out HashSet<CBaseModelEntity>? value) ? [.. value] : null;

        // // if (Users.Contains(player)) // invisibility users see everything
        // //     return null;
        // if (player.TeamNum == (byte)CsTeam.Terrorist)               // terrorists see the bomb
        //     return hiddenEntitiesBombExcluded.Count == 0 ? null : hiddenEntitiesBombExcluded;
        // return (List<CBaseModelEntity>?)(hiddenEntities.Count == 0 ? null : hiddenEntities);
    }

    private static void UpdateWeaponMeshGroupMask(CBaseEntity weapon, bool isLegacy = false)
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

    private void UpdatePlayerEconItemId(CEconItemView econItemView)
    {
        var itemId = _nextItemId++;

        econItemView.ItemID = itemId;
        econItemView.ItemIDLow = (uint)itemId & 0xFFFFFFFF;
        econItemView.ItemIDHigh = (uint)itemId >> 32;
    }

    private static void UpdatePlayerWeaponMeshGroupMask(CCSPlayerController player, CBasePlayerWeapon weapon, bool isLegacy)
    {
        UpdateWeaponMeshGroupMask(weapon, isLegacy);
    }

    private void SetVisibilityLevel(CCSPlayerController player, float invisibilityLevel)
    {
        if (invisibilityLevel >= 1.0f)
            invisibilityLevel = 1.0f;

        if (invisibilityLevel < 0.0f)
            invisibilityLevel = 0.0f;
        // go thru all wepons and mak dem hiden
        WeaponsMakeHidden(player, invisibilityLevel == 1.0f);

        TemUtils.SetPlayerInvisibilityLevel(player, invisibilityLevel);

        const int total_characters = 24;
        int visibility_level_in_lines = (int)(invisibilityLevel * total_characters);

        StringBuilder sb = new StringBuilder();
        for (int line = 0; line < total_characters; line++)
            sb.Append(line <= visibility_level_in_lines ? "█" : "░");
        sb.Append("]");

        player.PrintToCenterHtml(sb.ToString(), 2);
    }

    private void WeaponsMakeHidden(CCSPlayerController player, bool do_hide)
    {
        var pawn = player.PlayerPawn.Value!;

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

            if (do_hide)
            {
                foreach (var iter_user in playerHiddenEntities)
                    if (iter_user.Key != player)
                        iter_user.Value.Add(gun.Value); // hid from everyone else
            }
            else
            {
                foreach (var iter_user in playerHiddenEntities)
                    if (iter_user.Key != player)
                        iter_user.Value.Remove(gun.Value); // UNhid from everyone else
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

        foreach (var user in Users)
            SetVisibilityLevel(user, 0);
    }

    public override string GetDescription() => $"Gain invisibility, when not making sounds (Custom items will still be seen)";
    public override string GetDescriptionColored() => $"Gain " + NiceText.Blue("invisibility") + ", when not making sounds (Custom items will still be seen)";

    public double[] Levels = new double[65];
}