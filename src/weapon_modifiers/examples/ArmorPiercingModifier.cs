using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using super_powers_plugin.src;

public class ArmorPiercingModifier : BaseWeaponModifier
{
    public ArmorPiercingModifier()
    {
        Triggers = [typeof(EventWeaponFire)];
    }

    public override HookResult Execute(GameEvent gameEvent, WeaponModifierContext ctx)
    {
        if (gameEvent is EventWeaponFire fireEvent)
        {
            var shooter  = fireEvent.Userid;
            if (shooter == null || !shooter.IsValid)
                return HookResult.Continue;
            
            string weapon = fireEvent.Weapon;
            // Roll for jam chance
            var random = new Random();
            var roll = random.NextDouble() * 100.0;
            if (roll < cfg_jamProbability)
            {
                // Jam the weapon
                ctx.Weapon.Clip1 = 0;
                Utilities.SetStateChanged(ctx.Weapon, "CBasePlayerWeapon", "m_iClip1");
                // Optionally notify the player
                shooter.PrintToggleable($"WIP: Your {weapon} has jammed!");
            }
        }
        return HookResult.Continue;
    }

    public override void OnTakeDamage(DamageContext ctx)
    {
        // Server.PrintToChatAll($"called damage taker");

        if (ctx.Weapon == null || !ctx.Weapon.IsValid || ctx.Weapon.VData == null)
            return;

        if (ctx.Attacker == null || !ctx.Attacker.IsValid)
            return;

        if (ctx.Victim == null || !ctx.Victim.IsValid)
            return;

        if (cfg_headshotOnly && ctx.Info.HitGroupId != HitGroup_t.HITGROUP_HEAD)
            return;

        // Server.PrintToChatAll($"WIP: {ctx.Attacker.PlayerName} hit {ctx.Victim.PlayerName} with {ctx.Weapon.VData.Name} for {ctx.Damage} damage (headshot: {ctx.Info.HitGroupId == HitGroup_t.HITGROUP_HEAD})");

        // Apply damage multiplier
        ctx.Damage *= cfg_damageMultiplier;

    }

    public override string GetDescriptionColored() =>
        $"Deals {StringHelpers.Green("2x")} damage but has a {StringHelpers.Red("10%")} chance to jam";

    public float cfg_damageMultiplier = 2.0f;
    public float cfg_jamProbability = 10.0f;
    public bool cfg_headshotOnly = false;
}
