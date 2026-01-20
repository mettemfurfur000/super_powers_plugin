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
        explosion_fx!.PlayerDamage = cfg_damage;
        explosion_fx!.RadiusOverride = cfg_radius;
        explosion_fx!.Magnitude = (int)cfg_magnitute;
        // explosion_fx!.CustomDamageType = DamageTypes_t.DMG_BLAST;
        // explosion_fx!.CreateDebris = true;
        explosion_fx.AcceptInput("Explode");

        return HookResult.Continue;
    }


    public override string GetDescriptionColored() => "Explode on death, dealing " + StringHelpers.Red(cfg_damage.ToString() + " dmg") + " in a " + StringHelpers.Blue(cfg_radius) + " Hu radius";

    public int cfg_radius = 500;
    public float cfg_damage = 150f;
    public float cfg_magnitute = 75f;
}

