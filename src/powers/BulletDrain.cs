using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class BulletDrain : BasePower
{
    public BulletDrain()
    {
        Triggers = [typeof(EventPlayerHurt)];
        Price = 6500;
        Rarity = "Rare";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() != Triggers[0])
            return HookResult.Continue;

        EventPlayerHurt realEvent = (EventPlayerHurt)gameEvent;
        var attacker = realEvent.Attacker;
        var victim = realEvent.Userid;

        if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(attacker))
            return HookResult.Continue;

        if (attacker.UserId == victim.UserId || attacker.TeamNum == victim.TeamNum)
            return HookResult.Continue;

        var victimWeapon = victim.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (victimWeapon == null || !victimWeapon.IsValid)
            return HookResult.Continue;

        // only guns with an actual magazine can be drained
        if (victimWeapon.VData == null || victimWeapon.VData.MaxClip1 <= 0)
            return HookResult.Continue;

        if (victimWeapon.Clip1 <= 0)
            return HookResult.Continue;

        if (Random.Shared.NextSingle() >= cfg_chancePercent / 100f)
            return HookResult.Continue;

        victimWeapon.Clip1 -= 1;
        Utilities.SetStateChanged(victimWeapon, "CBasePlayerWeapon", "m_iClip1");

        return HookResult.Continue;
    }


    public override string GetDescriptionColored() =>
        StringHelpers.Blue(cfg_chancePercent.ToString() + "%") + " chance to remove a bullet from the enemy's magazine when you hit them";

    public int cfg_chancePercent = 50;
}
