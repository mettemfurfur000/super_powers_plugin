using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class BountyHunter : BasePower
{
    public BountyHunter()
    {
        Triggers = [typeof(EventPlayerDeath), typeof(EventRoundStart), typeof(EventRoundEnd)];
        Price = 4500;
        Rarity = "Uncommon";
    }

    private Dictionary<ulong, int> killsThisRound = [];

    public override HookResult Execute(GameEvent gameEvent)
    {
        var eventType = gameEvent.GetType();

        if (eventType == typeof(EventPlayerDeath))
        {
            EventPlayerDeath realEvent = (EventPlayerDeath)gameEvent;
            var attacker = realEvent.Attacker;
            var victim = realEvent.Userid;

            if (attacker == null || !attacker.IsValid || victim == null)
                return HookResult.Continue;
            if (!Users.Contains(attacker))
                return HookResult.Continue;
            if (attacker.UserId == victim.UserId || attacker.TeamNum == victim.TeamNum)
                return HookResult.Continue;

            killsThisRound[attacker.SteamID] = killsThisRound.GetValueOrDefault(attacker.SteamID) + 1;
            TemUtils.GiveMoney(attacker, cfg_killBonus, $"for collecting a bounty ({StringHelpers.GetPowerColoredName(this)})");
        }

        if (eventType == typeof(EventRoundEnd))
        {
            foreach (var user in Users.ToList())
            {
                if (!user.IsValid)
                    continue;

                if (killsThisRound.GetValueOrDefault(user.SteamID) > 0)
                    continue;

                // take as much as they can pay, never go below zero
                int loss = Math.Min(cfg_noKillPenalty, Math.Max(0, user.InGameMoneyServices!.Account));
                if (loss == 0)
                    continue;

                TemUtils.GiveMoney(user, -loss, $"for failing to collect a single bounty ({StringHelpers.GetPowerColoredName(this)})");
            }
        }

        if (eventType == typeof(EventRoundStart))
            killsThisRound.Clear();

        return HookResult.Continue;
    }

    public override void OnRemoveUser(CCSPlayerController? player, bool reasonDisconnect)
    {
        base.OnRemoveUser(player, reasonDisconnect);
        if (player != null)
            killsThisRound.Remove(player.SteamID);
    }


    public override string GetDescriptionColored() =>
        "Gain " + StringHelpers.Green("+" + cfg_killBonus + "$") + " for every kill" +
        ", but lose " + StringHelpers.Red("-" + cfg_noKillPenalty + "$") + " if you end the round with none";

    public int cfg_killBonus = 300;
    public int cfg_noKillPenalty = 300;
}
