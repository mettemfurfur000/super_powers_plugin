using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class InstantNades : BasePower
{
    public InstantNades()
    {
        Triggers = [typeof(EventGrenadeThrown)];
        Price = 3500;
        Rarity = "Uncommon";
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

        
        var match_grenade = Utilities.FindAllEntitiesByDesignerName<CHEGrenadeProjectile>(realEvent.Weapon + "_projectile");
        if (match_grenade.Count() == 0)
            return HookResult.Continue;

        var grenade = match_grenade.First();

        if (grenade != null && player.UserId == grenade.Thrower.Value!.OriginalController.Value!.UserId)
        {
            grenade.DetonateTime = Server.CurrentTime + 1 / cfg_divider;
        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Reduce nade and flash fuse time by {cfg_divider} times";
    public override string GetDescriptionColored() => "Reduce nade and flash fuse time by " + StringHelpers.Red(cfg_divider) + " times";

    public int cfg_divider = 4;
}

