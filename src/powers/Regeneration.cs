using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

using super_powers_plugin.src;

public class Regeneration : BasePower
{
    public Regeneration()
    {
        Triggers = [typeof(EventRoundStart)];
        Price = 5000;
        Rarity = "Uncommon";
    }

    public override void Update()
    {
        if (Server.TickCount % period != 0) return;

        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                continue;

            if (pawn.Health >= limit)
                continue;

            pawn.Health += increment;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
    }

    public override string GetDescription() => $"Regenerate {increment} Health if less than {limit} every {(float)(period / 64)} seconds";
    public override string GetDescriptionColored() => "Regenerate " + NiceText.Green(increment) + " Health if less than " + NiceText.Blue(limit) + " every " + NiceText.Blue(period / 64) + " seconds";

    private int increment = 10;
    private int limit = 75;
    private int period = 128;
}

