using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class EvilAura : BasePower
{
    public EvilAura()
    {
        Triggers = [typeof(EventRoundStart)];
        Price = 9000;
        Rarity = "Rare";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        return HookResult.Continue;
    }

    public override void Update()
    {
        if (Server.TickCount % period != 0)
            return;
        var players = Utilities.GetPlayers();
        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                continue;

            if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                continue;

            var playersInRadius = players.Where(p => p.PlayerPawn.Value != null && TemUtils.CalcDistance(p.PlayerPawn.Value.AbsOrigin!, pawn.AbsOrigin!) <= distance);

            foreach (var player_to_harm in playersInRadius)
            {
                if (player_to_harm.TeamNum == user.TeamNum) // skip teammates
                    continue;
                if (player_to_harm.TeamNum == 1) // skip spectators, just in case
                    continue;
                var harm_pawn = player_to_harm.PlayerPawn.Value!;
                if (harm_pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) // only harm alive specimens
                    continue;
                if (harm_pawn.Health <= 2)
                    continue;

                harm_pawn.Health = harm_pawn.Health - damage;
                Utilities.SetStateChanged(harm_pawn, "CBaseEntity", "m_iHealth");
            }
        }
    }

    private float distance = 250;
    private int damage = 1;
    private int period = 16;

    public override string GetDescription() => $"Slowly harm enemies close to you. Can't kill";
    public override string GetDescriptionColored() => $"Slowly " + StringHelpers.Red("harm") + " enemies close to you. Can't kill";
}

