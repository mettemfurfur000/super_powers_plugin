using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace super_powers_plugin.src;

public class Snowballing : ISuperPower
{
    public Snowballing() => Triggers = [typeof(EventPlayerDeath), typeof(EventRoundStart), typeof(EventPlayerHurt)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        Type type = gameEvent.GetType();

        if (type == typeof(EventPlayerDeath) && giveHealImmediately) // each  time he kills someone, he gets a heal bonus
        {
            EventPlayerDeath realEvent = (EventPlayerDeath)gameEvent;

            var player = realEvent.Attacker;
            if (player == null)
                return HookResult.Continue;

            if (!Users.Contains(player))
                return HookResult.Continue;

            var pawn = player.PlayerPawn.Value!;
            pawn.Health = pawn.Health + heal;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
        else if (type == typeof(EventPlayerHurt))
        {
            EventPlayerHurt realEvent = (EventPlayerHurt)gameEvent;

            var player = realEvent.Attacker!;

            if (!Users.Contains(player))
                return HookResult.Continue;

            // match stats contain kills thru de whol game, killCount only contains kills for this round
            float damage_uncapped_bonus = (resetOnRoundStart == false ? player.ActionTrackingServices!.MatchStats.Kills : player.KillCount) * dmg_inc;

            float damage_mult_bonus = max_dmg_inc > 0 ? Math.Min(damage_uncapped_bonus, max_dmg_inc) : damage_uncapped_bonus;
            int bonus_damage = (int)(realEvent.DmgHealth * damage_mult_bonus);

            var pawn = realEvent.Userid!.PlayerPawn.Value!;
            pawn.Health = pawn.Health - bonus_damage;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
        else if (type == typeof(EventRoundStart) && giveHealOnSpawn && resetOnRoundStart == false)
        {
            Users.ForEach(user =>
            {
                var pawn = user.PlayerPawn.Value!;
                pawn.Health = pawn.Health + Math.Min(user.ActionTrackingServices!.MatchStats.Kills * heal, max_heal);
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            });
        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Each kill will give you {heal} more HP and {dmg_inc * 100}% more damage. Limited to {max_heal} HP and {max_dmg_inc * 100}% bonus damage";

    private int heal = 25;
    private float dmg_inc = 0.1f;

    private int max_heal = 300;
    private float max_dmg_inc = 1.00f;

    private bool resetOnRoundStart = true;
    private bool giveHealImmediately = true;
    private bool giveHealOnSpawn = false;
}

