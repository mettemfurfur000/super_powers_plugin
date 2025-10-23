using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class FakePassport : BasePower
{
    public FakePassport()
    {
        Triggers = [typeof(EventRoundStart), typeof(EventPlayerDeath), typeof(EventRoundEnd)];
        Price = 4500;
        Rarity = "Uncommon";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent == null)
            return HookResult.Continue;

        if (gameEvent.GetType() == typeof(EventRoundEnd))
        {
            Users.ForEach(user =>
            {
                var pawn = user.PlayerPawn.Value!;

                if (pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                {
                    // Server.PrintToChatAll($"{user.PlayerName} is alive, resetting");
                    consecutiveDeaths[user] = 0;
                }
            });
        }

        if (gameEvent.GetType() == typeof(EventPlayerDeath))
        {
            EventPlayerDeath realEvent = (EventPlayerDeath)gameEvent;
            if (realEvent.Userid == null || !Users.Contains(realEvent.Userid))
                return HookResult.Continue;

            var victim = realEvent.Userid;
            var isHeadshot = !cfg_requireHeadshotDealth || realEvent.Headshot; // always true if headhots ar not required

            if (!consecutiveDeaths.ContainsKey(victim))
                consecutiveDeaths.Add(victim, isHeadshot ? 1 : 0);

            consecutiveDeaths[realEvent.Userid] = isHeadshot ? consecutiveDeaths[realEvent.Userid] + 1 : 0;

            // Server.PrintToChatAll($"{victim.PlayerName} dead, streak {consecutiveDeaths[realEvent.Userid]}");
        }

        if (gameEvent.GetType() == typeof(EventRoundStart))
        {
            Users.ForEach(user =>
            {
                if (consecutiveDeaths.TryAdd(user, 0))
                    return;

                int cur_streak = consecutiveDeaths[user];

                var pawn = user.PlayerPawn.Value!;

                pawn.Health = (int)(pawn.Health * Math.Pow(cfg_health_mult, cur_streak));
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

                // Server.PrintToChatAll($"{user.PlayerName} respawn, streak {cur_streak}");
            });
        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Gain {cfg_health_mult}X health on the round start for each consecutive death {(cfg_requireHeadshotDealth ? "from a headshot" : "")}";
    public override string GetDescriptionColored() => "Gain " + StringHelpers.Green($"{cfg_health_mult}X health") + " on the round start for each " + StringHelpers.Red("consecutive death" + (cfg_requireHeadshotDealth ? " from a headshot" : ""));

    public float cfg_health_mult = 2f;
    public bool cfg_requireHeadshotDealth = true;
    public Dictionary<CCSPlayerController, int> consecutiveDeaths = [];
}
