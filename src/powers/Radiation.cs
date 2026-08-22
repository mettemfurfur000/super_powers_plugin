using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

using super_powers_plugin.src;

public class Radiation : BasePower
{
    public Radiation()
    {
        Triggers = [];
        Price = 7000;
        Rarity = "Rare";
    }

    public override void Update()
    {
        if (Server.TickCount % cfg_periodTicks != 0)
            return;

        var traceOptions = new TraceOptions
        {
            InteractsAs = Contents.Solid,
            InteractsWith = Contents.Solid | Contents.Window, // only world geometry can block the sight
            InteractsExclude = Contents.Player // pawns never occlude
        };

        var players = Utilities.GetPlayers();
        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                continue;

            var eyePos = pawn.GetEyePosition();
            if (eyePos == null)
                continue;

            foreach (var enemy in players)
            {
                if (enemy.TeamNum == user.TeamNum) // skip teammates
                    continue;
                if (enemy.TeamNum == 1) // skip spectators, just in case
                    continue;

                var enemyPawn = enemy.PlayerPawn.Value;
                if (enemyPawn == null || !enemyPawn.IsValid || enemyPawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    continue;

                if (enemyPawn.Health <= cfg_healthCap) // never irradiate below the cap
                    continue;

                var enemyEyePos = enemyPawn.GetEyePosition();
                if (enemyEyePos == null || eyePos.Distance(enemyEyePos) > cfg_range)
                    continue;

                if (!IsInSight(eyePos, enemyEyePos, pawn, traceOptions))
                    continue;

                enemyPawn.Health = Math.Max(cfg_healthCap, enemyPawn.Health - cfg_damage);
                Utilities.SetStateChanged(enemyPawn, "CBaseEntity", "m_iHealth");
            }
        }
    }

    private bool IsInSight(Vector from, Vector to, CBaseEntity ignoreEntity, TraceOptions options)
    {
        // end the ray just outside the target's hull,
        // a segment terminating inside a body may register as a hit even with clear sight
        var deltaX = to.X - from.X;
        var deltaY = to.Y - from.Y;
        var deltaZ = to.Z - from.Z;
        float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);

        if (distance < cfg_traceMargin * 2f)
            return true; // practically standing inside each other

        float scale = (distance - cfg_traceMargin) / distance;
        var endPoint = new Vector(from.X + deltaX * scale, from.Y + deltaY * scale, from.Z + deltaZ * scale);

        var result = Trace.TraceEndShape(from, endPoint, ignoreEntity, options);
        return !result.DidHit(); // hit nothing on the way - target is visible
    }


    public override string GetDescriptionColored() =>
        "Deals " + StringHelpers.Red(cfg_damage) + " damage every second to enemies in sight," +
        " but never below " + StringHelpers.Blue(cfg_healthCap) + " health";

    public int cfg_damage = 1;
    public int cfg_healthCap = 50;
    public float cfg_range = 3072f;
    public int cfg_periodTicks = 64;
    public float cfg_traceMargin = 16f;
}
