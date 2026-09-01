using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

using super_powers_plugin.src;
using super_powers_plugin.src.hud;

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


    public override string GetDescriptionColored() => "Regenerate " + StringHelpers.Green(cfg_increment) + " Health if less than " + StringHelpers.Blue(cfg_limit) + " every " + StringHelpers.Blue(cfg_period / 64) + " seconds";

    public int cfg_increment = 10;
    public int cfg_limit = 75;
    public int cfg_period = 128;

    public override HudState GetHudState(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        int hp = pawn?.Health ?? 0;
        float bar = Math.Clamp((float)hp / cfg_limit, 0f, 1f);

        string info = hp >= cfg_limit
            ? $"HP {hp} | at cap"
            : $"+{cfg_increment}hp every {cfg_period / 64}s | cap {cfg_limit}";

        return new HudState
        {
            Name = Name.ToUpperInvariant(),
            Bar = $"HP {hp}/{cfg_limit} {SuperPowerHudManager.BuildBar(bar)}",
            Info = info,
            ShowButton = false,
            ActionText = "",
            Rarity = Rarity,
            IsActive = true
        };
    }
}

