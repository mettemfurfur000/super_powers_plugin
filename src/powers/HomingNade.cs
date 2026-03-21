using System.Diagnostics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using RayTrace;
using super_powers_plugin.src;

public class HomingNade : BasePower
{
    public HomingNade()
    {
        Triggers = [
            // typeof(EventHegrenadeDetonate),
            // typeof(EventMolotovDetonate),
            // typeof(EventSmokegrenadeDetonate),
            // typeof(EventFlashbangDetonate),
            // typeof(EventDecoyDetonate),
            typeof(EventGrenadeThrown),
            // typeof(EventRoundStart),
        ];

        Price = 8000;
        Rarity = "Rare";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() != Triggers[0])
            return HookResult.Continue;

        EventGrenadeThrown thrown = (EventGrenadeThrown)gameEvent;
        CCSPlayerController? player = thrown.Userid;

        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(player))
            return HookResult.Continue;

        var match_grenade = Utilities.FindAllEntitiesByDesignerName<CHEGrenadeProjectile>(thrown.Weapon + "_projectile");
        if (match_grenade.Count() == 0)
            return HookResult.Continue;

        var grenade = match_grenade.First();

        if (grenade != null && player.UserId == grenade.Thrower.Value!.OriginalController.Value!.UserId)
        {
            liveGrenades.Add(grenade);
            // Server.PrintToChatAll("Added a nade to the list");
        }

        return HookResult.Continue;
    }

    public override void Update()
    {
        liveGrenades.RemoveAll((g) =>
        {
            var isInvalid = !g.IsValid || g == null || g.DetonationRecorded;
            // if (isInvalid)
            // Server.PrintToChatAll("Deleted an invalid nade");
            return isInvalid;
        });

        var players = Utilities.GetPlayers().Where(p => (LifeState_t)p.LifeState == LifeState_t.LIFE_ALIVE); // Assuming player

        liveGrenades.ForEach(grenade =>
        {
            foreach (var player in players)
            {
                // skip players too far away and on the same team as the grenade owner
                if (player.TeamNum == grenade.TeamNum)
                    continue;
                if (player.LifeState != (int)LifeState_t.LIFE_ALIVE)
                    continue;
                var eyePos = player.PlayerPawn.Value!.GetEyePosition();
                if (eyePos == null)
                    continue;
                if (eyePos.Distance(grenade.AbsOrigin!) > cfg_TraceRadius)
                    continue;

                QAngle directionAngle = eyePos.AngleTo(grenade.AbsOrigin!);
                var traceOptions = new RayTrace.TraceOptions()
                {
                    InteractsAs = (ulong)InteractionLayers.MASK_SHOT_FULL, // trace against the world and players, but not other grenades
                    InteractsExclude = (ulong)InteractionLayers.Player
                };

                if (CRayTrace.TraceShape(eyePos, directionAngle, null, traceOptions, out var traceResult) == false)
                    continue;

                if (!traceResult.DidHit)
                    continue;

                if (traceResult.HitEntity == 0)
                    continue;

                var entity = new CBaseEntity(traceResult.HitEntity);

                if (entity.DesignerName == "worldent")
                {
                    // Server.PrintToChatAll("TraceShape hit the world");
                    continue;
                }

                // Server.PrintToChatAll($"TraceShape hit entity {entity.DesignerName}, fraction: {traceResult.Fraction}");

                var pawn = player.PlayerPawn.Value!;

                var directionVec = pawn.GetEyePosition()! - grenade.AbsOrigin!;
                var length = directionVec.Length();
                var direction = directionVec.Normalize();
                var pull = (1 - (length / cfg_TraceRadius)) * (1 - (length / cfg_TraceRadius)); // pull is stronger the closer the grenade is to the player, with a quadratic curve
                var addVelocity = direction * cfg_HomingStrength * pull; // the closer the grenade is to the player, the stronger the homing effect

                Server.NextFrame(() =>
                {
                    grenade.AbsVelocity.Add(addVelocity);
                });
            }
        });
    }

    public float cfg_TraceRadius = 512.0f;
    public float cfg_HomingStrength = 64.0f;
    public List<CBaseCSGrenadeProjectile> liveGrenades = [];

    public override string GetDescriptionColored() => $"Grenades " + StringHelpers.Blue("gravitate") + " towards enemy players when thrown.";
}

