using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using super_powers_plugin.src;

public class BiocodedWeaponModifier : BaseWeaponModifier
{
    public BiocodedWeaponModifier()
    {
        Triggers = [typeof(EventWeaponFire)];
    }

    public override HookResult Execute(GameEvent gameEvent, WeaponModifierContext ctx)
    {
        if (gameEvent is not EventWeaponFire) return HookResult.Continue;

        UInt32 lowId = ctx.Weapon.OriginalOwnerXuidLow;
        UInt32 highId = ctx.Weapon.OriginalOwnerXuidHigh;
        ulong ownerSteamId = ((ulong)highId << 32) | lowId;

        if (ctx.Wielder.SteamID != ownerSteamId)
        {
            ctx.Wielder.DropActiveWeapon();
            ctx.Wielder.PrintToggleable($"{ChatColors.DarkRed}[BIOCODED] This weapon refuses to fire");
            return HookResult.Stop;
        }

        return HookResult.Continue;
    }

    public override string GetDescriptionColored() =>
        $"Weapon refuses to function for anyone but the {StringHelpers.Red("original owner")}";
}
