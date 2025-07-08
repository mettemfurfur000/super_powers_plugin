using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

using super_powers_plugin.src;

public class PoisonedSmoke : BasePower
{
    public PoisonedSmoke()
    {
        Triggers = [typeof(EventSmokegrenadeDetonate), typeof(EventSmokegrenadeExpired)];

        Price = 5000;
        Rarity = "Uncommon";
    }
    
    public override HookResult Execute(GameEvent gameEvent)
    {
        Type type = gameEvent.GetType();
        if (type == typeof(EventSmokegrenadeDetonate))
        {
            var realEvent = (EventSmokegrenadeDetonate)gameEvent;

            var thrower = realEvent.Userid;

            if (thrower == null || !thrower.IsValid)
                return HookResult.Continue;

            if (!Users.Contains(thrower))
                return HookResult.Continue;

            SmokesActivePos.Add(Tuple.Create(realEvent.Entityid, new Vector(realEvent.X, realEvent.Y, realEvent.Z)));

            var smokeEntity = Utilities.GetEntityFromIndex<CSmokeGrenadeProjectile>(realEvent.Entityid);

            if (smokeEntity != null)
            {
                smokeEntity.SmokeColor.X = 0.0f;
                smokeEntity.SmokeColor.Y = Random.Shared.NextSingle() * 255.0f;
                smokeEntity.SmokeColor.Z = 0.0f;
                // Utilities.SetStateChanged(smokeEntity, "CSmokeGrenadeProjectile", "m_vSmokeColor");
                // Server.PrintToChatAll($"set to green entity {smokeEntity.DesignerName}");
            }

        }

        if (type == typeof(EventSmokegrenadeExpired))
        {
            var realEvent = (EventSmokegrenadeExpired)gameEvent;

            SmokesActivePos.RemoveAll(t => t.Item1 == realEvent.Entityid);
        }

        return HookResult.Continue;
    }

    public override void Update()
    {
        if (Server.TickCount % 64 != 0)
            return;

        var players = Utilities.GetPlayers();

        foreach (var pos in SmokesActivePos)
        {
            var playersInRadius = players.Where(p => p.PlayerPawn.Value != null && TemUtils.CalcDistance(p.PlayerPawn.Value.AbsOrigin!, pos.Item2) <= smoke_radius);

            foreach (var player_to_harm in playersInRadius)
            {
                if (player_to_harm.TeamNum == 1) // skip spectators, just in case
                    continue;
                var harm_pawn = player_to_harm.PlayerPawn.Value!;
                if (harm_pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) // only harm alive specimens
                    continue;
                if (harm_pawn.Health <= value) // dont ever touch players with low health
                {
                    harm_pawn.CommitSuicide(false, true);
                    continue;
                }

                harm_pawn.Health = harm_pawn.Health - value;
                Utilities.SetStateChanged(harm_pawn, "CBaseEntity", "m_iHealth");
            }
        }

    }

    public List<Tuple<int, Vector>> SmokesActivePos = [];

    public override string GetDescription() => $"Your smoke is poisoned, deals {value} damage, but cant kill on its own";
    public override string GetDescriptionColored() => "Your smoke is poisoned, deals " + NiceText.Red(value) + " damage, but cant kill on its own";

    private int value = 2;
    private int smoke_radius = 144;
}

