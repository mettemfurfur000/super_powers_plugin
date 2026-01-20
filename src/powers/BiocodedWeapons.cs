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

        switch (gameEvent)
        {
            case EventWeaponFire fireEvent:
                shooter = fireEvent.Userid;
                break;
            case EventInspectWeapon inspectEvent:
                shooter = inspectEvent.Userid;
                break;
        }

        if (shooter == null)
            return HookResult.Continue;

        if (PlayerCanUse(shooter))
            return HookResult.Continue;

        shooter.DropActiveWeapon();
        shooter.PrintToggleable($"{ChatColors.DarkRed}[BIOCODED] Weapon is biocoded and can't be used");

        return HookResult.Continue;
    }

    // if owners does not match and owner has this power, make weapon unusable
    public bool PlayerCanUse(CCSPlayerController currentUser)
    {
        CCSPlayerController? originalOwner = Utilities.GetPlayerFromSteamId(currentUser.PlayerPawn!.Value!.WeaponServices!.ActiveWeapon.Value!.OriginalOwnerXuidLow);

        if (originalOwner == null) // owner not found
            return true;

        if (!Users.Contains(originalOwner)) // owner does not have the power
            return true;

        if (currentUser != originalOwner) // if shooter is not the owner (owner has the power)
            return false;

        return true; // shooter is the owner
    }


    public override string GetDescriptionColored() => $"{StringHelpers.Red("Weapons")} you bought are {StringHelpers.Red("biocoded")} to you only";
}

