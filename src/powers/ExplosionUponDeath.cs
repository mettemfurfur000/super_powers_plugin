using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

using super_powers_plugin.src;

public class ExplosionUponDeath : BasePower
{
    public ExplosionUponDeath()
    {
        Triggers = [typeof(EventPlayerDeath)];
        Price = 2500;
        Rarity = "Common";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() != Triggers[0])
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

        var explosion_fx = Utilities.CreateEntityByName<CEnvExplosion>("env_explosion");

        var node = pawn.CBodyComponent!.SceneNode;

        explosion_fx!.Teleport(node!.AbsOrigin, node!.AbsRotation, new Vector(0, 0, 0));
        explosion_fx!.DispatchSpawn();
        explosion_fx!.PlayerDamage = damage;
        explosion_fx!.RadiusOverride = radius;
        explosion_fx!.Magnitude = (int)magnitute;
        // explosion_fx!.CustomDamageType = DamageTypes_t.DMG_BLAST;
        // explosion_fx!.CreateDebris = true;
        explosion_fx.AcceptInput("Explode");

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Explode on death, dealing {damage} damage in a {radius}Hu radius";
    public override string GetDescriptionColored() => "Explode on death, dealing " + StringHelpers.Red(damage.ToString() + " dmg") + " in a " + StringHelpers.Blue(radius) + " Hu radius";

    private int radius = 500;
    private float damage = 150f;
    private float magnitute = 75f;
}

