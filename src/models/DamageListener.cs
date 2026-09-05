using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using super_powers_plugin.src;

namespace super_powers_plugin.src;

public class DamageContext
{
    public CCSPlayerController? Victim;
    public CCSPlayerController? Attacker;
    public CCSWeaponBase? Weapon;
    public CTakeDamageInfo Info;
    public bool Prevented = false;

    public float Damage
    {
        get => Info.Damage;
        set => Info.Damage = value;
    }
}

public static class DamageListener
{
    private static HookResult OnTakeDamage(CBaseEntity entity, CTakeDamageInfo info)
    {
        var victimPawn = entity.As<CCSPlayerPawn>();
        if (victimPawn == null)
            return HookResult.Continue;

        if (victimPawn.Controller == null || !victimPawn.Controller.IsValid || victimPawn.Controller.Value == null)
            return HookResult.Continue;

        var victimController = victimPawn.Controller.Value.As<CCSPlayerController>();
        if (victimController == null || !victimController.IsValid)
            return HookResult.Continue;
        
        // Server.PrintToChatAll($"damage event: {victimController.PlayerName} took {info.Damage:F1} damage from {info.Attacker?.Value?.DesignerName ?? "null"}");

        CCSWeaponBase? weapon = null;

        var attackerHandle = info.Attacker;
        if (attackerHandle == null || !attackerHandle.IsValid || attackerHandle.Value == null)
            return HookResult.Continue;

        var attackerPawn = attackerHandle.Value.As<CCSPlayerPawn>();
        if (attackerPawn == null || !attackerPawn.IsValid || attackerPawn.Controller == null || !attackerPawn.Controller.IsValid || attackerPawn.Controller.Value == null)
            return HookResult.Continue;

        CCSPlayerController? attacker = attackerPawn.Controller.Value.As<CCSPlayerController>();

        if (attackerPawn.WeaponServices == null)
            return HookResult.Continue;

        var baseWeapon = attackerPawn.WeaponServices.ActiveWeapon.Value;
        if (baseWeapon == null || !baseWeapon.IsValid)
            return HookResult.Continue;
        
        var realWeapon = WeaponModifierManager.AsWeaponBase(baseWeapon);

        var ctx = new DamageContext
        {
            Victim = victimController,
            Attacker = attacker,
            Weapon = realWeapon,
            Info = info
        };

        // Server.PrintToChatAll($"damage event: {ctx.Attacker?.PlayerName ?? "null"} hit {ctx.Victim?.PlayerName ?? "null"} with {ctx.Weapon?.VData?.Name ?? "null"} for {ctx.Damage} damage (headshot: {ctx.Info.HitGroupId == HitGroup_t.HITGROUP_HEAD})");

        SuperPowerController.ExecuteDamageHooks(ctx);
        WeaponModifierManager.ExecuteDamageHooks(ctx);

        if (ctx.Prevented)
            return HookResult.Handled;

        return HookResult.Continue;
    }

    public static void Register(BasePlugin plugin)
    {
        plugin.RegisterListener<Listeners.OnEntityTakeDamagePre>(OnTakeDamage);
    }
}
