using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using super_powers_plugin.src;

public class WeaponModifierContext
{
    public required CCSPlayerController Wielder;
    public required CCSWeaponBase Weapon;
    public ulong WeaponKey;
    public required List<BaseWeaponModifier> AllModifiers;
}

public abstract class BaseWeaponModifier
{
    public string Name => StringHelpers.GetWeaponModifierName(this);
    public string ColoredName => StringHelpers.GetWeaponModifierColoredName(this);

    public List<Type> Triggers = [];

    public int weight = 1;
    public bool noShop = false;
    public int cfg_price = 0;

    public virtual bool OnApply(CCSWeaponBase weapon, CCSPlayerController wielder) => true;
    public virtual void OnRemove(CCSWeaponBase weapon, CCSPlayerController wielder) { }
    public virtual HookResult Execute(GameEvent gameEvent, WeaponModifierContext ctx) => HookResult.Continue;
    public virtual void Update(CCSWeaponBase weapon, CCSPlayerController wielder) { }
    public virtual void OnPickup(CCSWeaponBase weapon, CCSPlayerController newWielder) { }
    public virtual void OnDrop(CCSWeaponBase weapon, CCSPlayerController previousWielder) { }
    public virtual void ParseCfg(Dictionary<string, string> cfg) =>
        WeaponModifierManager.ParseConfig(this, this.GetType(), cfg);
    public virtual string GetDescriptionColored() => "";
    public string GetDescriptionPlain() => StringHelpers.RemoveColorCodes(GetDescriptionColored());

    public virtual void OnTakeDamage(DamageContext ctx) { }
}
