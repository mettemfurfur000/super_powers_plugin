using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

using super_powers_plugin.src;

public class InstantPlant : BasePower
{
    public InstantPlant()
    {
        Triggers = [typeof(EventBombBeginplant)];
        teamReq = CsTeam.Terrorist;

        Price = 2500;
        Rarity = "Common";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventBombBeginplant)gameEvent;
        var player = realEvent.Userid;
        if (player != null && player.IsValid && player.PawnIsAlive)
        {
            if (!Users.Contains(player))
                return HookResult.Continue;

            var bomb = Utilities.FindAllEntitiesByDesignerName<CC4>("weapon_c4").ToList().FirstOrDefault();
            if (bomb == null)
            {
                return HookResult.Continue; ;
            }

            bomb.BombPlacedAnimation = false;
            bomb.ArmedTime = 0.0f;
        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Plant a bomb with no delay";
    public override string GetDescriptionColored() => $"Plant a " + NiceText.Red("bomb") + " with " + NiceText.Red("no delay");
}

