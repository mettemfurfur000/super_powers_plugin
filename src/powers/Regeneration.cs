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
        if (Server.TickCount % cfg_period != 0) return;

        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                continue;

            if (pawn.Health >= cfg_limit)
                continue;

            pawn.Health += cfg_increment;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
    }

    public override string GetDescription() => $"Regenerate {cfg_increment} Health if less than {cfg_limit} every {(float)(cfg_period / 64)} seconds";
    public override string GetDescriptionColored() => "Regenerate " + StringHelpers.Green(cfg_increment) + " Health if less than " + StringHelpers.Blue(cfg_limit) + " every " + StringHelpers.Blue(cfg_period / 64) + " seconds";

    public int cfg_increment = 10;
    public int cfg_limit = 75;
    public int cfg_period = 128;
}

