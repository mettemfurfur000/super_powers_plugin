using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Logging;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using super_powers_plugin.src;

public class ConcreteSmoke : BasePower
{
    public ConcreteSmoke() => Triggers = [typeof(EventSmokegrenadeDetonate)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        EventSmokegrenadeDetonate realEvent = (EventSmokegrenadeDetonate)gameEvent;

        var entity = Utilities.GetEntityFromIndex<CSmokeGrenadeProjectile>(realEvent.Entityid);

        if (entity != null)
        {
            TemUtils.__plugin!.AddTimer(1f, () => PrintVoxelData(entity));
        }

        return HookResult.Continue;
    }

    public void PrintVoxelData(CSmokeGrenadeProjectile entity)
    {
        string outstring = "";

        outstring += $"size {entity.VoxelFrameDataSize}\n";
        outstring += $"position {entity.InitialPosition.X} {entity.InitialPosition.Y} {entity.InitialPosition.Z}\n";

        TemUtils.Log("info: " + outstring);

        nint ptr = NativeAPI.GetNetworkVectorElementAt(entity.VoxelFrameData.Handle, 0);

        outstring = "";

        unsafe
        {
            byte* bptr = (byte*)ptr;
            for (int i = 0; i < entity.VoxelFrameDataSize; i++)
            {
                if (i % 32 == 0)
                {
                    TemUtils.Log(outstring);
                    outstring = "";
                }
                outstring += $"{bptr[i]:X2} ";
                // outstring += $"{((char)bptr[i])} ";
            }
            TemUtils.Log(outstring);
        }

    }

    // public static float Dot(Vector vector1, Vector vector2)
    // {
    //     return (vector1.X * vector2.X)
    //          + (vector1.Y * vector2.Y)
    //          + (vector1.Z * vector2.Z);
    // }

    // public static bool LineIntersectsSphere(Vector centre, float radius, Vector origin, Vector target)
    // {
    //     Vector direction = origin - target;
    //     // t satisfies a quadratic
    //     float a = Dot(direction, direction);
    //     float b = 2 * Dot(direction, origin - centre);
    //     float c = Dot(origin - centre, origin - centre) - radius * radius;
    //     float discriminant = b * b - 4 * a * c;

    //     if (discriminant < 0)
    //     {
    //         return false;
    //     }
    //     else if (discriminant > 0)
    //     {
    //         double t = (-b + Math.Sqrt(discriminant)) / (2 * a);
    //         double t2 = -b / a - t;
    //         if (Math.Abs(t2) < Math.Abs(t)) t = t2;
    //         // cout << "Closest intersection at " << origin + t * direction << '\n';
    //     }
    //     else
    //     {
    //         // cout << "Touches sphere at " << origin + (-0.5 * b / a) * direction << '\n';
    //     }
    // }


}

