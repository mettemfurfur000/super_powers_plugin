using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

using super_powers_plugin.src;

public class ExplosionUponDeath : BasePower
{
    public ExplosionUponDeath() => Triggers = [typeof(EventPlayerDeath)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent is null)
            return HookResult.Continue;

        EventPlayerDeath realEvent = (EventPlayerDeath)gameEvent;
        var player = realEvent.Userid;
        if (player == null)
            return HookResult.Continue;

        if (!Users.Where(p => p.UserId == player.UserId).Any())
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return HookResult.Continue;

        var heProjectile = Utilities.CreateEntityByName<CHEGrenadeProjectile>("hegrenade_projectile");

        if (heProjectile == null || !heProjectile.IsValid) return HookResult.Continue;

        var node = pawn.CBodyComponent!.SceneNode;
        Vector pos = node!.AbsOrigin;
        pos.Z += 10;
        heProjectile.Thrower.Raw = pawn.EntityHandle.Raw;
        heProjectile.TicksAtZeroVelocity = 100;
        heProjectile.TeamNum = pawn.TeamNum;
        heProjectile.Damage = damage;
        heProjectile.DmgRadius = radius;
        heProjectile.Teleport(pos, node!.AbsRotation, new Vector(0, 0, -10));
        heProjectile.DispatchSpawn();
        heProjectile.AcceptInput("InitializeSpawnFromWorld", player.PlayerPawn.Value!, player.PlayerPawn.Value!, "");
        heProjectile.DetonateTime = 0;

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Explode on death, dealing {damage} damage in a {radius} units radius";

    private int radius = 500;
    private float damage = 125f;
}

