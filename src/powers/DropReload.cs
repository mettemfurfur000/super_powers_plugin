using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class DropReload : BasePower
{
    public DropReload()
    {
        Triggers = []; // EventItemRemove never fires, inventories are polled instead
        Price = 4500;
        Rarity = "Uncommon";

        tracker.WeaponRemoved += OnWeaponRemoved;
    }

    private readonly InventoryTracker tracker = new();

    public override void Update()
    {
        tracker.Update();
    }

    private void OnWeaponRemoved(CCSPlayerController user, string designerName, int lastClip1)
    {
        if (!Users.Contains(user))
            return;

        var pawn = user.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return;

        // drop reload - dropping your empty weapon fully reloads your second weapon

        if (lastClip1 > 0) // only EMPTY weapons trigger the reload
            return;

        // a real drop leaves a weapon entity lying near the player,
        // thrown grenades or stripped loadouts don't - this filters them out
        if (!IsLyingNearby(designerName, pawn))
            return;

        var secondWeapon = pawn.WeaponServices!.MyWeapons
            .Select(w => w.Value)
            .Where(w => w != null && w.IsValid && w.DesignerName != designerName)
            .FirstOrDefault(w => w!.VData != null && w.VData.MaxClip1 > 0);

        if (secondWeapon == null || !secondWeapon.IsValid)
            return;

        secondWeapon.Clip1 = secondWeapon.VData!.MaxClip1;
        Utilities.SetStateChanged(secondWeapon, "CBasePlayerWeapon", "m_iClip1");
    }

    private bool IsLyingNearby(string itemName, CCSPlayerPawn pawn)
    {
        var origin = pawn.AbsOrigin!;

        foreach (var weapon in Utilities.FindAllEntitiesByDesignerName<CBasePlayerWeapon>(itemName))
        {
            if (!weapon.IsValid || weapon.AbsOrigin == null)
                continue;
            if (weapon.OwnerEntity.Value != null && weapon.OwnerEntity.Value.IsValid)
                continue; // carried by someone
            if (weapon.VData == null || weapon.VData.MaxClip1 <= 0)
                continue; // not a droppable gun (knife/c4/grenades)
            if (TemUtils.CalcDistance(weapon.AbsOrigin, origin) > cfg_maxDropDistance)
                continue;

            return true;
        }

        return false;
    }

    public override bool OnAdd(CCSPlayerController player, bool forced = false)
    {
        bool added = base.OnAdd(player, forced);
        if (added)
            tracker.Watch(player);
        return added;
    }

    public override void OnRemoveUser(CCSPlayerController? player, bool reasonDisconnect)
    {
        base.OnRemoveUser(player, reasonDisconnect);
        if (player == null)
            tracker.Clear();
        else
            tracker.Forget(player);
    }


    public override string GetDescriptionColored() =>
        "Dropping your " + StringHelpers.Red("empty weapon") + " fully reloads your second one";

    public float cfg_maxDropDistance = 128f;
}
