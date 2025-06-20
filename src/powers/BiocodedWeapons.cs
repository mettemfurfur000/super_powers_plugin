using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class BiocodedWeapons : BasePower
{
    public BiocodedWeapons() => Triggers = [typeof(EventWeaponFire), typeof(EventInspectWeapon)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        CCSPlayerController? shooter = null;

        if (gameEvent.GetType() == typeof(EventInspectWeapon))
        {
            var realEvent = gameEvent as EventInspectWeapon;
            shooter = realEvent!.Userid;
            return HookResult.Continue;
        }
        else if (gameEvent.GetType() == typeof(EventWeaponFire))
        {
            var realEvent = gameEvent as EventWeaponFire;
            shooter = realEvent!.Userid;
            return HookResult.Continue;
        }

        if (shooter != null && IsWeaponBiocoded(shooter))
        {
            if (gameEvent.GetType() == typeof(EventWeaponFire))
                shooter.DropActiveWeapon();
            shooter.PrintToCenter($"Weapon is biocoded and can't be used");
        }

        return HookResult.Continue;
    }

    // if owners does not match and owner has this power, make weapon unusable
    private bool IsWeaponBiocoded(CCSPlayerController currentUser)
    {
        ulong shooterId = currentUser.SteamID;
        ulong weaponOwnerId = TemUtils.GetActiveWeaponUserSteamId64(currentUser);

        if (shooterId != weaponOwnerId) // if shooter is shooting someon esles weapon, check users
            foreach (var user in Users)
                if (user.SteamID == weaponOwnerId) // owner has the power
                    return true;

        return false;
    }

    public override string GetDescription() => $"Only you can use weapons you bought";
}

