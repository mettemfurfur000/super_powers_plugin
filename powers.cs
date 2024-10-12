using System;
using System.Numerics;
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

public class StartHealth : ISuperPower
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
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
    private int value = 250;

}

public class StartArmor : ISuperPower
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
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
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
            {
                //Server.PrintToConsole("player is not in list");
                return HookResult.Continue; ;
            }
            var bomb = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").ToList().FirstOrDefault();
            if (bomb == null)
            {
                //player.PrintToCenter("Failed to find bomb for you, buddy... my bad!");
                return HookResult.Continue; ;
            }
            Server.NextFrame(() =>
            {
                bomb.DefuseCountDown = 0;
                Utilities.SetStateChanged(bomb, "CPlantedC4", "m_flDefuseCountDown");
                //Server.PrintToConsole($"Successful instant defuse by {player.PlayerName}");
            });
        }
        else
            Server.PrintToConsole($"{PowerName} : player is null or not valid");
        return HookResult.Continue;
    }
    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();

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
            {
                //Server.PrintToConsole("player is not in list");
                return HookResult.Continue; ;
            }

            var bomb = Utilities.FindAllEntitiesByDesignerName<CC4>("weapon_c4").ToList().FirstOrDefault();
            if (bomb == null)
            {
                //player.PrintToCenter("Failed to find bomb for you, buddy... my bad!");
                return HookResult.Continue; ;
            }

            bomb.BombPlacedAnimation = false;
            bomb.ArmedTime = 0.0f;
        }
        else
            Server.PrintToConsole($"{PowerName} : player is null or not valid");

        return HookResult.Continue;
    }
    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();

    private string PowerName => this.GetType().ToString().Split(".").Last();
}

public class FoodSpawner : ISuperPower
{
    public List<Type> Triggers => [typeof(EventRoundStart)];
    public HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventRoundStart)gameEvent;
        foreach (var user in Users)
        {
            if (user == null || !user.IsValid)
            {
                Server.PrintToConsole("user is null or not valid");
                continue;
            }
            var pawn = user.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
            {
                Server.PrintToConsole("pawn is null or not valid");
                continue;
            }
            var prop = Utilities.CreateEntityByName<CPhysicsPropMultiplayer>("prop_physics_multiplayer");
            if (prop == null)
            {
                Server.PrintToConsole("Failed to create a prop");
                continue;
            }
            var pizza_id = TemUtils.RandomString(12);

            prop.Globalname = pizza_id;
            prop.SetModel("models/food/fruits/banana01a.vmdl");
            prop.Teleport(pawn.AbsOrigin, pawn.AbsRotation, pawn.AbsVelocity);

            if (prop.Collision != null && prop.Collision.EnablePhysics != 1)
            {
                Server.PrintToConsole("prop.Collision.EnablePhysics != 1");
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
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();

    private string PowerName => this.GetType().ToString().Split(".").Last();
}

public class InfiniteAmmo : ISuperPower
{
    public List<Type> Triggers => [typeof(EventWeaponFire)];
    public HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventWeaponFire)gameEvent;
        var player = realEvent.Userid;

        if (player != null && player.IsValid && player.PawnIsAlive)
        {
            if (!Users.Where(p => p.UserId == player.UserId).Any())
                return HookResult.Continue;

            CBasePlayerWeapon? activeWeapon = player?.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

            if (activeWeapon == null)
                return HookResult.Continue;

            if (activeWeapon.Clip1 < 5)
                activeWeapon.Clip1 = 5;
            else
                activeWeapon.Clip1 += 1;

        }
        else
            Server.PrintToConsole($"{PowerName} : player is null or not valid");
        return HookResult.Continue;
    }
    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();

    private string PowerName => this.GetType().ToString().Split(".").Last();
}

public class SonicSpeed : ISuperPower
{
    public List<Type> Triggers => [typeof(EventRoundStart)];
    public HookResult Execute(GameEvent gameEvent)
    {
        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                return HookResult.Continue; ;

            pawn.MovementServices!.Maxspeed = value;
            pawn.VelocityModifier = (float)(value / 240);
        }
        return HookResult.Continue;
    }

    public void Update()
    {
        if (Server.TickCount % 16 != 0) return;
        foreach (var user in Users)
        {
            var pawn = user.PlayerPawn.Value;
            if (pawn == null)
                return;

            pawn.MovementServices!.Maxspeed = value;
            pawn.VelocityModifier = (float)(value / 240);
        }
    }
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();

    private string PowerName => this.GetType().ToString().Split(".").Last();
    private int value = 404;
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

        // player.PrintToConsole($"Hit group is {realEvent.Hitgroup}");

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
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();

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
        if (Server.TickCount % 32 != 0)
            return;
        foreach (var user in Users)
        {
            user.InGameMoneyServices!.Account = 90000;
            Utilities.SetStateChanged(user, "CCSPlayerController", "m_pInGameMoneyServices");
        }
    }

    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
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

        var grenade = Utilities.FindAllEntitiesByDesignerName<CHEGrenadeProjectile>("hegrenade_projectile").First();
        if (player.UserId == grenade.Thrower.Value!.OriginalController.Value!.UserId)
        {
            grenade.Damage *= 10;
            grenade.DmgRadius *= 10;
        }

        return HookResult.Continue;
    }

    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
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

            var playersInRadius = players.Where(p => p.PlayerPawn.Value != null && CalcDistance(p.PlayerPawn.Value.AbsOrigin!, pawn.AbsOrigin!) <= distance);

            foreach (var player_to_harm in playersInRadius)
            {
                if (player_to_harm.TeamNum == user.TeamNum) // skip teammates
                    continue;
                if (player_to_harm.TeamNum == 1) // skip spectators, just in case
                    continue;
                var harm_pawn = player_to_harm.PlayerPawn.Value!;

                TemUtils.Damage(harm_pawn, (int)damage);

                user.PrintToCenter($"Harmed someone for {damage}...");
                player_to_harm.PrintToCenter($"You have been hurt by {user.PlayerName}'s evil aura");
            }
        }
    }
    private float CalcDistance(CounterStrikeSharp.API.Modules.Utils.Vector v1, CounterStrikeSharp.API.Modules.Utils.Vector v2)
    {
        return (float)Math.Sqrt(Math.Pow(v1.X - v2.X, 2) + Math.Pow(v1.Y - v2.Y, 2) + Math.Pow(v1.Z - v2.Z, 2));
    }
    private float distance = 500;
    private float damage = 1;
    private int period = 20;

    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
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
            SplitMasterRule();
        }

        HashSet<string> power_commands = [];
        try
        {
            power_commands = dormant_power_rules[gameRules.TotalRoundsPlayed];
        }
        catch
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
                    Server.PrintToConsole($"Executed command {real_command} for {user.PlayerName}");
                });
            }
        }
        return HookResult.Continue;
    }
    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
    private Dictionary<int, HashSet<string>> dormant_power_rules = new();

    private string master_rule = "fill_me";
    private string round_rule_separator = "|";
    private string command_separator = ";";

    private void SplitMasterRule()
    {
        var round_rules = master_rule.Split(round_rule_separator).ToHashSet();
        if (round_rules.Count == 0)
            return;

        foreach (var rule in round_rules)
        {
            var power_commands = rule.Split(command_separator);

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

            TemUtils.Damage(pawn, (int)(realEvent.DmgHealth * damage_multiplier));
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
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
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
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
}


public class Invisibility : ISuperPower
{
    public List<Type> Triggers => [typeof(EventPlayerSound), typeof(EventWeaponFire)];
    public HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent is EventPlayerSound realEventSound)
        {
            HandleEvent(realEventSound.Userid);
        }
        else if (gameEvent is EventWeaponFire realEventFire)
        {
            HandleEvent(realEventFire.Userid);
        }
        return HookResult.Continue;
    }

    private void HandleEvent(CCSPlayerController? player)
    {
        if (player != null)
        {
            if (!Users.Contains(player))
                return;

            var idx = Users.IndexOf(player);
            if (idx != -1)
            {
                levels[idx] = -1.0f; // will be visible for a few seconds or so
                TemUtils.SetPlayerVisibilityLevel(player, 0.0f);
            }
        }
    }

    public void Update()
    {
        if (Server.TickCount % 8 != 0)
            return;

        for (int i = 0; i < Users.Count; i++)
        {
            var newValue = levels[i] < 1.0f ? levels[i] + 0.5 : 1.0f;

            if (newValue != levels[i])
                TemUtils.SetPlayerVisibilityLevel(Users[i], (float)newValue);

            levels[i] = newValue;

            Users[i].BloodType = levels[i] >= 1.0f ? BloodType.ColorGreen : BloodType.ColorRed; // green == full invisible, red == visible
            Utilities.SetStateChanged(Users[i], "CBaseEntity", "m_nBloodType");
        }
    }

    public void OnRemove(CCSPlayerController? player)
    {
        if (player == null)
        {
            foreach (var p in Users)
            {
                levels[Users.IndexOf(p)] = -1.0f;
                TemUtils.SetPlayerVisibilityLevel(p, 0.0f);
            }
            return;
        }

        levels[Users.IndexOf(player)] = -1.0f;
        TemUtils.SetPlayerVisibilityLevel(player, 0.0f);
    }
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
    public List<double> levels = [65];
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

        var pawn = player.Pawn.Value;
        if (pawn == null)
            return HookResult.Continue;

        //pawn.Teleport(null, null, new CounterStrikeSharp.API.Modules.Utils.Vector(pawn.AbsVelocity.X, pawn.AbsVelocity.Y, pawn.AbsVelocity.Z + 300));

        if (pawn.V_angle.X < -55)
            Server.NextFrame(() =>
            {
                pawn.AbsVelocity.Z *= multiplier;
                if (pawn.AbsVelocity.Z < 300 * multiplier)
                    Server.NextFrame(() =>
                    {
                        pawn.AbsVelocity.Z *= multiplier;
                    });
            });

        //pawn.MovementServices.Impulse;
        // Server.PrintToChatAll($"{pawn.MovementServices.Impulse}");

        return HookResult.Continue;
    }

    public void Update() { }
    private float multiplier = 2;
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
}
