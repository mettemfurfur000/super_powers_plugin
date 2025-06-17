// using CounterStrikeSharp.API;
// using CounterStrikeSharp.API.Core;
// using CounterStrikeSharp.API.Modules.Events;

// using super_powers_plugin.src;

// public class Passport : BasePower
// {
//     public Passport() => Triggers = [typeof(EventRoundStart)];
//     public override HookResult Execute(GameEvent gameEvent)
//     {
//         foreach (var user in Users)
//         {
//             var pawn = user.PlayerPawn.Value;
//             if (pawn == null)
//                 continue;

//             pawn.ArmorValue = value;
//             Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

//         }
//         return HookResult.Continue;
//     }

//     public override string GetDescription() => $"Gain {health_mult}X health on the round start for each consecutive death from a headshot";

//     private float health_mult = 1.5f;
// }
