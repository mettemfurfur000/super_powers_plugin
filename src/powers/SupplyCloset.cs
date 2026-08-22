using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

using super_powers_plugin.src;

public class SupplyCloset : BasePower
{
    public SupplyCloset()
    {
        Triggers = [];
        Price = 6000;
        Rarity = "Rare";
    }

    public override bool SupportsLeveling => true;

    public override void Update()
    {
        if (Server.TickCount % cfg_periodTicks != 0)
            return;

        var traceOptions = new TraceOptions
        {
            InteractsAs = Contents.Solid,
            InteractsWith = Contents.Solid | Contents.Window, // only world geometry can block the sight
            InteractsExclude = Contents.Player // pawns never occlude
        };

        var players = Utilities.GetPlayers();
        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                continue;

            var eyePos = pawn.GetEyePosition();
            if (eyePos == null)
                continue;

            bool upgraded = GetLevel(user) >= 1;
            bool suppliedSomeone = false;

            foreach (var teammate in players)
            {
                if (!cfg_includeSelf && teammate.UserId == user.UserId)
                    continue;
                if (teammate.TeamNum != user.TeamNum || teammate.TeamNum == 0)
                    continue;

                var teammatePawn = teammate.PlayerPawn.Value;
                if (teammatePawn == null || !teammatePawn.IsValid || teammatePawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                    continue;

                var teammateEyePos = teammatePawn.GetEyePosition();
                if (teammateEyePos == null || eyePos.Distance(teammateEyePos) > cfg_range)
                    continue;

                // end the ray just outside the teammate's hull, a segment terminating
                // inside a body may register as a hit even with clear sight
                var deltaX = teammateEyePos.X - eyePos.X;
                var deltaY = teammateEyePos.Y - eyePos.Y;
                var deltaZ = teammateEyePos.Z - eyePos.Z;
                float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);

                if (distance >= cfg_traceMargin * 2f)
                {
                    float scale = (distance - cfg_traceMargin) / distance;
                    var endPoint = new Vector(eyePos.X + deltaX * scale, eyePos.Y + deltaY * scale, eyePos.Z + deltaZ * scale);

                    // world geometry in between - not visible, nobody is supplied
                    var traceResult = Trace.TraceEndShape(eyePos, endPoint, pawn, traceOptions);
                    if (traceResult.DidHit())
                        continue;
                }

                int newArmor = Math.Min(cfg_armorCap, teammatePawn.ArmorValue + cfg_armorPerSecond);
                bool reachedCap = newArmor >= cfg_armorCap;

                if (teammatePawn.ArmorValue != newArmor)
                {
                    teammatePawn.ArmorValue = newArmor;
                    Utilities.SetStateChanged(teammatePawn, "CCSPlayerPawn", "m_ArmorValue");
                    suppliedSomeone = true;
                }

                // upgraded: once a teammate is fully stocked, hand out a helmet too
                var itemServices = new CCSPlayer_ItemServices(teammatePawn.ItemServices!.Handle);
                if (upgraded && reachedCap && !itemServices.HasHelmet)
                {
                    itemServices.HasHelmet = true;
                    Utilities.SetStateChanged(teammatePawn, "CCSPlayer_ItemServices", "m_bHasHelmet");
                    suppliedSomeone = true;
                }
            }

            if (suppliedSomeone)
                GrantXP(user, cfg_xpPerTrigger);
        }
    }


    public override string GetDescriptionColored() =>
        "Teammates in your line of sight gain " + StringHelpers.Green(cfg_armorPerSecond) + " armor every second," +
        " up to " + StringHelpers.Blue(cfg_armorCap) +
        ". Upgraded: at full armor they get a " + StringHelpers.Green("helmet");

    public int cfg_armorPerSecond = 2;
    public int cfg_armorCap = 100;
    public float cfg_range = 3072f;
    public bool cfg_includeSelf = true;
    public int cfg_periodTicks = 64;
    public float cfg_traceMargin = 16f;
}
