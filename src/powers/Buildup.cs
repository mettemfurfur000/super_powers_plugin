using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class Buildup : BasePower
{
    public Buildup()
    {
        Triggers = [typeof(EventGrenadeThrown)];
        Price = 3000;
        Rarity = "Common";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() != Triggers[0])
            return HookResult.Continue;

        EventGrenadeThrown realEvent = (EventGrenadeThrown)gameEvent;
        var user = realEvent.Userid;

        if (user == null || !user.IsValid || !Users.Contains(user))
            return HookResult.Continue;

        var pawn = user.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            return HookResult.Continue;

        pawn.ArmorValue = Math.Min(cfg_armorCap, pawn.ArmorValue + cfg_armorPerUtility);
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

        return HookResult.Continue;
    }


    public override string GetDescriptionColored() =>
        "Gain " + StringHelpers.Green(cfg_armorPerUtility) + " armor for every thrown utility" +
        ", capped at " + StringHelpers.Blue(cfg_armorCap);

    public int cfg_armorPerUtility = 16;
    public int cfg_armorCap = 100;
}
