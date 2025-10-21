using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class Vampirism : BasePower
{
    public Vampirism()
    {
        Triggers = [typeof(EventPlayerHurt)];
        Price = 8000;
        Rarity = "Legendary";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() != Triggers[0])
            return HookResult.Continue;

        var realEvent = (EventPlayerHurt)gameEvent;
        var attacker = realEvent.Attacker;
        var victim = realEvent.Userid;

        if (attacker == null || !attacker.IsValid)
            return HookResult.Continue;

        if (!Users.Where(p => p.UserId == attacker.UserId).Any())
            return HookResult.Continue;

        var attackerPawn = attacker.PlayerPawn.Value;
        if (attackerPawn == null || !attackerPawn.IsValid)
            return HookResult.Continue;

        attackerPawn.Health = attackerPawn.Health + realEvent.DmgHealth / divisor;

        Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");
        //Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

        var sounds = vampireSounds.Split(";");
        if (playSounds)
            attacker.ExecuteClientCommand("play " + sounds.ElementAt(new Random().Next(sounds.Length)));
        // TemUtils.EmitSound(attacker,) // dosn work aparently, requires source2viewer to get exact sound thing name
        if (playSounds)
            if (victim != null && victim.IsValid)
                victim.ExecuteClientCommand("play " + sounds.ElementAt(new Random().Next(sounds.Length)));

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Gain {(int)(100 / divisor)}% of dealt damage as extra health" + (playSounds ? ", annoying sounds included" : "");
    public override string GetDescriptionColored() => "Gain " + StringHelpers.Green((int)(100 / divisor)) + "% of dealt damage as extra health" + (playSounds ? ", annoying sounds included" : "");

    private int divisor = 5;
    private bool playSounds = false;
    private string vampireSounds = "sounds/physics/flesh/flesh_squishy_impact_hard4.vsnd;sounds/physics/flesh/flesh_squishy_impact_hard3.vsnd;sounds/physics/flesh/flesh_squishy_impact_hard2.vsnd;sounds/physics/flesh/flesh_squishy_impact_hard1.vsnd";
}

