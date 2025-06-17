using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class SmallSize : BasePower
{
    public SmallSize() => Triggers = [typeof(EventRoundStart)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        Users.ForEach(user =>
        {
            SetScale(user, scale);
        });
        return HookResult.Continue;
    }

    public override void OnRemovePower(CCSPlayerController? player)
    {
        if (player != null)
            SetScale(player, 1);
        else
            Users.ForEach(user =>
            {
                SetScale(user, 1);
            });
    }
    public override string GetDescription() => $"WIP: smaller model, but camera on the same height";

    public void SetScale(CCSPlayerController? player, float value = 1)
    {
        if (player == null)
            return;
        var pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return;

        var skeletonInstance = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance();
        if (skeletonInstance != null)
            skeletonInstance.Scale = value;

        pawn.AcceptInput("SetScale", null, null, value.ToString());

        Server.NextFrame(() =>
        {
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
        });

        // pawn.ViewOffset.X = 100;
        // Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_vecViewOffset");
        // Server.NextFrame(() =>
        // {
        //     pawn.ViewOffset.X = 100;
        //     Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_vecViewOffset");
        // });
    }

    public override void Update()
    {
        Users.ForEach(user =>
        {
            SetScale(user, scale);
        });

        if (Server.TickCount % 64 != 0)
            return;

        Users.ForEach(user =>
        {
            user.PrintToChat(user.PlayerPawn.Value!.ViewOffset.Z + "");
        });
    }

    private float scale = 0.5f;
}

