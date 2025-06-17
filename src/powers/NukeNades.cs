using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class NukeNades : BasePower
{
    public NukeNades() => Triggers = [typeof(EventGrenadeThrown)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventGrenadeThrown)gameEvent;
        var player = realEvent.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(player))
            return HookResult.Continue;

        var all_grenades = Utilities.FindAllEntitiesByDesignerName<CHEGrenadeProjectile>("hegrenade_projectile");
        if (all_grenades.Count() == 0)
            return HookResult.Continue;

        var grenade = all_grenades.First();
        if (player.UserId == grenade.Thrower.Value!.OriginalController.Value!.UserId)
        {
            grenade.Damage *= multiplier;
            grenade.DmgRadius *= multiplier;

            grenade.DetonateTime += 1;
        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"HE grenades, but {multiplier} times more explosive";

    private float multiplier = 10;
}

