using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace super_powers_plugin.src;

public class BiocodedWeapons : ISuperPower
{
    public BiocodedWeapons() => Triggers = [typeof(EventWeaponFire)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        EventWeaponFire realEvent = (EventWeaponFire)gameEvent;

        ulong ownerId = Combine(realEvent.Userid!.PlayerPawn!.Value!.WeaponServices!.ActiveWeapon.Value!.OriginalOwnerXuidLow, realEvent.Userid!.PlayerPawn!.Value!.WeaponServices!.ActiveWeapon.Value!.OriginalOwnerXuidHigh);

        var shooter = realEvent.Userid;
        ulong shooterId = shooter.SteamID;

        CCSPlayerController? owner = null;

        if (ownerId != shooterId) // shoter don match de weapon owner
            foreach (var user in Users)
                if (user.SteamID == ownerId) // owner found, he has dis power
                {
                    owner = user;
                    break;
                }

        if (owner == null) // owner not founde - donm car
            return HookResult.Continue;

        shooter.DropActiveWeapon();
        shooter.PrintToCenter($"Weapon is biocoded to {owner.PlayerName} and can't be used");

        if (damageOnUseBiocoded > 0)
        {
            var pawn = shooter.PlayerPawn.Value!;

            pawn.Health = pawn.Health <= damageOnUseBiocoded ? 1 : pawn.Health - damageOnUseBiocoded;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }

        return HookResult.Continue;
    }

    private static ulong Combine(uint a, uint b)
    {
        uint ua = (uint)a;
        ulong ub = (uint)b;
        return ub << 32 | ua;
    }

    public override string GetDescription() => $"Only you can use weapons you bought";
    private int damageOnUseBiocoded = 15;
}

