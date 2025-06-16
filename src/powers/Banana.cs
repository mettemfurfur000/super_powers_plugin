using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Events;

namespace super_powers_plugin.src;

public class Banana : ISuperPower
{
    public Banana()
    {
        Triggers = [typeof(EventRoundStart)];
        NeededResources = ["models/food/fruits/banana01a.vmdl"];

        setDisabled();
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        foreach (var user in Users)
        {
            if (user == null || !user.IsValid)
                continue;

            var pawn = user.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                continue;

            var prop = Utilities.CreateEntityByName<CPhysicsPropMultiplayer>("prop_physics_multiplayer");
            if (prop == null)
                continue;

            var pizza_id = TemUtils.RandomString(12);

            prop.Globalname = pizza_id;
            prop.SetModel(NeededResources[0]);
            prop.Teleport(pawn.AbsOrigin, pawn.AbsRotation, pawn.AbsVelocity);

            if (prop.Collision != null && prop.Collision.EnablePhysics != 1)
                prop.Collision.EnablePhysics = 1;

            if (prop.Collision != null)
            {
                prop.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
                prop.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_PROPS;
            }

            prop.AddEntityIOEvent("SetScale", null, null, "5");
            prop.DispatchSpawn();
        }
        return HookResult.Continue;
    }

    public override string GetDescription() => $"Spawns a banana each round, not edible";
}

