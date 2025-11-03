using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using super_powers_plugin.src;

public class BiocodedWeapons : BasePower
{
    public BiocodedWeapons()
    {
        Triggers = [typeof(EventWeaponFire), typeof(EventInspectWeapon)];
        Price = 2500;
        Rarity = "Uncommon";
    }

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
            shooter.PrintIfShould($"{ChatColors.DarkRed}[BIOCODED] Weapon is biocoded and can't be used");
        }

        return HookResult.Continue;
    }

    // if owners does not match and owner has this power, make weapon unusable
    public bool IsWeaponBiocoded(CCSPlayerController currentUser)
    {
        ulong shooterId = currentUser.SteamID;
        // ulong weaponOwnerId = TemUtils.GetActiveWeaponUserSteamId64(currentUser);

        CCSPlayerController? originalOwner = Utilities.GetPlayerFromSteamId(currentUser.PlayerPawn!.Value!.WeaponServices!.ActiveWeapon.Value!.OriginalOwnerXuidLow);

        if (originalOwner == null)
            return false;

        if (!Users.Contains(originalOwner))
            return false;

        if (currentUser != originalOwner)
            return true;

        // if (shooterId != weaponOwnerId) // if shooter is shooting someon esles weapon, check users
        //     foreach (var user in Users)
        //         if (user.SteamID == weaponOwnerId) // owner has the power
        //             return true;

        return false;
    }

    public override string GetDescription() => $"Only you can use weapons you bought";
    public override string GetDescriptionColored() => "Only you can use your " + StringHelpers.Red("weapons");
}

