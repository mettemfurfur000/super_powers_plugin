using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace super_powers_plugin.src;

public class Builder : ISuperPower
{
    public Builder()
    {
        Triggers = [];
        NeededResources = ["models/props/de_dust/stoneblocks48.vmdl_c"];
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        return HookResult.Continue;
    }

    // stolen https://github.com/Kandru/cs2-roll-the-dice/blob/main/src/RollTheDice%2BDiceNoExplosives.cs#L204

    private uint CreatePhysicsModel(Vector origin, QAngle angles, Vector velocity)
    {
        CDynamicProp prop = Utilities.CreateEntityByName<CDynamicProp>("prop_physics_override")!;

        prop.Health = 10;
        prop.MaxHealth = 10;

        prop.DispatchSpawn();
        // var randomModel = _explosiveModels[new Random().Next(_explosiveModels.Count)];
        // prop.SetModel(randomModel.Model);
        // prop.CBodyComponent!.SceneNode!.Scale = randomModel.Scale;
        // prop.Teleport(origin, angles, velocity);
        // prop.AnimGraphUpdateEnabled = false;
        return prop.Index;
    }

    public override string GetDescription() => $"build";

    // public List<string> NeededResources { get; set; } = ["models/props/de_dust/stoneblocks48.vmdl_c"];
}

