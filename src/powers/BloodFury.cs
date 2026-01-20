using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

using super_powers_plugin.src;

public class BloodFury : BasePower
{
    public BloodFury()
    {
        Triggers = [typeof(EventPlayerDeath), typeof(EventPlayerHurt), typeof(EventRoundStart)];
        NeededResources = ["particles/survival_fx/gas_cannister_impact_child_explosion.vpcf"];
        Price = 7000;
        Rarity = "Rare";
    }

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent is EventRoundStart realEventStart) // trigger rage on player kills
        {
            ActivatedUsers.Clear();
            InvicibilityTicks.Clear();
        }

        if (gameEvent is EventPlayerDeath realEvent) // trigger rage on player kills
        {
            var player = realEvent.Attacker;
            if (player == null)
                return HookResult.Continue;

            if (!Users.Contains(player))
                return HookResult.Continue;

            if (!IsEnoughKills(player))
                return HookResult.Continue;

            if (ActivatedUsers.Contains(player))
                return HookResult.Continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null)
                return HookResult.Continue;

            {
                // main powerup section
                TemUtils.PowerApplySpeed(Users, cfg_SpeedModifier);

                TemUtils.CreateParticle(pawn.AbsOrigin!, NeededResources[0], 2, "Breakable.MatGlass", player: player);

                InvicibilityTicks.Add(Tuple.Create(player, (int)(Server.TickCount + (cfg_InvincibilitySeconds * 64))));
            }

            ActivatedUsers.Add(player);
        }

        if (gameEvent is EventPlayerHurt realEvent2) // deal more damage to players
        {
            var player = realEvent2.Attacker!;

            var victim = realEvent2.Userid!;

            if (InvicibilityTicks.Any(t => t.Item1 == victim && t.Item2 >= Server.TickCount))
            {
                var _pawn = victim.PlayerPawn.Value!;

                _pawn.Health = _pawn.Health + realEvent2.DmgHealth;
                Utilities.SetStateChanged(_pawn, "CBaseEntity", "m_iHealth");

                _pawn.ArmorValue += realEvent2.DmgArmor;
                Utilities.SetStateChanged(_pawn, "CCSPlayerPawn", "m_ArmorValue");
            }

            if (!Users.Contains(player))
                return HookResult.Continue;

            if (!IsEnoughKills(player))
                return HookResult.Continue;

            var pawn = realEvent2.Userid!.PlayerPawn.Value!;

            int bonus_damage = (int)(realEvent2.DmgHealth * cfg_DamageBonusMult) + cfg_DamageBonusFlat;

            pawn.Health = pawn.Health - bonus_damage;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }


        return HookResult.Continue;
    }



    public override void OnRemovePower(CCSPlayerController? player)
    {
        TemUtils.PowerRemoveSpeedModifier(Users, player);

        ActivatedUsers.Clear();
        InvicibilityTicks.Clear();
    }

    public override void Update()
    {
        InvicibilityTicks.RemoveAll(t => t.Item2 <= Server.TickCount); // remove expired ticks

        if (Server.TickCount % cfg_UpdatePeriod != 0)
            return;

        Users.ForEach(user =>
        {
            if (!IsEnoughKills(user))
                return;

            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                return;

            if (pawn.Health < 100)
            {
                pawn.Health += cfg_HealthRegenPerUpdate;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            }
        });

    }

    public bool IsEnoughKills(CCSPlayerController player)
    {
        var curKills = cfg_CountOnlyHeadshots ? player.ActionTrackingServices!.NumRoundKillsHeadshots : player.ActionTrackingServices!.NumRoundKills;
        return curKills >= cfg_KillsToRage;
    }

    public bool cfg_CountOnlyHeadshots = false;
    public int cfg_KillsToRage = 3;
    public int cfg_UpdatePeriod = 64;
    public int cfg_SpeedModifier = 450;
    public int cfg_DamageBonusFlat = 10;
    public double cfg_DamageBonusMult = 0.25d;
    public int cfg_HealthRegenPerUpdate = 1;
    public double cfg_InvincibilitySeconds = 1.5d;
    public override string GetDescriptionColored() => "After " + StringHelpers.Red(cfg_KillsToRage) + (cfg_CountOnlyHeadshots ? " Headshots" : " Kills") + ", gain speed, extra damage, and " + StringHelpers.Blue("temporary invincibility");
    public List<CCSPlayerController> ActivatedUsers = [];
    public List<Tuple<CCSPlayerController, int>> InvicibilityTicks = [];
}

