using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using super_powers_plugin.src;

public class InfiniteAmmoWeaponModifier : BaseWeaponModifier
{
    public InfiniteAmmoWeaponModifier()
    {
        Triggers = [typeof(EventWeaponFire)];
    }

    public override HookResult Execute(GameEvent gameEvent, WeaponModifierContext ctx)
    {
        if (gameEvent is not EventWeaponFire) return HookResult.Continue;

        var weapon = ctx.Weapon;

        if (weapon == null || !weapon.IsValid || weapon.VData == null)
            return HookResult.Continue;

        // if (weapon.Clip1 < weapon.VData.MaxClip1)
        // {
        //     weapon.Clip1 = weapon.VData.MaxClip1 + 1;
        //     Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
        // }

        weapon.Clip1++;
        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");

        return HookResult.Continue;
    }

    public override string GetDescriptionColored() =>
        $"Weapon never runs out of {StringHelpers.Green("ammo")}";
}
