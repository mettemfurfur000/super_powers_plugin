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

namespace super_powers_plugin;

/*

    public TemplatePower(Dictionary<string, Dictionary<string, string>> cfg)
    {
        internal_name = Utils.ToSnakeCase(this.GetType().ToString()).Split(".").Last();
    }

*/
/*
public class TemplatePower : ISuperPower
{
    public List<Type> Triggers => [typeof(EventRoundStart)];
    public HookResult Execute(GameEvent gameEvent) { return HookResult.Continue; }

    public void Update() { }
    int value = 404;
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
}
*/

public class BonusHealth : ISuperPower
{
    public List<Type> Triggers => [typeof(EventRoundStart)];
    public HookResult Execute(GameEvent gameEvent)
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

    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];
    private int value = 250;

}

public class Regeneration : ISuperPower
{
    public List<Type> Triggers => [typeof(EventRoundStart)];
    public HookResult Execute(GameEvent gameEvent)
    {
        return HookResult.Continue;
    }

    public void Update()
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

    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];
    private int increment = 10;
    private int limit = 75;
    private int period = 128;
}

public class BonusArmor : ISuperPower
{
    public List<Type> Triggers => [typeof(EventRoundStart)];
    public HookResult Execute(GameEvent gameEvent)
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
    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];
    private int value = 250;
}

public class InstantDefuse : ISuperPower
{
    public List<Type> Triggers => [typeof(EventBombBegindefuse)];
    public HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventBombBegindefuse)gameEvent;
        var player = realEvent.Userid;
        if (player != null && player.IsValid && player.PawnIsAlive)
        {
            if (!Users.Where(p => p.UserId == player.UserId).Any())
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
    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];

    private string PowerName => this.GetType().ToString().Split(".").Last();
}

public class InstantPlant : ISuperPower
{
    public List<Type> Triggers => [typeof(EventBombBeginplant)];
    public HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventBombBeginplant)gameEvent;
        var player = realEvent.Userid;
        if (player != null && player.IsValid && player.PawnIsAlive)
        {
            if (!Users.Where(p => p.UserId == player.UserId).Any())
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
    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];

    private string PowerName => this.GetType().ToString().Split(".").Last();
}

public class Banana : ISuperPower
{
    public List<Type> Triggers => [typeof(EventRoundStart)];
    public HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventRoundStart)gameEvent;
        foreach (var user in Users)
        {
            if (user == null || !user.IsValid)
            {
                continue;
            }
            var pawn = user.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
            {
                continue;
            }
            var prop = Utilities.CreateEntityByName<CPhysicsPropMultiplayer>("prop_physics_multiplayer");
            if (prop == null)
            {
                continue;
            }
            var pizza_id = TemUtils.RandomString(12);

            prop.Globalname = pizza_id;
            prop.SetModel("models/food/fruits/banana01a.vmdl");
            prop.Teleport(pawn.AbsOrigin, pawn.AbsRotation, pawn.AbsVelocity);

            if (prop.Collision != null && prop.Collision.EnablePhysics != 1)
            {
                prop.Collision.EnablePhysics = 1;
            }

            if (prop.Collision != null)
            {
                prop.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
                prop.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_INTERACTIVE;
            }

            prop.AddEntityIOEvent("SetScale", null, null, "5");
            prop.DispatchSpawn();
        }
        return HookResult.Continue;
    }

    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];

    private string PowerName => this.GetType().ToString().Split(".").Last();
}

public class InfiniteAmmo : ISuperPower
{
    public List<Type> Triggers => [typeof(EventWeaponFire)];
    public HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventWeaponFire)gameEvent;
        var player = realEvent.Userid;

        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (!Users.Where(p => p.UserId == player.UserId).Any())
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
    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];

    private string PowerName => this.GetType().ToString().Split(".").Last();
}

public class SuperSpeed : ISuperPower
{
    public List<Type> Triggers => [typeof(EventRoundStart)];
    public HookResult Execute(GameEvent gameEvent) { return HookResult.Continue; }

    public void Update()
    {
        if (Server.TickCount % period != 0) return;
        ApplySpeed();
    }

    private void ApplySpeed()
    {
        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                return;

            pawn.MovementServices!.Maxspeed = value;
            pawn.VelocityModifier = (float)(value / 320);
        }
    }

    void OnRemove(CCSPlayerController? player)
    {
        if (player != null)
        {
            if (Users.Contains(player))
            {
                var pawn = player.PlayerPawn.Value!;

                pawn.MovementServices!.Maxspeed = default_velocity_max;
                pawn.VelocityModifier = 1;
            }
        }
    }

    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];

    private string PowerName => this.GetType().ToString().Split(".").Last();
    private int value = 500;
    private int period = 16;
    public const int default_velocity_max = 250;
}

public class HeadshotImmunity : ISuperPower
{
    public List<Type> Triggers => [typeof(EventPlayerHurt)];
    public HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventPlayerHurt)gameEvent;
        var player = realEvent.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (!Users.Where(p => p.UserId == player.UserId).Any())
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
    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];

    private string PowerName => this.GetType().ToString().Split(".").Last();
}

public class InfiniteMoney : ISuperPower
{
    public List<Type> Triggers => [typeof(EventRoundStart)];
    public HookResult Execute(GameEvent gameEvent)
    {
        return HookResult.Continue;
    }
    public void Update()
    {
        if (Server.TickCount % 64 != 0)
            return;
        foreach (var user in Users)
        {
            user.InGameMoneyServices!.Account += 500;
            Utilities.SetStateChanged(user, "CCSPlayerController", "m_pInGameMoneyServices");
        }
    }

    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];
}

public class NukeNades : ISuperPower
{
    public List<Type> Triggers => [typeof(EventGrenadeThrown)];
    public HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventGrenadeThrown)gameEvent;
        var player = realEvent.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (!Users.Where(p => p.UserId == player.UserId).Any())
            return HookResult.Continue;

        var all_grenades = Utilities.FindAllEntitiesByDesignerName<CHEGrenadeProjectile>("hegrenade_projectile");
        if (all_grenades.Count() == 0)
            return HookResult.Continue;

        var grenade = all_grenades.First();
        if (player.UserId == grenade.Thrower.Value!.OriginalController.Value!.UserId)
        {
            grenade.Damage *= 10;
            grenade.DmgRadius *= 10;

            //grenade.DetonateTime *= 3;

            //TemUtils.MakeModelGlow(grenade);
        }

        return HookResult.Continue;
    }

    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];
}

public class EvilAura : ISuperPower
{
    public List<Type> Triggers => [typeof(EventRoundStart)];
    public HookResult Execute(GameEvent gameEvent)
    {
        return HookResult.Continue;
    }
    public void Update()
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

            var playersInRadius = players.Where(p => p.PlayerPawn.Value != null && CalcDistance(p.PlayerPawn.Value.AbsOrigin!, pawn.AbsOrigin!) <= distance);

            foreach (var player_to_harm in playersInRadius)
            {
                if (player_to_harm.TeamNum == user.TeamNum) // skip teammates
                    continue;
                if (player_to_harm.TeamNum == 1) // skip spectators, just in case
                    continue;
                var harm_pawn = player_to_harm.PlayerPawn.Value!;
                if (harm_pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) // only harm alive specimens
                    continue;

                TemUtils.Damage(harm_pawn, (uint)damage);

                user.PrintToCenter($"Harmed someone for {damage}...");
                player_to_harm.PrintToCenter($"You have been hurt by {user.PlayerName}'s evil aura");
            }
        }
    }
    private float CalcDistance(Vector v1, Vector v2)
    {
        return (float)Math.Sqrt(Math.Pow(v1.X - v2.X, 2) + Math.Pow(v1.Y - v2.Y, 2) + Math.Pow(v1.Z - v2.Z, 2));
    }
    private float distance = 250;
    private float damage = 1;
    private int period = 16;

    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];
}

public class DormantPower : ISuperPower
{
    public List<Type> Triggers => [typeof(EventRoundStart)];
    public HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.Handle == 0)
            return HookResult.Continue; // prevent recursive call

        var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;

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

        power_commands = dormant_power_rules[gameRules.TotalRoundsPlayed];

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
    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];
    private Dictionary<int, HashSet<string>> dormant_power_rules = [];

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

public class GlassCannon : ISuperPower
{
    public List<Type> Triggers => [typeof(EventRoundStart), typeof(EventPlayerHurt)];
    public HookResult Execute(GameEvent gameEvent)
    {
        var eventType = gameEvent.GetType();
        if (eventType == typeof(EventPlayerHurt))
        {
            var realEvent = (EventPlayerHurt)gameEvent;
            var attacker = realEvent.Attacker;

            if (attacker == null || !attacker.IsValid)
                return HookResult.Continue;

            if (!Users.Where(p => p.UserId == attacker.UserId).Any())
                return HookResult.Continue;

            var victim = realEvent.Userid;
            if (victim == null || !victim.IsValid)
                return HookResult.Continue;

            var pawn = victim.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
                return HookResult.Continue;

            TemUtils.Damage(pawn, (uint)(realEvent.DmgHealth * damage_multiplier));
        }

        if (eventType == typeof(EventRoundStart))
        {
            foreach (var user in Users)
            {
                var pawn = user.PlayerPawn.Value;
                if (pawn == null)
                    continue;

                pawn.Health = health;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            }
        }

        return HookResult.Continue;
    }
    public void Update() { }
    private int health = 50;
    private int damage_multiplier = 2;
    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];
}

public class Vampirism : ISuperPower
{
    public List<Type> Triggers => [typeof(EventPlayerHurt)];
    public HookResult Execute(GameEvent gameEvent)
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
        if (victim != null && victim.IsValid)
            victim.ExecuteClientCommand("play " + sounds.ElementAt(new Random().Next(sounds.Length)));

        return HookResult.Continue;
    }

    public void Update() { }
    private int divisor = 5;
    private string vampireSounds = "sounds/physics/flesh/flesh_squishy_impact_hard4.vsnd;sounds/physics/flesh/flesh_squishy_impact_hard3.vsnd;sounds/physics/flesh/flesh_squishy_impact_hard2.vsnd;sounds/physics/flesh/flesh_squishy_impact_hard1.vsnd";
    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];
}


public class Invisibility : ISuperPower
{
    public List<Type> Triggers => [typeof(EventPlayerSound), typeof(EventWeaponFire)];
    public HookResult Execute(GameEvent gameEvent)
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

    public void Update()
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

    public void OnRemove(CCSPlayerController? player)
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
    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];
    public double[] Levels = new double[65];
}


public class SuperJump : ISuperPower
{
    public List<Type> Triggers => [typeof(EventPlayerJump)];
    public HookResult Execute(GameEvent gameEvent)
    {
        EventPlayerJump realEvent = (EventPlayerJump)gameEvent;
        var player = realEvent.Userid;
        if (player == null)
            return HookResult.Continue;

        if (!Users.Where(p => p.UserId == player.UserId).Any())
            return HookResult.Continue;

        var pawn = player.Pawn.Value;
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

    public void Update() { }
    private float multiplier = 2;
    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];
}

public class ExplosionUponDeath : ISuperPower
{
    public List<Type> Triggers => [typeof(EventPlayerDeath)];
    public HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent is null)
            return HookResult.Continue;

        EventPlayerDeath realEvent = (EventPlayerDeath)gameEvent;
        var player = realEvent.Userid;
        if (player == null)
            return HookResult.Continue;

        if (!Users.Where(p => p.UserId == player.UserId).Any())
            return HookResult.Continue;

        var pawn = player.Pawn.Value;
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
    public void Update() { }

    private int radius = 500;
    private float damage = 125f;

    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];
}

// supposed to warp players back a few seconds after they get hurt
public class WarpPeek : ISuperPower
{
    public List<Type> Triggers => [typeof(EventPlayerHurt)];
    public HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent is null)
            return HookResult.Continue;

        EventPlayerHurt realEvent = (EventPlayerHurt)gameEvent;
        var player = realEvent.Userid;
        if (player == null)
            return HookResult.Continue;

        if (!Users.Where(p => p.UserId == player.UserId).Any())
            return HookResult.Continue;

        if (timeouts.ContainsKey(player))
            if (timeouts[player] > 0)
            {
                timeouts[player] = timeout;
                return HookResult.Continue;
            }

        var pawn = player.Pawn.Value;
        if (pawn == null)
            return HookResult.Continue;

        int next_index = (current_index + 1) % max_index;

        pawn.Teleport(positions[player][next_index].Item1, positions[player][next_index].Item2, new Vector(0, 0, 0));

        timeouts[player] = timeout;

        return HookResult.Continue;
    }

    public void Update()
    {
        if (Server.TickCount % period != 0)
            return;

        foreach (var user in Users)
        {
            var pawn = user.Pawn.Value;
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

    public List<CCSPlayerController> Users { get; set; } = [];
    public List<ulong> UsersSteamIDs { get; set; } = [];
    // each user must have their own position history
    // has a static array for memory economy
    public Dictionary<CCSPlayerController, Dictionary<int, Tuple<Vector, QAngle>>> positions = [];
    public Dictionary<CCSPlayerController, int> timeouts = [];
    public int current_index = 0;
    private int max_index = 10;
    private int period = 16;
    private int timeout = 30;
}