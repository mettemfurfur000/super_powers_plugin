using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace super_powers_plugin.src;

public class InfiniteAmmo : ISuperPower
{
    public InfiniteAmmo() => Triggers = [typeof(EventWeaponFire)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventWeaponFire)gameEvent;
        var player = realEvent.Userid;

        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(player))
            return HookResult.Continue;

        if (player != null && player.IsValid && player.PawnIsAlive)
        {
            CBasePlayerWeapon? activeWeapon = player?.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

            if (activeWeapon == null)
                return HookResult.Continue;

            if (activeWeapon.Clip1 < 5)
                activeWeapon.Clip1 = 5;
            else
                activeWeapon.Clip1 += 1;

        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Zeus included, nades not included";
}

