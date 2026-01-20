using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class NukeNades : BasePower
{
    public NukeNades()
    {
        Triggers = [typeof(EventGrenadeThrown)];
        Price = 7000;
        Rarity = "Rare";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() != Triggers[0])
            return HookResult.Continue;

        var realEvent = (EventGrenadeThrown)gameEvent;
        var player = realEvent.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(player))
            return HookResult.Continue;

        var all_grenades = Utilities.FindAllEntitiesByDesignerName<CHEGrenadeProjectile>("hegrenade_projectile");
        if (!all_grenades.Any())
            return HookResult.Continue;

        all_grenades.ToList().ForEach(grenade =>
        {
            double secondsToExplod = grenade.DetonateTime - Server.CurrentTime;

            if (
                secondsToExplod == knownDelay &&
                player.UserId == grenade.Thrower.Value!.OriginalController.Value!.UserId)
            {
                grenade.Damage *= cfg_multiplier;
                grenade.DmgRadius *= cfg_multiplier;

                grenade.DetonateTime += (float)(cfg_detonateTime - knownDelay);
            }
        });

        return HookResult.Continue;
    }
    public readonly double knownDelay = 1.5;

    public override string GetDescriptionColored() => "HE grenades are " + StringHelpers.Red(cfg_multiplier) + " times more explosive takes " + StringHelpers.Red(cfg_detonateTime) + " to explode";
    public float cfg_multiplier = 10;
    public float cfg_detonateTime = 5;
}

