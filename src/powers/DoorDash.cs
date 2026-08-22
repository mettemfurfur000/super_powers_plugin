using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

using super_powers_plugin.src;

public class DoorDash : BasePower
{
    public DoorDash()
    {
        Triggers = [typeof(EventPlayerSpawn)];
        Price = 2500;
        Rarity = "Common";
    }

    private class AfkSession
    {
        public Vector LastPosition = new(0, 0, 0);
        public int NextPayTick;
    }

    private Dictionary<CCSPlayerController, AfkSession> sessions = [];

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() != Triggers[0])
            return HookResult.Continue;

        EventPlayerSpawn realEvent = (EventPlayerSpawn)gameEvent;
        var user = realEvent.Userid;

        if (user == null || !user.IsValid || !Users.Contains(user))
            return HookResult.Continue;

        var pawn = user.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
            return HookResult.Continue;

        // a fresh respawn starts a new AFK watch
        sessions[user] = new AfkSession
        {
            LastPosition = new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z),
            NextPayTick = Server.TickCount + (int)(cfg_periodSeconds * 64)
        };

        return HookResult.Continue;
    }

    public override void Update()
    {
        if (Server.TickCount % 32 != 0) // twice per second is plenty for spotting movement
            return;

        foreach (var entry in sessions.ToList())
        {
            var user = entry.Key;
            var session = entry.Value;

            var pawn = user.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE || pawn.AbsOrigin == null)
                continue;

            var position = pawn.AbsOrigin;

            // moved away from where we last saw them - AFK streak is broken until the next respawn
            if (TemUtils.CalcDistance(position, session.LastPosition) > cfg_maxMovementUnits)
            {
                sessions.Remove(user);
                continue;
            }

            if (!cfg_payDuringFreeze && IsFreezePeriod())
                continue;

            if (Server.TickCount >= session.NextPayTick)
            {
                session.NextPayTick += (int)(cfg_periodSeconds * 64);
                TemUtils.GiveMoney(user, cfg_moneyPerPeriod, $"for standing still ({StringHelpers.GetPowerColoredName(this)})");
            }
        }
    }

    private static bool IsFreezePeriod()
    {
        try { return TemUtils.GetGameRules().FreezePeriod; }
        catch { return false; }
    }

    public override void OnRemoveUser(CCSPlayerController? player, bool reasonDisconnect)
    {
        base.OnRemoveUser(player, reasonDisconnect);
        if (player != null)
            sessions.Remove(player);
    }

    public override void OnRemovePower(CCSPlayerController? player)
    {
        base.OnRemovePower(player);
        if (player == null)
            sessions.Clear();
    }


    public override string GetDescriptionColored() =>
        "For every " + StringHelpers.Green(cfg_periodSeconds) + " seconds you stand still after respawning," +
        " gain " + StringHelpers.Green(cfg_moneyPerPeriod + "$");

    public int cfg_moneyPerPeriod = 100;
    public float cfg_periodSeconds = 2f;
    public float cfg_maxMovementUnits = 8f;
    public bool cfg_payDuringFreeze = false;
}
