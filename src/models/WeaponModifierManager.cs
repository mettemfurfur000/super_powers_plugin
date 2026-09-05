using System.Reflection;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace super_powers_plugin.src;

public static class WeaponModifierManager
{
    private static List<BaseWeaponModifier> Modifiers = [];
    private static Dictionary<ulong, List<BaseWeaponModifier>> WeaponModifiers = [];
    private static Dictionary<int, ulong> EntityToWeaponId = [];
    private static ulong NextWeaponId = 1;

    static WeaponModifierManager()
    {
        Modifiers.Add(new ArmorPiercingModifier());
        Modifiers.Add(new BiocodedWeaponModifier());
        Modifiers.Add(new InfiniteAmmoWeaponModifier());
    }

    public static IEnumerable<BaseWeaponModifier> SelectModifiers(string pattern)
    {
        return ModifierConfigHelper.SelectByName(Modifiers, pattern, StringHelpers.GetWeaponModifierName);
    }

    public static HashSet<BaseWeaponModifier> GetModifiers()
    {
        return Modifiers.ToHashSet();
    }

    public static List<string> GetModifierList()
    {
        List<string> list = [];
        foreach (var m in Modifiers)
            list.Add(StringHelpers.GetWeaponModifierName(m));
        return list;
    }

    public static CCSWeaponBase? AsWeaponBase(CBasePlayerWeapon? weapon)
    {
        if (weapon == null || !weapon.IsValid)
            return null;
 
        return weapon.As<CCSWeaponBase>();
    }

    public static ulong EnsureWeaponTracked(CCSWeaponBase weapon)
    {
        var item = weapon.AttributeManager.Item;

        if (!string.IsNullOrEmpty(item.CustomName)
            && ulong.TryParse(item.CustomName, out var existingId))
        {
            return existingId;
        }

        if (!string.IsNullOrEmpty(item.CustomName)
            && string.IsNullOrEmpty(item.CustomNameOverride))
        {
            item.CustomNameOverride = item.CustomName;
        }

        var newId = NextWeaponId++;
        item.CustomName = newId.ToString();
        Utilities.SetStateChanged(weapon, "CEconEntity", "m_AttributeManager");

        return newId;
    }

    public static void RebuildEntityMap()
    {
        EntityToWeaponId.Clear();

        foreach (var player in Utilities.GetPlayers())
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn?.WeaponServices == null) continue;

            var activeWeapon = AsWeaponBase(pawn.WeaponServices.ActiveWeapon.Value);
            if (activeWeapon != null && activeWeapon.IsValid)
            {
                var id = EnsureWeaponTracked(activeWeapon);
                EntityToWeaponId[(int)activeWeapon.Index] = id;
            }

            foreach (var handle in pawn.WeaponServices.MyWeapons)
            {
                var weapon = AsWeaponBase(handle.Value);
                if (weapon != null && weapon.IsValid)
                {
                    var id = EnsureWeaponTracked(weapon);
                    EntityToWeaponId[(int)weapon.Index] = id;
                }
            }
        }
    }

    public static void Update()
    {
        if (Server.TickCount % 32 == 0)
            RebuildEntityMap();

        foreach (var player in Utilities.GetPlayers())
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn?.WeaponServices == null) continue;

            var activeWeapon = AsWeaponBase(pawn.WeaponServices.ActiveWeapon.Value);
            if (activeWeapon != null && activeWeapon.IsValid
                && EntityToWeaponId.TryGetValue((int)activeWeapon.Index, out var trackingId)
                && WeaponModifiers.TryGetValue(trackingId, out var modifiers))
            {
                foreach (var mod in modifiers)
                    mod.Update(activeWeapon, player);
            }
        }
    }

    public static HookResult ExecuteModifier(GameEvent gameEvent)
    {
        HookResult ret = HookResult.Continue;
        Type type = gameEvent.GetType();

        CCSPlayerController? player = null;

        if (gameEvent is EventWeaponFire wf)
            player = wf.Userid;
        else if (gameEvent is EventPlayerHurt ph)
            player = ph.Attacker;
        else if (gameEvent is EventBulletImpact bi)
            player = bi.Userid;
        else if (gameEvent is EventItemPickup ip)
            player = ip.Userid;
        else if (gameEvent is EventItemEquip ie)
            player = ie.Userid;
        else if (gameEvent is EventWeaponReload wr)
            player = wr.Userid;

        if (player == null || !player.IsValid || player.PlayerPawn.Value?.WeaponServices == null)
            return ret;

        var pawn = player.PlayerPawn.Value;
        var activeWeapon = AsWeaponBase(pawn.WeaponServices.ActiveWeapon.Value);
        if (activeWeapon == null || !activeWeapon.IsValid)
            return ret;

        if (!EntityToWeaponId.TryGetValue((int)activeWeapon.Index, out var trackingId))
        {
            trackingId = EnsureWeaponTracked(activeWeapon);
            EntityToWeaponId[(int)activeWeapon.Index] = trackingId;
        }

        if (!WeaponModifiers.TryGetValue(trackingId, out var activeModifiers))
            return ret;

        foreach (var mod in activeModifiers)
        {
            if (mod.Triggers.Contains(type))
            {
                var ctx = new WeaponModifierContext
                {
                    Wielder = player,
                    Weapon = activeWeapon,
                    WeaponKey = trackingId,
                    AllModifiers = activeModifiers
                };

                if (mod.Execute(gameEvent, ctx) == HookResult.Stop)
                    ret = HookResult.Stop;
            }
        }

        return ret;
    }

    public static string ApplyModifier(CCSPlayerController player, string modifierName)
    {
        var modifiers = SelectModifiers(modifierName);
        if (!modifiers.Any())
            return "Error: Modifier not found";

        var pawn = player.PlayerPawn.Value;

        if (pawn == null || !pawn.IsValid)
            return "Error: Player pawn is invalid";

        if (pawn.WeaponServices == null)
            return "Error: Player has no weapon services";

        var active_weapon = pawn.WeaponServices.ActiveWeapon;

        if (active_weapon == null || !active_weapon.IsValid)
            return "Error: active_weapon is invalid";

        var weapon = AsWeaponBase(active_weapon.Value);
        if (weapon == null || !weapon.IsValid)
        {
            return "Error: failed to cast active_weapon to CCSWeaponBase";
        }

        string result = "";
        foreach (var mod in modifiers)
        {
            if (mod.OnApply(weapon, player) == false)
            {
                result += $"Failed to apply {mod.Name} to weapon\n";
                continue;
            }

            var trackingId = EnsureWeaponTracked(weapon);
            EntityToWeaponId[(int)weapon.Index] = trackingId;

            if (!WeaponModifiers.ContainsKey(trackingId))
                WeaponModifiers[trackingId] = [];

            if (!WeaponModifiers[trackingId].Contains(mod))
            {
                WeaponModifiers[trackingId].Add(mod);
                result += $"Applied {mod.Name} to weapon\n";
            }
            else
            {
                result += $"Weapon already has {mod.Name}\n";
            }
        }

        return result;
    }

    public static string ApplyModifierToWeapon(CCSPlayerController player, BaseWeaponModifier mod)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn?.WeaponServices == null)
            return "Error: Player has no weapon services";

        var weapon = AsWeaponBase(pawn.WeaponServices.ActiveWeapon.Value);
        if (weapon == null || !weapon.IsValid)
            return "Error: failed to cast active_weapon to CCSWeaponBase";

        if (mod.OnApply(weapon, player) == false)
            return $"Failed to apply {mod.Name} to weapon";

        var trackingId = EnsureWeaponTracked(weapon);
        EntityToWeaponId[(int)weapon.Index] = trackingId;

        if (!WeaponModifiers.ContainsKey(trackingId))
            WeaponModifiers[trackingId] = [];

        if (!WeaponModifiers[trackingId].Contains(mod))
            WeaponModifiers[trackingId].Add(mod);

        return $"Applied {mod.Name} to weapon";
    }

    public static string RemoveModifier(CCSPlayerController player, string modifierName)
    {
        var modifiers = SelectModifiers(modifierName);
        if (!modifiers.Any())
            return "Error: Modifier not found";

        var pawn = player.PlayerPawn.Value;
        if (pawn?.WeaponServices == null)
            return "Error: Player has no weapon services";

        var weapon = AsWeaponBase(pawn.WeaponServices.ActiveWeapon.Value);
        if (weapon == null || !weapon.IsValid)
            return "Error: failed to cast active_weapon to CCSWeaponBase";

        if (!EntityToWeaponId.TryGetValue((int)weapon.Index, out var trackingId))
            return "Error: Weapon not tracked";

        if (!WeaponModifiers.TryGetValue(trackingId, out var activeModifiers))
            return "Error: Weapon has no modifiers";

        string result = "";
        foreach (var mod in modifiers)
        {
            if (activeModifiers.Contains(mod))
            {
                activeModifiers.Remove(mod);
                mod.OnRemove(weapon, player);
                result += $"Removed {mod.Name} from weapon\n";
            }
            else
            {
                result += $"Weapon does not have {mod.Name}\n";
            }
        }

        if (activeModifiers.Count == 0)
            WeaponModifiers.Remove(trackingId);

        return result;
    }

    public static string GetModifiersStatus()
    {
        string output = "Weapon Modifiers Status:\n";

        if (WeaponModifiers.Count == 0)
        {
            output += "  No modifiers applied\n";
            return output;
        }

        foreach (var kvp in WeaponModifiers)
        {
            var trackingId = kvp.Key;
            var mods = kvp.Value;

            if (mods.Count == 0) continue;

            output += $"  Weapon [{trackingId}]: ";
            foreach (var mod in mods)
                output += $"{mod.Name} ";
            output += "\n";
        }

        return output;
    }

    public static string GetModifiersStatusForPlayer(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn?.WeaponServices == null)
            return "Error: Player has no weapon services";

        var active_weapon = pawn.WeaponServices.ActiveWeapon;

        if (active_weapon == null || !active_weapon.IsValid)
            return "Error: Player has no active weapon";

        var weapon = AsWeaponBase(active_weapon.Value);
        if (weapon == null || !weapon.IsValid)
        {
            return "Error: failed to cast active_weapon to CCSWeaponBase";
        }

        if (!EntityToWeaponId.TryGetValue((int)weapon.Index, out var trackingId))
            return "Error: Weapon not tracked";

        if (!WeaponModifiers.TryGetValue(trackingId, out var activeModifiers))
            return "No modifiers applied to this weapon";

        string output = $"Modifiers for Weapon [{trackingId}]: ";
        foreach (var mod in activeModifiers)
            output += $"{mod.Name} ";
        output += "\n";

        return output;
    }

    public static void FeedConfig(WeaponModifierConfig cfg)
    {
        ModifierConfigHelper.FeedConfig(cfg.Modifiers, Modifiers, StringHelpers.GetWeaponModifierName, (mod, modCfg) => mod.ParseCfg(modCfg));
    }

    public static void Reconfigure(Dictionary<string, string> configuration, string modifierNamePattern)
    {
        ModifierConfigHelper.Reconfigure(configuration, modifierNamePattern, Modifiers, StringHelpers.GetWeaponModifierName, (mod, cfg) => mod.ParseCfg(cfg));
    }

    public static Dictionary<string, Dictionary<string, string>> GenerateDefaultConfig()
    {
        return ModifierConfigHelper.GenerateDefaultConfig(Modifiers, StringHelpers.GetWeaponModifierName);
    }

    public static BindingFlags fieldFlags = ModifierConfigHelper.FieldFlags;
    public static readonly string prefix = ModifierConfigHelper.Prefix;

    public static void ParseConfig(BaseWeaponModifier modifier, Type iter_type, Dictionary<string, string> cfg_unresolved)
    {
        ModifierConfigHelper.ParseConfig(modifier, iter_type, cfg_unresolved);
    }

    public static BaseWeaponModifier? GetRandomModifier()
    {
        int totalWeight = Modifiers.Sum(m => m.weight);
        if (totalWeight <= 0) return null;

        var random = new Random();
        int roll = random.Next(totalWeight);

        foreach (var mod in Modifiers)
        {
            roll -= mod.weight;
            if (roll < 0)
                return mod;
        }

        return null;
    }

    public static string? InspectModifierReflective(BaseWeaponModifier modifier, Type type)
    {
        return ModifierConfigHelper.InspectReflective(modifier, type);
    }

    public static void ExecuteDamageHooks(DamageContext ctx)
    {
        if (ctx.Attacker == null || ctx.Weapon == null)
            return;

        if (!EntityToWeaponId.TryGetValue((int)ctx.Weapon.Index, out var trackingId))
            return;

        if (!WeaponModifiers.TryGetValue(trackingId, out var modifiers))
            return;

        foreach (var mod in modifiers)
            mod.OnTakeDamage(ctx);
    }
}
