using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class Pacifism : BasePower
{
    public Pacifism()
    {
        Triggers = [typeof(EventPlayerHurt), typeof(EventRoundStart)];
        Price = 6000;
        Rarity = "Uncommon";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() == typeof(EventPlayerHurt))
        {
            var realEvent = (EventPlayerHurt)gameEvent;
            var victim = realEvent.Userid;
            var attacker = realEvent.Attacker;

            if (victim == null || !victim.IsValid || attacker == null || !attacker.IsValid)
                return HookResult.Continue;

            if (Users.Contains(attacker)) // if attacker is a user, reset its capabilities
            {
                // Server.PrintToChatAll("pacifism removed");
                status.Remove(attacker);

                attacker.PrintToChat("Pacifism removed");
            }

            if (Users.Contains(victim)) // check if victim might be le pacifist
            {
                // Server.PrintToChatAll("pacifism victom found");
                if (status.Contains(victim)) // effect is active, cancel all damage
                {
                    // Server.PrintToChatAll("damage sustained");

                    var victim_pawn = victim.PlayerPawn.Value;
                    if (victim_pawn == null || !victim_pawn.IsValid)
                        return HookResult.Continue;

                    victim_pawn.Health += realEvent.DmgHealth;
                    victim_pawn.ArmorValue += realEvent.DmgArmor;

                    Utilities.SetStateChanged(victim_pawn, "CBaseEntity", "m_iHealth");
                    Utilities.SetStateChanged(victim_pawn, "CCSPlayerPawn", "m_ArmorValue");
                }
            }
        }
        else
        {
            // Server.PrintToChatAll("Gained pacifism");
            // reset pacifism status
            status.Clear();
            status = [.. Users];

            Users.ForEach((c) => c.PrintToChat("Gained Pacifism"));

            // Server.PrintToChatAll(status.ToString());
        }

        return HookResult.Continue;
    }

    public override void OnRemovePower(CCSPlayerController? player)
    {
        if (player != null)
        {
            status.Remove(player);
            player.PrintToChat("Pacifism removed");
        }
        else
        {
            status.Clear();
            status.ForEach((c) => c.PrintToChat("Pacifism removed"));
        }
    }

    // public Dictionary<CCSPlayerController, bool> status;
    public List<CCSPlayerController> status = [];

    public override string GetDescription() => $"Gain invincibility until you deal damage first";
    public override string GetDescriptionColored() => "Gain " + NiceText.Blue("invincibility") + " until you deal damage first";
}

