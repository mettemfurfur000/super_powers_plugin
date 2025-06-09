using System;
using System.Drawing;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using System.Threading;
using System.Linq;
using CounterStrikeSharp.API.Modules.Entities;
using System.Data.Common;
namespace super_powers_plugin.src;

/*

    public TemplatePower(Dictionary<string, Dictionary<string, string>> cfg)
    {
        internal_name = Utils.ToSnakeCase(this.GetType().ToString()).Split(".").Last();
    }

*/
/*
public class TemplatePower : ISuperPower
{
    public List<Type> () => Triggers = [typeof(EventRoundStart)];
    public override HookResult Execute(GameEvent gameEvent) { return HookResult.Continue; }

    
    int value = 404;
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
}
*/

public class BonusHealth : ISuperPower
{
    public BonusHealth() => Triggers = [typeof(EventRoundStart)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                continue;

            pawn.Health = value;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
        return HookResult.Continue;
    }

    public override string GetDescription() => $"+{value - 100} HP on the start of each round";
    private int value = 250;
}

public class Regeneration : ISuperPower
{
    public Regeneration() => Triggers = [typeof(EventRoundStart)];
    public override void Update()
    {
        if (Server.TickCount % period != 0) return;

        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                continue;

            if (pawn.Health >= limit)
                continue;

            pawn.Health += increment;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
    }

    public override string GetDescription() => $"Regenerate {increment} HP if less than {limit} every {(float)(period / 64)} seconds";

    private int increment = 10;
    private int limit = 75;
    private int period = 128;
}

public class BonusArmor : ISuperPower
{
    public BonusArmor() => Triggers = [typeof(EventRoundStart)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                continue;

            pawn.ArmorValue = value;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

        }
        return HookResult.Continue;
    }

    public override string GetDescription() => $"Obtain {value} armor each round, head armor not included";

    private int value = 250;
}

public class InstantDefuse : ISuperPower
{
    public InstantDefuse() => Triggers = [typeof(EventBombBegindefuse)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventBombBegindefuse)gameEvent;
        var player = realEvent.Userid;
        if (player != null && player.IsValid && player.PawnIsAlive)
        {
            if (!Users.Contains(player))
                return HookResult.Continue;

            var bomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").ToList().FirstOrDefault();
            if (bomb == null)
            {
                return HookResult.Continue; ;
            }
            Server.NextFrame(() =>
            {
                bomb.DefuseCountDown = 0;
                Utilities.SetStateChanged(bomb, "CPlantedC4", "m_flDefuseCountDown");

            });
        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Defuse bombs instantly (even withot defuse kit)";
}

public class InstantPlant : ISuperPower
{
    public InstantPlant() => Triggers = [typeof(EventBombBeginplant)];
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
}

public class Banana : ISuperPower
{
    public Banana()
    {
        Triggers = [typeof(EventRoundStart)];
        NeededResources = ["models/food/fruits/banana01a.vmdl"];
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

public class InfiniteAmmo : ISuperPower
{
    public InfiniteAmmo() => Triggers = [typeof(EventWeaponFire)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventWeaponFire)gameEvent;
        var player = realEvent.Userid;

        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(player))
            return HookResult.Continue;

        if (player != null && player.IsValid && player.PawnIsAlive)
        {
            CBasePlayerWeapon? activeWeapon = player?.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

            if (activeWeapon == null)
                return HookResult.Continue;

            if (activeWeapon.Clip1 < 5)
                activeWeapon.Clip1 = 5;
            else
                activeWeapon.Clip1 += 1;

        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Zeus included, nades not included";
}

public class SuperSpeed : ISuperPower
{
    public SuperSpeed() => Triggers = [typeof(EventRoundStart)];

    public override HookResult Execute(GameEvent gameEvent)
    {
        TemUtils.PowerApplySpeed(Users, value);
        return HookResult.Continue;
    }

    public override void Update()
    {
        if (Server.TickCount % period != 0) return;
        TemUtils.PowerApplySpeed(Users, value);
    }

    public override void OnRemovePower(CCSPlayerController? player)
    {
        TemUtils.PowerRemoveSpeedModifier(Users, player);
    }

    public override string GetDescription() => $"Increased walking speed ({(float)value / default_velocity_max})";

    private int value = 700;
    private int period = 4;
    public const int default_velocity_max = 250;
}

public class HeadshotImmunity : ISuperPower
{
    public HeadshotImmunity() => Triggers = [typeof(EventPlayerHurt)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventPlayerHurt)gameEvent;
        var player = realEvent.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(player))
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return HookResult.Continue;

        if ((HitGroup_t)realEvent.Hitgroup == HitGroup_t.HITGROUP_HEAD)
        {
            pawn.Health = pawn.Health + realEvent.DmgHealth;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            pawn.ArmorValue += realEvent.DmgArmor;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Calcels all headshots, landed on your head";
}

public class InfiniteMoney : ISuperPower
{
    public InfiniteMoney() => Triggers = [typeof(EventRoundStart)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        return HookResult.Continue;
    }

    public override void Update()
    {
        if (Server.TickCount % 32 != 0)
            return;
        foreach (var user in Users)
        {
            user.InGameMoneyServices!.Account += 500;
            Utilities.SetStateChanged(user, "CCSPlayerController", "m_pInGameMoneyServices");
        }
    }

    public override void OnRemovePower(CCSPlayerController? player)
    {
        if (player != null)
            player.InGameMoneyServices!.Account = 800;
        else
        {
            Users.ForEach(p =>
            {
                p.InGameMoneyServices!.Account = 800;
            });
        }
    }

    public override string GetDescription() => $"Near infinite supply of money";
}

public class NukeNades : ISuperPower
{
    public NukeNades() => Triggers = [typeof(EventGrenadeThrown)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventGrenadeThrown)gameEvent;
        var player = realEvent.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(player))
            return HookResult.Continue;

        var all_grenades = Utilities.FindAllEntitiesByDesignerName<CHEGrenadeProjectile>("hegrenade_projectile");
        if (all_grenades.Count() == 0)
            return HookResult.Continue;

        var grenade = all_grenades.First();
        if (player.UserId == grenade.Thrower.Value!.OriginalController.Value!.UserId)
        {
            grenade.Damage *= multiplier;
            grenade.DmgRadius *= multiplier;

            grenade.DetonateTime += 1;
        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"HE grenades, but {multiplier} times more explosive";

    private float multiplier = 10;
}

public class EvilAura : ISuperPower
{
    public EvilAura() => Triggers = [typeof(EventRoundStart)];

    public override HookResult Execute(GameEvent gameEvent)
    {
        return HookResult.Continue;
    }

    public override void Update()
    {
        if (Server.TickCount % period != 0)
            return;
        var players = Utilities.GetPlayers();
        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                continue;

            if (pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                continue;

            var playersInRadius = players.Where(p => p.PlayerPawn.Value != null && TemUtils.CalcDistance(p.PlayerPawn.Value.AbsOrigin!, pawn.AbsOrigin!) <= distance);

            foreach (var player_to_harm in playersInRadius)
            {
                if (player_to_harm.TeamNum == user.TeamNum) // skip teammates
                    continue;
                if (player_to_harm.TeamNum == 1) // skip spectators, just in case
                    continue;
                var harm_pawn = player_to_harm.PlayerPawn.Value!;
                if (harm_pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) // only harm alive specimens
                    continue;
                if (harm_pawn.Health <= 2)
                    continue;

                harm_pawn.Health = harm_pawn.Health - damage;
                Utilities.SetStateChanged(harm_pawn, "CBaseEntity", "m_iHealth");

                user.PrintToCenter($"Harmed someone for {damage}...");
                player_to_harm.PrintToCenter($"You have been hurt by {user.PlayerName}'s evil aura");
            }
        }
    }

    private float distance = 250;
    private int damage = 1;
    private int period = 16;

    public override string GetDescription() => $"Slowly harm enemies close to you. Can't kill";
}

public class DormantPower : ISuperPower
{
    public DormantPower() => Triggers = [typeof(EventRoundStart)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.Handle == 0)
            return HookResult.Continue; // prevent recursive call

        var gameRules = TemUtils.GetGameRules();

        if (dormant_power_rules.Count == 0)
        {
            ParseMasterRule();
        }

        HashSet<string> power_commands = [];

        if (dormant_power_rules.Count == 0)
        {
            Server.PrintToConsole($"No rules for {gameRules.TotalRoundsPlayed} rounds");
            return HookResult.Continue;
        }

        try
        {
            power_commands = dormant_power_rules[gameRules.TotalRoundsPlayed];
        }
        catch (Exception)
        {
            return HookResult.Continue;
        }

        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                continue;

            foreach (var command in power_commands)
            {
                Server.NextFrame(() =>
                {
                    var real_command = command.Replace("user", user.PlayerName);
                    Server.ExecuteCommand(real_command);
                    TemUtils.Log($"{ChatColors.Blue}Executed command {real_command} for {user.PlayerName}");
                });
            }
        }
        return HookResult.Continue;
    }

    private Dictionary<int, HashSet<string>> dormant_power_rules = [];

    public override string GetDescription() => $"Internal use only";

    private string master_rule = "fill_me";
    private string round_rule_separator = "|";
    private string power_separator = ";";

    private void ParseMasterRule()
    {
        if (master_rule == "fill_me")
        {
            TemUtils.AlertError("Master rule is not set");
            return;
        }

        var round_rules = master_rule.Split(round_rule_separator).ToHashSet();
        if (round_rules.Count == 0)
            return;

        foreach (var rule in round_rules)
        {
            var power_commands = rule.Split(power_separator);

            int round_number = int.Parse(power_commands[0]);

            dormant_power_rules.Add(round_number, power_commands.ToHashSet());
        }
    }
}

public class DamageBonus : ISuperPower
{
    public DamageBonus() => Triggers = [typeof(EventPlayerHurt)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventPlayerHurt)gameEvent;
        var attacker = realEvent.Attacker;

        if (attacker == null || !attacker.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(attacker))
            return HookResult.Continue;

        var victim = realEvent.Userid;
        if (victim == null || !victim.IsValid)
            return HookResult.Continue;

        var pawn = victim.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        pawn.Health = pawn.Health - realEvent.DmgHealth * damage_multiplier;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        return HookResult.Continue;
    }

    public override string GetDescription() => $"All your damage is multiplied by {damage_multiplier}";

    private int damage_multiplier = 2;
}

public class Vampirism : ISuperPower
{
    public Vampirism() => Triggers = [typeof(EventPlayerHurt)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventPlayerHurt)gameEvent;
        var attacker = realEvent.Attacker;
        var victim = realEvent.Userid;

        if (attacker == null || !attacker.IsValid)
            return HookResult.Continue;

        if (!Users.Where(p => p.UserId == attacker.UserId).Any())
            return HookResult.Continue;

        var attackerPawn = attacker.PlayerPawn.Value;
        if (attackerPawn == null || !attackerPawn.IsValid)
            return HookResult.Continue;

        attackerPawn.Health = attackerPawn.Health + realEvent.DmgHealth / divisor;

        Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");
        //Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");

        var sounds = vampireSounds.Split(";");
        attacker.ExecuteClientCommand("play " + sounds.ElementAt(new Random().Next(sounds.Length)));
        // TemUtils.EmitSound(attacker,) // dosn work aparently, requires source2viewer
        if (victim != null && victim.IsValid)
            victim.ExecuteClientCommand("play " + sounds.ElementAt(new Random().Next(sounds.Length)));

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Gain {(int)(100 / divisor)}% of dealt damage, annoying sounds included";

    private int divisor = 5;
    private string vampireSounds = "sounds/physics/flesh/flesh_squishy_impact_hard4.vsnd;sounds/physics/flesh/flesh_squishy_impact_hard3.vsnd;sounds/physics/flesh/flesh_squishy_impact_hard2.vsnd;sounds/physics/flesh/flesh_squishy_impact_hard1.vsnd";
}


public class Invisibility : ISuperPower
{
    public Invisibility() => Triggers = [typeof(EventPlayerSound), typeof(EventWeaponFire)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent is EventPlayerSound realEventSound)
            HandleEvent(realEventSound.Userid, 0.4);
        if (gameEvent is EventWeaponFire realEventFire)
            HandleEvent(realEventFire.Userid, 0.2);
        return HookResult.Continue;
    }

    private void HandleEvent(CCSPlayerController? player, double duration = 1.0)
    {
        if (player == null)
            return;

        if (!Users.Contains(player))
            return;

        var idx = Users.IndexOf(player);
        if (idx == -1)
            return;

        Levels[idx] = -duration;
    }

    public override void Update()
    {
        for (int i = 0; i < Users.Count; i++)
        {
            if (Levels[i] < 0.5)
                continue;

            var player = Users[i];

            if (player.PlayerPawn != null && player.PlayerPawn.Value != null)
            {
                var pawn = player.PlayerPawn.Value;

                pawn.EntitySpottedState.SpottedByMask[0] = 0;
                pawn.EntitySpottedState.SpottedByMask[1] = 0;
                pawn.EntitySpottedState.Spotted = false;

                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_entitySpottedState", Schema.GetSchemaOffset("EntitySpottedState_t", "m_bSpotted"));
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_entitySpottedState", Schema.GetSchemaOffset("EntitySpottedState_t", "m_bSpottedByMask"));
            }

        }

        if (Server.TickCount % 8 != 0)
            return;

        for (int i = 0; i < Users.Count; i++)
        {
            var newValue = Levels[i] < 1.0f ? Levels[i] + 0.1 : 1.0f;

            if (newValue != Levels[i])
            {
                var player = Users[i];
                TemUtils.SetPlayerVisibilityLevel(player, (float)newValue);

                Levels[i] = newValue;
            }
        }
    }

    public override void OnRemovePower(CCSPlayerController? player)
    {
        if (player == null)
        {
            foreach (var p in Users)
            {
                Levels[Users.IndexOf(p)] = -1.0f;
                TemUtils.SetPlayerVisibilityLevel(p, 0.0f);
            }
            return;
        }

        Levels[Users.IndexOf(player)] = -1.0f;
        TemUtils.SetPlayerVisibilityLevel(player, 0.0f);
    }

    public override string GetDescription() => $"I cant really see you";

    public double[] Levels = new double[65];
}


public class SuperJump : ISuperPower
{
    public SuperJump() => Triggers = [typeof(EventPlayerJump)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        EventPlayerJump realEvent = (EventPlayerJump)gameEvent;
        var player = realEvent.Userid;
        if (player == null)
            return HookResult.Continue;

        if (!Users.Where(p => p.UserId == player.UserId).Any())
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return HookResult.Continue;

        if (pawn.V_angle.X < -55)
            Server.NextFrame(() =>
            {
                pawn.AbsVelocity.Z *= multiplier;
                if (pawn.AbsVelocity.Z < 290 * multiplier)
                    Server.NextFrame(() =>
                    {
                        pawn.AbsVelocity.Z *= multiplier;
                    });
            });

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Look up and jump to get {multiplier} times higher";

    private float multiplier = 2;
}

public class ExplosionUponDeath : ISuperPower
{
    public ExplosionUponDeath() => Triggers = [typeof(EventPlayerDeath)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent is null)
            return HookResult.Continue;

        EventPlayerDeath realEvent = (EventPlayerDeath)gameEvent;
        var player = realEvent.Userid;
        if (player == null)
            return HookResult.Continue;

        if (!Users.Where(p => p.UserId == player.UserId).Any())
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return HookResult.Continue;

        var heProjectile = Utilities.CreateEntityByName<CHEGrenadeProjectile>("hegrenade_projectile");

        if (heProjectile == null || !heProjectile.IsValid) return HookResult.Continue;

        var node = pawn.CBodyComponent!.SceneNode;
        Vector pos = node!.AbsOrigin;
        pos.Z += 10;
        heProjectile.TicksAtZeroVelocity = 100;
        heProjectile.TeamNum = pawn.TeamNum;
        heProjectile.Damage = damage;
        heProjectile.DmgRadius = radius;
        heProjectile.Teleport(pos, node!.AbsRotation, new Vector(0, 0, -10));
        heProjectile.DispatchSpawn();
        heProjectile.AcceptInput("InitializeSpawnFromWorld", player.PlayerPawn.Value!, player.PlayerPawn.Value!, "");
        heProjectile.DetonateTime = 0;

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Explode on death, dealing {damage} damage in a {radius} units radius";

    private int radius = 500;
    private float damage = 125f;
}

// supposed to warp players back a few seconds after they get hurt
public class WarpPeek : ISuperPower
{
    public WarpPeek() => Triggers = [typeof(EventPlayerHurt)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent is null)
            return HookResult.Continue;

        EventPlayerHurt realEvent = (EventPlayerHurt)gameEvent;
        var player = realEvent.Userid;
        if (player == null)
            return HookResult.Continue;

        if (!Users.Contains(player))
            return HookResult.Continue;

        if (timeouts.ContainsKey(player))
            if (timeouts[player] > 0)
            {
                timeouts[player] = timeout;
                return HookResult.Continue;
            }

        var pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return HookResult.Continue;

        int next_index = (current_index + 1) % max_index;

        pawn.Teleport(positions[player][next_index].Item1, positions[player][next_index].Item2, new Vector(0, 0, 0));

        timeouts[player] = timeout;

        return HookResult.Continue;
    }

    public override void Update()
    {
        if (Server.TickCount % period != 0)
            return;

        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null) continue;

            var absOrigin = pawn.AbsOrigin;
            if (absOrigin == null) continue;

            if (!positions.ContainsKey(user))
                positions[user] = [];

            positions[user][current_index] = new Tuple<Vector, QAngle>(
                new Vector(pawn.AbsOrigin!.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z),
                new QAngle(pawn.V_angle.X, pawn.V_angle.Y, pawn.V_angle.Z)
                );

            if (!timeouts.ContainsKey(user))
                timeouts.Add(user, 0);

            if (timeouts[user] > 0)
                timeouts[user] -= 1;
        }

        current_index++;
        if (current_index >= max_index)
            current_index = 0;
    }

    public override string GetDescription() => $"Warp back in time when hit. Only position is saved";

    // each user must have their own position history
    // has a static array for memory economy
    public Dictionary<CCSPlayerController, Dictionary<int, Tuple<Vector, QAngle>>> positions = [];
    public Dictionary<CCSPlayerController, int> timeouts = [];
    public int current_index = 0;
    private int max_index = 10;
    private int period = 16;
    private int timeout = 30;
}

public class Snowballing : ISuperPower
{
    public Snowballing() => Triggers = [typeof(EventPlayerDeath), typeof(EventRoundStart), typeof(EventPlayerHurt)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        Type type = gameEvent.GetType();

        if (type == typeof(EventPlayerDeath) && giveHealImmediately) // each  time he kills someone, he gets a heal bonus
        {
            EventPlayerDeath realEvent = (EventPlayerDeath)gameEvent;

            var player = realEvent.Attacker;
            if (player == null)
                return HookResult.Continue;

            if (!Users.Contains(player))
                return HookResult.Continue;

            var pawn = player.PlayerPawn.Value!;
            pawn.Health = pawn.Health + heal;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
        else if (type == typeof(EventPlayerHurt))
        {
            EventPlayerHurt realEvent = (EventPlayerHurt)gameEvent;

            var player = realEvent.Attacker!;

            if (!Users.Contains(player))
                return HookResult.Continue;

            // match stats contain kills thru de whol game, killCount only contains kills for this round
            float damage_uncapped_bonus = (resetOnRoundStart == false ? player.ActionTrackingServices!.MatchStats.Kills : player.KillCount) * dmg_inc;

            float damage_mult_bonus = max_dmg_inc > 0 ? Math.Min(damage_uncapped_bonus, max_dmg_inc) : damage_uncapped_bonus;
            int bonus_damage = (int)(realEvent.DmgHealth * damage_mult_bonus);

            var pawn = realEvent.Userid!.PlayerPawn.Value!;
            pawn.Health = pawn.Health - bonus_damage;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
        else if (type == typeof(EventRoundStart) && giveHealOnSpawn && resetOnRoundStart == false)
        {
            Users.ForEach(user =>
            {
                var pawn = user.PlayerPawn.Value!;
                pawn.Health = pawn.Health + Math.Min(user.ActionTrackingServices!.MatchStats.Kills * heal, max_heal);
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            });
        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Each kill will give you {heal} more HP and {dmg_inc * 100}% more damage. Limited to {max_heal} HP and {max_dmg_inc * 100}% bonus damage";

    private int heal = 25;
    private float dmg_inc = 0.1f;

    private int max_heal = 300;
    private float max_dmg_inc = 1.00f;

    private bool resetOnRoundStart = true;
    private bool giveHealImmediately = true;
    private bool giveHealOnSpawn = false;
}

public class ChargeJump : ISuperPower
{
    public ChargeJump() => Triggers = [typeof(EventPlayerJump)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        EventPlayerJump realEvent = (EventPlayerJump)gameEvent;
        var player = realEvent.Userid;
        if (player == null)
            return HookResult.Continue;

        if (!Users.Contains(player))
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return HookResult.Continue;

        if ((player.Buttons & PlayerButtons.Duck) != 0)
        {
            // get the player's look angle
            var look_angle = new QAngle(pawn.V_angle.X, pawn.V_angle.Y, pawn.V_angle.Z);
            var out_forward = new Vector();

            NativeAPI.AngleVectors(look_angle.Handle, out_forward.Handle, 0, 0);
            // amplify the forward vector
            out_forward *= jump_force;
            // add the forward vector to the player's velocity
            Server.NextFrame(() =>
                pawn.AbsVelocity.Add(out_forward)
            );
        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Jump while crouching to make a leap forward";

    private float jump_force = 600f;
}

public class SmallSize : ISuperPower
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

public class RageMode : ISuperPower
{
    public RageMode()
    {
        Triggers = [typeof(EventPlayerDeath), typeof(EventPlayerHurt), typeof(EventRoundStart)];
        NeededResources = ["particles/survival_fx/gas_cannister_impact_child_explosion.vpcf"];
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
                TemUtils.PowerApplySpeed(Users, SpeedModifier);

                TemUtils.CreateParticle(pawn.AbsOrigin!, NeededResources[0], 2, "Breakable.MatGlass", player: player);

                InvicibilityTicks.Add(Tuple.Create(player, (int)(Server.TickCount + (InvincibilitySeconds * 64))));
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

            int bonus_damage = (int)(realEvent2.DmgHealth * DamageBonusMult) + DamageBonusFlat;

            pawn.Health = pawn.Health - bonus_damage;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }


        return HookResult.Continue;
    }
    public override string GetDescription() => $"When a player gets {KillsToRage} {(CountOnlyHeadshots ? "Headshots" : "Kills")}, he enters 'rage mode,' gaining speed, damage boost, and temporary invincibility";

    public override void OnRemovePower(CCSPlayerController? player)
    {
        TemUtils.PowerRemoveSpeedModifier(Users, player);

        ActivatedUsers.Clear();
        InvicibilityTicks.Clear();
    }

    public override void Update()
    {
        InvicibilityTicks.RemoveAll(t => t.Item2 <= Server.TickCount); // remove expired ticks

        if (Server.TickCount % UpdatePeriod != 0)
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
                pawn.Health += HealthRegenPerUpdate;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            }
        });

    }

    private bool IsEnoughKills(CCSPlayerController player)
    {
        var curKills = CountOnlyHeadshots ? player.ActionTrackingServices!.NumRoundKillsHeadshots : player.ActionTrackingServices!.NumRoundKills;
        return curKills >= KillsToRage;
    }

    private bool CountOnlyHeadshots = false;
    private int KillsToRage = 3;
    private int UpdatePeriod = 64;
    private int SpeedModifier = 450;
    private int DamageBonusFlat = 10;
    private double DamageBonusMult = 0.25d;
    private int HealthRegenPerUpdate = 1;
    private double InvincibilitySeconds = 1.5d;

    public List<CCSPlayerController> ActivatedUsers { get; set; } = [];
    public List<Tuple<CCSPlayerController, int>> InvicibilityTicks { get; set; } = [];
}

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

public class BotDisguise : ISuperPower
{
    public BotDisguise() => Triggers = [typeof(EventRoundStart)]; // TODO: clear player names from dropped weapons
    public override HookResult Execute(GameEvent gameEvent)
    {
        var e = gameEvent as EventRoundStart;

        Users.ForEach(u => ChangeNameRevertable(u));

        return HookResult.Continue;
    }

    public override void OnRemovePower(CCSPlayerController? player)
    {
        if (player == null)
        {
            Users.ForEach(u => RevertName(u));
            return;
        }

        RevertName(player);
    }

    public List<string> name_pool = ["Maddison", "Colton", "Rose", "Phoenix", "Maxine", "Chase", "Anna", "Andres", "Jaliyah", "Fox", "Emerie", "Karsyn", "Faye", "Lennox", "Reign", "Cole", "Kynlee", "Emory", "Bethany", "Van", "Emory", "Kenji", "Ivy", "Kane", "Alivia", "Bryce", "Milan", "Riley", "Reina", "Idris", "Ellis", "Nova", "Giovanna", "Ulises", "Harper", "Mark", "Mercy", "Iker", "Rowan", "Blake", "Mariah", "Korbin", "Nola", "Dillon", "Amara", "Gael", "Briana", "Dane", "Melany", "Quentin", "Sutton", "Shepherd", "Margo", "Matthias", "Paris", "Allen", "Whitney", "Blaze", "Leyla", "Eden", "Remy", "Remi", "Izabella", "Victor", "Freyja", "Waylon", "Judith", "Enoch", "Kinslee", "Marlon", "Jade", "Zyair", "Ryleigh", "Aaron", "Miracle", "Kannon", "Aaliyah", "Lochlan", "Ivanna", "Luka", "Kairi", "Jason", "Megan", "Kohen", "Bexley", "Patrick", "Persephone", "Shepard", "Ariella", "Johnathan", "Josephine", "Jacob", "Ansley", "Solomon", "Aylin", "Armando", "Aaliyah", "Anthony", "Kendra", "Jones", "Gracie", "Osiris", "Kylee", "Blaise", "Adeline", "Rodney", "Destiny", "Dominick", "Estelle", "Reuben", "Mia", "Cody", "Iyla", "Fabian", "Oakleigh", "Roger", "Anaya", "Brodie", "Emmalyn", "Memphis", "Keily", "Forest", "Millie", "Jorge", "Elise", "Caleb", "Summer", "Manuel", "Pearl", "Pierce", "Rosalia", "Edgar", "June", "Marley", "Marlowe", "Edgar", "Mavis", "Kashton", "Dayana", "Marshall", "Alanna", "Layne", "Adelina", "Mekhi"];

    public override string GetDescription() => $"Disguise as a bot (to a certain point)";

    public override void Update()
    {
        if (Server.TickCount % 128 == 0)
            Users.ForEach(u => TemUtils.CleanWeaponOwner(u));
        if (Server.TickCount % 1024 == 0)
            Users.ForEach(u => ChangeNameRevertable(u));
    }

    private void ChangeNameRevertable(CCSPlayerController player)
    {
        if (player.IsBot)
            return;

        ulong uuid = player.SteamID;

        if (!originalNames.ContainsKey(uuid))
        {
            originalNames[uuid] = player.PlayerName;
        }

        if (chosenNames.ContainsKey(uuid))
        {
            TemUtils.UpdatePlayerName(player, chosenNames[uuid], "BOT");
            return;
        }

        ulong nameIndex = (ulong)Random.Shared.Next() % (ulong)name_pool.Count; // comp an index from da current name
        string name = name_pool[(int)nameIndex];

        chosenNames[uuid] = name;

        TemUtils.UpdatePlayerName(player, name, "BOT");
    }

    private void RevertName(CCSPlayerController player)
    {
        if (player.IsBot)
            return;

        ulong uuid = player.SteamID;

        if (originalNames.ContainsKey(uuid))
        {
            TemUtils.UpdatePlayerName(player, originalNames[uuid]);

            originalNames.Remove(uuid);
            chosenNames.Remove(uuid);
        }
    }

    public Dictionary<ulong, string> originalNames = [];
    public Dictionary<ulong, string> chosenNames = [];
}


public class BotGuesser : ISuperPower
{
    public BotGuesser() => Triggers = [typeof(EventRoundStart)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var gameRules = TemUtils.GetGameRules();

        if (gameRules.TotalRoundsPlayed == 0 ||
         gameRules.TotalRoundsPlayed % guess_each_x_rounds != 0)
            return HookResult.Continue;

        foreach (var user in Users)
        {
            user.PrintToChat("Vote availiable - use !signal kick <name>");
            user.PrintToChat("Vote availiable - use !signal kick <name>");
            user.PrintToChat("Vote availiable - use !signal kick <name>");
            user.PrintToChat("Vote availiable - use !signal kick <name>");
            user.PrintToChat("Wildcards usable to match any set of characters, example - '*hnepixel*' , matches 0hnepixel ");

            allow_vote = true;
        }

        return HookResult.Continue;
    }

    public override Tuple<SIGNAL_STATUS, string> OnSignal(CCSPlayerController? player, List<string> args)
    {
        if (args.Count != 2)
            goto shortcut_ignore;

        string details = "";

        string subcmd = args[0];
        if (subcmd == "kick")
        {
            if (allow_vote == false)
            {
                details = "Not availiable";
                goto shortcut_error;
            }

            if (args.Count == 1)
            {
                details = "Not enough arguments";
                goto shortcut_error;
            }

            string target = args[1];
            // Server.PrintToChatAll($"desired to kick {target}");

            var sel = TemUtils.SelectPlayers(target);

            if (sel == null || !sel.Any())
            {
                details = "Player not found";
                goto shortcut_error;
            }

            var sel_player = sel.First();

            // Server.PrintToChatAll($"selected {sel_player.PlayerName}");

            var power = SuperPowerController.GetPowersByName("bot_disguise");

            if (power.Users.Contains(sel_player))
            {
                allow_vote = false;

                if (sel_player.IsBot == do_kick_bots)
                {
                    Server.PrintToChatAll($"Guessed right");
                    sel_player.Disconnect(CounterStrikeSharp.API.ValveConstants.Protobuf.NetworkDisconnectionReason.NETWORK_DISCONNECT_KICKED_VOTEDOFF);
                }
                else
                    Server.PrintToChatAll($"Incorrect");
            }
            else
            {
                Server.PrintToChatAll($"{sel_player.PlayerName} cant be chosen for this, vote is still availiable");
            }

            return Tuple.Create(SIGNAL_STATUS.ACCEPTED, "");
        }

    shortcut_ignore:
        return Tuple.Create(SIGNAL_STATUS.IGNORED, "");
    shortcut_error:
        return Tuple.Create(SIGNAL_STATUS.ERROR, details);
    }

    private bool do_kick_bots = false;
    private int guess_each_x_rounds = 5;

    public bool allow_vote = false;
    public int rounds_without_a_vote = 0;

    public override string GetDescription() => $"Allows to kick bots each round";
}

public class HealingZeus : ISuperPower
{
    public HealingZeus() => Triggers = [typeof(EventPlayerHurt)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventPlayerHurt)gameEvent;
        var attacker = realEvent.Attacker;
        var victim = realEvent.Userid;

        if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(attacker))
            return HookResult.Continue;

        var victim_pawn = victim.PlayerPawn.Value;
        if (victim_pawn == null || !victim_pawn.IsValid)
            return HookResult.Continue;

        if (victim_pawn.TeamNum != attacker.TeamNum)
            return HookResult.Continue;

        victim_pawn.Health = value;

        Utilities.SetStateChanged(victim_pawn, "CBaseEntity", "m_iHealth");
        return HookResult.Continue;
    }

    public override string GetDescription() => $"zap your teammates to set their health to {value}";

    private int value = 75;
}

public class FlashOfDisability : ISuperPower
{
    public FlashOfDisability() => Triggers = [typeof(EventPlayerBlind)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventPlayerBlind)gameEvent;

        // Server.PrintToChatAll("blind detected");

        var attacker = realEvent.Attacker;
        var victim = realEvent.Userid;

        if (attacker == null || !attacker.IsValid || victim == null || !victim.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(attacker))
            return HookResult.Continue;

        if (ignore_self_flash && attacker == victim)
            return HookResult.Continue;

        // SuperPowerController.DisablePlayer(victim, (int)(victim.PlayerPawn.Value!.BlindStartTime - victim.PlayerPawn.Value!.BlindUntilTime));
        SuperPowerController.DisablePlayer(victim, (int)victim.PlayerPawn.Value!.FlashDuration * 64);

        return HookResult.Continue;
    }

    public override string GetDescription() => $"enemies have their powers disabled if you flash them";

    private bool ignore_self_flash = true;
}

public class PoisonedSmoke : ISuperPower
{
    public PoisonedSmoke() => Triggers = [typeof(EventSmokegrenadeDetonate), typeof(EventSmokegrenadeExpired)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        Type type = gameEvent.GetType();
        if (type == typeof(EventSmokegrenadeDetonate))
        {
            var realEvent = (EventSmokegrenadeDetonate)gameEvent;

            var thrower = realEvent.Userid;

            if (thrower == null || !thrower.IsValid)
                return HookResult.Continue;

            if (!Users.Contains(thrower))
                return HookResult.Continue;

            SmokesActivePos.Add(Tuple.Create(realEvent.Entityid, new Vector(realEvent.X, realEvent.Y, realEvent.Z)));

            var smokeEntity = Utilities.GetEntityFromIndex<CSmokeGrenadeProjectile>(realEvent.Entityid);

            if (smokeEntity != null)
            {
                smokeEntity.SmokeColor.X = 0.0f;
                smokeEntity.SmokeColor.Y = Random.Shared.NextSingle() * 255.0f;
                smokeEntity.SmokeColor.Z = 0.0f;
                // Utilities.SetStateChanged(smokeEntity, "CSmokeGrenadeProjectile", "m_vSmokeColor");
                // Server.PrintToChatAll($"set to green entity {smokeEntity.DesignerName}");
            }

        }

        if (type == typeof(EventSmokegrenadeExpired))
        {
            var realEvent = (EventSmokegrenadeExpired)gameEvent;

            SmokesActivePos.RemoveAll(t => t.Item1 == realEvent.Entityid);
        }

        return HookResult.Continue;
    }

    public override void Update()
    {
        if (Server.TickCount % 64 != 0)
            return;

        var players = Utilities.GetPlayers();

        foreach (var pos in SmokesActivePos)
        {
            var playersInRadius = players.Where(p => p.PlayerPawn.Value != null && TemUtils.CalcDistance(p.PlayerPawn.Value.AbsOrigin!, pos.Item2) <= smoke_radius);

            foreach (var player_to_harm in playersInRadius)
            {
                if (player_to_harm.TeamNum == 1) // skip spectators, just in case
                    continue;
                var harm_pawn = player_to_harm.PlayerPawn.Value!;
                if (harm_pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) // only harm alive specimens
                    continue;
                if (harm_pawn.Health <= value) // dont ever touch players with low health
                {
                    harm_pawn.CommitSuicide(false, true);
                    continue;
                }

                harm_pawn.Health = harm_pawn.Health - value;
                Utilities.SetStateChanged(harm_pawn, "CBaseEntity", "m_iHealth");
            }
        }

    }

    public List<Tuple<int, Vector>> SmokesActivePos = [];

    public override string GetDescription() => $"your smoke poisons anyone in it, {value} damage per second";

    private int value = 2;
    private int smoke_radius = 144;
}

public class DamageLoss : ISuperPower
{
    public DamageLoss() => Triggers = [typeof(EventPlayerHurt)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventPlayerHurt)gameEvent;
        var victim = realEvent.Userid;

        if (victim == null || !victim.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(victim))
            return HookResult.Continue;

        if (Random.Shared.NextSingle() < probability / 100.0f)
            return HookResult.Continue;

        var victim_pawn = victim.PlayerPawn.Value;
        if (victim_pawn == null || !victim_pawn.IsValid)
            return HookResult.Continue;

        victim_pawn.Health += realEvent.DmgHealth;
        victim_pawn.ArmorValue += realEvent.DmgArmor;

        Utilities.SetStateChanged(victim_pawn, "CBaseEntity", "m_iHealth");
        Utilities.SetStateChanged(victim_pawn, "CCSPlayerPawn", "m_ArmorValue");

        return HookResult.Continue;
    }

    public override string GetDescription() => $"{probability}% chance to ignore incoming damage event";

    private int probability = 50;
}

public class ShortFusedBomb : ISuperPower
{
    public ShortFusedBomb() => Triggers = [typeof(EventBombPlanted)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventBombPlanted)gameEvent;

        if (!Users.Contains(realEvent.Userid!))
            return HookResult.Continue;

        var bombEntity = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
        if (bombEntity != null)
        {
            bombEntity.TimerLength *= 2;
            Server.PrintToChatAll($"set timer to {bombEntity.TimerLength}");
            Utilities.SetStateChanged(bombEntity, "CPlantedC4", "m_flTimerLength");
            // bombEntity.DefuseCountDown = 2;
            // Utilities.SetStateChanged(bombEntity, "CPlantedC4", "m_flDefuseCountDown");
        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"bomb have detonation time divided by {divisor} (T only)";

    private int divisor = 2;
}


public class InstantNades : ISuperPower
{
    public InstantNades() => Triggers = [typeof(EventGrenadeThrown)];
    public override HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventGrenadeThrown)gameEvent;
        var player = realEvent.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (!Users.Contains(player))
            return HookResult.Continue;

        Server.PrintToChatAll(realEvent.Weapon);

        var match_grenade = Utilities.FindAllEntitiesByDesignerName<CHEGrenadeProjectile>(realEvent.Weapon + "_projectile");
        if (match_grenade.Count() == 0)
            return HookResult.Continue;

        var grenade = match_grenade.First();

        if (grenade != null && player.UserId == grenade.Thrower.Value!.OriginalController.Value!.UserId)
        {
            grenade.DetonateTime = Server.CurrentTime + 1 / divider;
        }

        return HookResult.Continue;
    }

    public override string GetDescription() => $"Reduce grenade and flash fuse by {divider} times";

    private int divider = 4;
}

public class Pacifism : ISuperPower
{
    public Pacifism() => Triggers = [typeof(EventPlayerHurt), typeof(EventRoundStart)];

    public override HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.GetType() == typeof(EventPlayerHurt))
        {
            var realEvent = (EventPlayerHurt)gameEvent;
            var victim = realEvent.Userid;
            var attacker = realEvent.Attacker;

            if (victim == null || !victim.IsValid || attacker == null || !attacker.IsValid)
                return HookResult.Continue;

            if (Users.Contains(attacker)) // if attacker is a user, reset its capabilities
            {
                // Server.PrintToChatAll("pacifism removed");
                status.Remove(attacker);

                attacker.PrintToCenter("Pacifism removed");
            }

            if (Users.Contains(victim)) // check if victim might be le pacifist
            {
                // Server.PrintToChatAll("pacifism victom found");
                if (status.Contains(victim)) // effect is active, cancel all damage
                {
                    // Server.PrintToChatAll("damage sustained");

                    var victim_pawn = victim.PlayerPawn.Value;
                    if (victim_pawn == null || !victim_pawn.IsValid)
                        return HookResult.Continue;

                    victim_pawn.Health += realEvent.DmgHealth;
                    victim_pawn.ArmorValue += realEvent.DmgArmor;

                    Utilities.SetStateChanged(victim_pawn, "CBaseEntity", "m_iHealth");
                    Utilities.SetStateChanged(victim_pawn, "CCSPlayerPawn", "m_ArmorValue");
                }
            }
        }
        else
        {
            // Server.PrintToChatAll("Gained pacifism");
            // reset pacifism status
            status.Clear();
            status = [.. Users];

            Users.ForEach((c) => c.PrintToCenter("Gained Pacifism"));

            // Server.PrintToChatAll(status.ToString());
        }

        return HookResult.Continue;
    }

    public override void OnRemovePower(CCSPlayerController? player)
    {
        if (player != null)
        {
            status.Remove(player);
            player.PrintToCenter("Pacifism removed");
        }
        else
        {
            status.Clear();
            status.ForEach((c) => c.PrintToCenter("Pacifism removed"));
        }
    }

    // public Dictionary<CCSPlayerController, bool> status;
    public List<CCSPlayerController> status = [];

    public override string GetDescription() => $"On round start, gain invincibility until you start dealing damage";
}