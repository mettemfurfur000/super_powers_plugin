using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class InfiniteAmmo : BasePower
{
    public InfiniteAmmo()
    {
        Triggers = [typeof(EventWeaponFire)];
        Price = 6500;
        Rarity = "Uncommon";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() != Triggers[0])
            return HookResult.Continue;

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

    public override string GetDescription() => $"Free ammo, yay!";
    public override string GetDescriptionColored() => NiceText.Blue("Free ammo, yay!");
}

