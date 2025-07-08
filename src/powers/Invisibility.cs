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
            typeof(EventItemEquip)
        ];

        Price = 8000;
        Rarity = PowerRarity.Legendary;
    }

    private int soundDivider = 1000;
    private int fullRecoverMs = 800;
    private bool sendBar = true;
    private int tickSkip = 3;
    private float visibilityFloor = 0.5f;
    private float weaponRevealFactor = 0.5f;
    private float weaponRevealFactorSilenced = 0.2f;

    public List<CBasePlayerWeapon> washedWeapons = [];

    public override HookResult Execute(GameEvent gameEvent)
    {
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

    // public void CleanActiveWeapon(CCSPlayerController user)
    // {
    //     var weapon = user.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value!;

    //     if (washedWeapons.Contains(weapon))
    //         return;
    //     washedWeapons.Add(weapon);


    //     string weapon_name = weapon.GetDesignerName();
    //     string cur_model = weapon.GetModel();

    //     Weapon.RemoveWeapon(user, weapon_name);

    //     string original_key = $"{weapon_name}:{cur_model}";
    //     string madeUp_key = $"{weapon_name}:weapons/models/glock18/weapon_pist_glock18.vmdl";

    //     TemUtils.__plugin!.AddTimer(0.05f, () =>
    //     {
    //         user.GiveNamedItem(weapon_name);

    //         Weapon.EquipWeapon(user, madeUp_key);
    //         TemUtils.__plugin!.AddTimer(0.05f, () =>
    //         {
    //             Weapon.EquipWeapon(user, original_key);
    //             user.ExecuteClientCommand("lastinv");
    //         });
    //         TemUtils.__plugin!.AddTimer(0.05f, () =>
    //         {
    //             user.ExecuteClientCommand("lastinv");
    //             TemUtils.__plugin!.AddTimer(0.05f, () => { user.ExecuteClientCommand("lastinv"); });
    //         });
    //     });
    // }

    // public override void OnRemovePower(CCSPlayerController? player)
    // {
    //     if (player == null)
    //     {
    //         foreach (var p in Users)
    //         {
    //             Levels[Users.IndexOf(p)] = -1.0f;
    //             TemUtils.SetPlayerInvisibilityLevel(p, 0.0f);
    //         }
    //         return;
    //     }

    //     Levels[Users.IndexOf(player)] = -1.0f;
    //     TemUtils.SetPlayerInvisibilityLevel(player, 0.0f);
    // }

    public override string GetDescription() => $"Gain invisibility, when not making sounds (Custom items will still be seen)";
    public override string GetDescriptionColored() => $"Gain " + NiceText.Blue("invisibility") + ", when not making sounds (Custom items will still be seen)";

    public double[] Levels = new double[65];
}

// stolen code
// cant mak it wok

// public static class Weapon
// {
//     public static string GetDesignerName(this CBasePlayerWeapon weapon)
//     {
//         string weaponDesignerName = weapon.DesignerName;
//         ushort weaponIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;

//         weaponDesignerName = (weaponDesignerName, weaponIndex) switch
//         {
//             var (name, _) when name.Contains("bayonet") => "weapon_knife",
//             ("weapon_m4a1", 60) => "weapon_m4a1_silencer",
//             ("weapon_hkp2000", 61) => "weapon_usp_silencer",
//             ("weapon_deagle", 64) => "weapon_revolver",
//             ("weapon_mp7", 23) => "weapon_mp5sd",
//             _ => weaponDesignerName
//         };

//         return weaponDesignerName;
//     }

//     public static unsafe string GetViewModel(CCSPlayerController player)
//     {
//         var viewModel = ViewModel(player)?.VMName ?? string.Empty;
//         return viewModel;
//     }

//     public static unsafe void SetViewModel(CCSPlayerController player, string model)
//     {
//         ViewModel(player)?.SetModel(model);
//     }

//     public static void UpdateModel(CCSPlayerController player, CBasePlayerWeapon weapon, string model, string worldmodel, bool update)
//     {
//         weapon.Globalname = $"{GetViewModel(player)},{model}";
//         weapon.SetModel(worldmodel);

//         if (update)
//             SetViewModel(player, model);
//     }

//     public static void ResetWeapon(CCSPlayerController player, CBasePlayerWeapon weapon, bool update)
//     {
//         string globalname = weapon.Globalname;

//         if (string.IsNullOrEmpty(globalname))
//             return;

//         string[] globalnamedata = globalname.Split(',');

//         weapon.Globalname = string.Empty;
//         weapon.SetModel(globalnamedata[0]);

//         if (update)
//             SetViewModel(player, globalnamedata[0]);
//     }

//     public static string GetModel(this CBaseEntity ent) => ent.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState?.ModelName ?? string.Empty;

//     public static string FigureOutModel(CBasePlayerWeapon weapon)
//     {
//         string model = GetModel(weapon);

//         return model;
//     }

//     public static bool EquipWeapon(CCSPlayerController player, string model)
//     {
//         return Weapon.HandleEquip(player, model, true);
//     }

//     public static bool HandleEquip(CCSPlayerController player, string modelName, bool isEquip)
//     {
//         if (player.PawnIsAlive)
//         {
//             var weaponpart = modelName.Split(':');
//             if (weaponpart.Length != 2 && weaponpart.Length != 3)
//                 return false;

//             string weaponName = weaponpart[0];
//             string weaponModel = weaponpart[1];
//             string worldModel = weaponpart[1];

//             if (weaponpart.Length == 3)
//                 worldModel = weaponpart[2];

//             CBasePlayerWeapon? weapon = Get(player, weaponName);

//             if (weapon != null)
//             {
//                 bool equip = weapon == player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

//                 if (isEquip)
//                     UpdateModel(player, weapon, weaponModel, worldModel, equip);

//                 else ResetWeapon(player, weapon, equip);

//                 return true;
//             }

//             else return false;
//         }

//         return true;
//     }

//     public static CBasePlayerWeapon? Get(CCSPlayerController player, string weaponName)
//     {
//         CPlayer_WeaponServices? weaponServices = player.PlayerPawn?.Value?.WeaponServices;

//         if (weaponServices == null)
//             return null;

//         CBasePlayerWeapon? activeWeapon = weaponServices.ActiveWeapon?.Value;

//         if (activeWeapon != null && GetDesignerName(activeWeapon) == weaponName)
//             return activeWeapon;

//         return weaponServices.MyWeapons.SingleOrDefault(p => p.Value != null && GetDesignerName(p.Value) == weaponName)?.Value;
//     }

//     private static unsafe CBaseViewModel? ViewModel(CCSPlayerController player)
//     {
//         nint? handle = player.PlayerPawn.Value?.ViewModelServices?.Handle;

//         if (handle == null || !handle.HasValue)
//             return null;

//         CCSPlayer_ViewModelServices viewModelServices = new(handle.Value);

//         nint ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
//         Span<nint> viewModels = MemoryMarshal.CreateSpan(ref ptr, 3);

//         CHandle<CBaseViewModel> viewModel = new(viewModels[0]);

//         return viewModel.Value;
//     }

//     public static void RemoveWeapon(CCSPlayerController player, string weaponName)
//     {
//         CPlayer_WeaponServices? weaponServices = player.PlayerPawn?.Value?.WeaponServices;

//         if (weaponServices == null)
//             return;

//         var matchedWeapon = weaponServices.MyWeapons
//         .FirstOrDefault(w => w?.IsValid == true && w.Value != null && w.Value.DesignerName == weaponName);

//         try
//         {
//             if (matchedWeapon?.IsValid == true)
//             {
//                 weaponServices.ActiveWeapon.Raw = matchedWeapon.Raw;

//                 CBaseEntity? weaponEntity = weaponServices.ActiveWeapon.Value?.As<CBaseEntity>();
//                 if (weaponEntity == null || !weaponEntity.IsValid)
//                     return;

//                 player.DropActiveWeapon();
//                 weaponEntity?.AddEntityIOEvent("Kill", weaponEntity, null, "", 0.1f);
//             }
//         }
//         catch (Exception ex)
//         {
//             Server.PrintToConsole($"Error while Refreshing Weapon via className: {ex.Message}");
//         }
//     }
// }