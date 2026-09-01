using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;
using super_powers_plugin.src.hud;

public class GoldenBullet : BasePower
{
    public GoldenBullet()
    {
        Triggers = [typeof(EventPlayerDeath)];
        Price = 4000;
        Rarity = "Uncommon";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() != Triggers[0])
            return HookResult.Continue;

        EventPlayerDeath realEvent = (EventPlayerDeath)gameEvent;

        var shooter = realEvent.Attacker!;

        if (!Users.Contains(shooter))
            return HookResult.Continue;

        var weapon = shooter.PlayerPawn.Value!.WeaponServices!.ActiveWeapon;

        if (weapon.Value!.Clip1 == 1)
        {
            float timesMult = 1.0f;
            
            if (cfg_headshotMultEnabled && realEvent.Headshot)
            {
                                timesMult = cfg_multOnHeadshot;
            }

            TemUtils.GiveMoney(shooter, (int)(cfg_killReward * timesMult), $"for killing an enemy with a last bullet ({StringHelpers.GetPowerColoredName(this)})");
        }

        return HookResult.Continue;
    }



    public override string GetDescriptionColored() => "Kill with a last bullet gives you $" + StringHelpers.Green(cfg_killReward) + (cfg_headshotMultEnabled ? $", X{cfg_multOnHeadshot} for headshots" : "");
    public int cfg_killReward = 3000;
    public float cfg_multOnHeadshot = 1.5f;
    public bool cfg_headshotMultEnabled = false;

    public override HudState GetHudState(CCSPlayerController player)
    {
        var weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        int clip = weapon?.Clip1 ?? 0;
        bool lastBullet = clip == 1;

        return new HudState
        {
            Name = Name.ToUpperInvariant(),
            Bar = lastBullet ? "[!] LAST BULLET" : "",
            Info = lastBullet
                ? $"kill = ${cfg_killReward}{(cfg_headshotMultEnabled ? $" | hs x{cfg_multOnHeadshot}" : "")}"
                : $"clip {clip} | last bullet kills = ${cfg_killReward}",
            ShowButton = false,
            ActionText = "",
            Rarity = Rarity,
            IsActive = true
        };
    }
}

