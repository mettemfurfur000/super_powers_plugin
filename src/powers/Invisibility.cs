using System.Runtime.InteropServices;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory;
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
            typeof(EventRoundStart)
        ];

        Price = 8000;
        Rarity = PowerRarity.Legendary;

        checkTransmitListenerEnabled = true;
    }

    private int soundDivider = 1000;
    private int fullRecoverMs = 800;
    private bool sendBar = true;
    private int tickSkip = 3;
    private float visibilityFloor = 0.5f;
    private float weaponRevealFactor = 0.5f;
    private float weaponRevealFactorSilenced = 0.2f;

    public bool bombFoundForTheRound = false;
    public CBasePlayerWeapon? bomb = null;
    public List<CBaseModelEntity> hiddenEntitiesBombIncluded = [];
    public List<CBaseModelEntity> hiddenEntities = [];

    public List<CBasePlayerWeapon> washedWeapons = [];

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent is EventRoundStart realEventStart)
        {
            bombFoundForTheRound = false;
            hiddenEntitiesBombIncluded.Clear();
            hiddenEntities.Clear();
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
            HandleEvent(realEventSound.Userid, realEventSound.Radius / soundDivider);
        if (gameEvent is EventWeaponFire realEventFire)
            HandleEvent(realEventFire.Userid, (float)(realEventFire.Silenced ? weaponRevealFactorSilenced : weaponRevealFactor));

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
            TemUtils.SetPlayerInvisibilityLevel(player, (float)newValue);

            if (sendBar)
                if (newValue != Levels[i])
                    UpdateVisibilityBar(player, (float)newValue);

            Levels[i] = newValue;
        }

        // find and hide the bomb if the invisible user carries it

        if (Server.TickCount % 16 != 0) // 4 times per second
            return;

        if (!bombFoundForTheRound)
            Users.ForEach(user =>
            {
                var pawn = user.PlayerPawn.Value!;

                var weaponServices = pawn.WeaponServices;
                if (weaponServices != null)
                {
                    var myWeapons = weaponServices.MyWeapons;
                    if (myWeapons != null)
                        foreach (var gun in myWeapons)
                        {
                            var realWeapon = gun.Value;

                            if (realWeapon == null)
                                continue;

                            if (realWeapon!.DesignerName == "weapon_c4")
                            {
                                // Server.PrintToChatAll("bomba detectod");
                                bomb = realWeapon;
                                bombFoundForTheRound = true;

                                hiddenEntitiesBombIncluded.Add(bomb);
                                hiddenEntities.Add(bomb);

                                // Server.PrintToChatAll("bomb found and hidden");
                            }
                        }
                }
            });
    }

    public override List<CBaseModelEntity>? GetHiddenEntities(CCSPlayerController player)
    {
        // if (Users.Contains(player)) // invisibility users see everything
        //     return null;
        if (player.TeamNum == (byte)CsTeam.Terrorist)               // terrorists see the bomb
            return hiddenEntities.Count == 0 ? null : hiddenEntities;
        return hiddenEntitiesBombIncluded.Count == 0 ? null : hiddenEntitiesBombIncluded;
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

        var viewModel = GetPlayerViewModel(player);
        if (viewModel == null || viewModel.Weapon.Value == null ||
            viewModel.Weapon.Value.Index != weapon.Index) return;

        UpdateWeaponMeshGroupMask(viewModel, isLegacy);
        Utilities.SetStateChanged(viewModel, "CBaseEntity", "m_CBodyComponent");
    }

    private static unsafe CBaseViewModel? GetPlayerViewModel(CCSPlayerController player)
    {
        if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.ViewModelServices == null) return null;
        CCSPlayer_ViewModelServices viewModelServices = new(player.PlayerPawn.Value.ViewModelServices!.Handle);
        var ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
        var references = MemoryMarshal.CreateSpan(ref ptr, 3);
        var viewModel = (CHandle<CBaseViewModel>)Activator.CreateInstance(typeof(CHandle<CBaseViewModel>), references[0])!;
        return viewModel.Value == null ? null : viewModel.Value;
    }

    private void UpdateVisibilityBar(CCSPlayerController player, float invisibilityLevel)
    {
        if (invisibilityLevel >= 1.0f)
            invisibilityLevel = 1.0f;

        if (invisibilityLevel < 0.0f)
            invisibilityLevel = 0.0f;

        const int total_characters = 24;
        int visibility_level_in_lines = (int)(invisibilityLevel * total_characters);

        StringBuilder sb = new StringBuilder();
        for (int line = 0; line < total_characters; line++)
            sb.Append(line <= visibility_level_in_lines ? "█" : "░");
        sb.Append("]");

        player.PrintToCenterHtml(sb.ToString(), 2);
    }

    public override string GetDescription() => $"Gain invisibility, when not making sounds (Custom items will still be seen)";
    public override string GetDescriptionColored() => $"Gain " + NiceText.Blue("invisibility") + ", when not making sounds (Custom items will still be seen)";

    public double[] Levels = new double[65];
}