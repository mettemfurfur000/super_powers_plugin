using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Microsoft.VisualBasic;
using super_powers_plugin.src;

public class Wallhacks : BasePower
{
    public Wallhacks()
    {
        Triggers = [];
        // NoShop = true;
        Rarity = "Legendary";
        Price = 8500;
        checkTransmitListenerEnabled = true;
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        return HookResult.Continue;
    }

    public override void OnRemovePower(CCSPlayerController? player)
    {
        // no change needed
    }

    public override string GetDescription() => $"smaller model, same hull";

    public override void Update()
    {
        if (Server.TickCount % 32 == 0)
        {
            StopGlowing(); // ther must be a better way
            GlowEveryone();
        }
    }

    private void StopGlowing()
    {
        foreach (KeyValuePair<CCSPlayerController, Tuple<CBaseModelEntity, CBaseModelEntity>> entry in models)
        {
            if (entry.Value.Item1.IsValid)
                entry.Value.Item1.Remove();
            if (entry.Value.Item2.IsValid)
                entry.Value.Item2.Remove();
        }

        models.Clear();

        // extra code to remove everything that glows like things we spawned

        Utilities.FindAllEntitiesByDesignerName<CBaseModelEntity>("prop_dynamic").ToList().ForEach((e) =>
        {
            if (e.Glow.GlowType == 3)
                e.Remove();
        });
    }

    private void GlowEveryone()
    {
        foreach (CCSPlayerController ctrl in Utilities.GetPlayers().Where(IsPlayerConnected))
            if (!models.ContainsKey(ctrl))
            {
                var ret = TemUtils.MakePawnGlow(ctrl.PlayerPawn.Value!, (byte)(ctrl.PlayerPawn.Value!.TeamNum == 2 ? 3 : 2));

                if (ret != null)
                    models.Add(ctrl, ret);
            }
    }

    public static bool IsPlayerConnected(CCSPlayerController player)
    {
        return player.Connected == PlayerConnectedState.PlayerConnected;
    }

    public override List<CBaseModelEntity>? GetHiddenEntities(CCSPlayerController player)
    {
        if (models.Count == 0)
            return null;

        // if the player is in our user list, he has wallhak and should wee the glow models
        if (Users.Contains(player)) // so we return nonin
            return null;

        // this will cause 64 heap allocations for each player on the server
        List<CBaseModelEntity> ret = [];

        foreach (KeyValuePair<CCSPlayerController, Tuple<CBaseModelEntity, CBaseModelEntity>> entry in models)
        {
            ret.Add(entry.Value.Item1);
            ret.Add(entry.Value.Item2);
        }

        return ret;
    }

    public Dictionary<CCSPlayerController, Tuple<CBaseModelEntity, CBaseModelEntity>> models = [];
}