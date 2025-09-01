using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class SmallSize : BasePower
{
    public SmallSize()
    {
        Triggers = [typeof(EventRoundStart)];
        // Price = 5000;
        // Rarity = "Uncommon";
        NoShop = true;
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (affectHullSizeAll)
            TemUtils.SetGlobalPlayerHull(scale);
        Users.ForEach(user => SetScale(user, scale));
        return HookResult.Continue;
    }

    public override void OnRemovePower(CCSPlayerController? player)
    {
        if (affectHullSizeAll)
            TemUtils.SetGlobalPlayerHull(1.0f);
        if (player != null)
            SetScale(player, 1);
        else
            Users.ForEach(user => SetScale(user, 1));
    }

    public override string GetDescription() => $"smaller model, same hull";

    public void SetScale(CCSPlayerController? player, float value = 1)
    {
        if (player == null)
            return;
        var pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return;

        pawn.AcceptInput("SetScale", null, null, value.ToString());
    }

    public override void Update()
    {
        if (Server.TickCount % 64 != 0)
            return;

        // if (affectHullSizeAll)
        //     TemUtils.SetGlobalPlayerHull(scale);
        Users.ForEach(user => SetScale(user, scale));
    }

    private float scale = 0.5f;
    private bool affectHullSizeAll = false;
}

