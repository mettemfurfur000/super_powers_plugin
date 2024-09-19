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

namespace super_powers_plugin;

/*

    public TemplatePower(Dictionary<string, Dictionary<string, string>> cfg)
    {
        internal_name = Utils.ToSnakeCase(this.GetType().ToString()).Split(".").Last();
    }

*/

public class TemplatePower : ISuperPower
{
    public Type TriggerEventType => typeof(EventRoundStart);
    public HookResult Execute(GameEvent gameEvent) { return HookResult.Continue; }
    public void ParseCfg(Dictionary<string, string> cfg) { value = int.Parse(cfg["value"]); }
    public void Update() { }
    int value = 404;
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
}

public class StartHealth : ISuperPower
{
    public Type TriggerEventType => typeof(EventRoundStart);
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
    private int value = 404;
    public void ParseCfg(Dictionary<string, string> cfg) { value = int.Parse(cfg["value"]); }
}

public class StartArmor : ISuperPower
{
    public Type TriggerEventType => typeof(EventRoundStart);
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
    private int value = 404;
    public void ParseCfg(Dictionary<string, string> cfg) { value = int.Parse(cfg["value"]); }
}

public class InstantDefuse : ISuperPower
{
    public Type TriggerEventType => typeof(EventBombBegindefuse);
    public HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventBombBegindefuse)gameEvent;
        var player = realEvent.Userid;
        if (player != null && player.IsValid && player.PawnIsAlive)
        {
            if (!Users.Where(p => p.UserId == player.UserId).Any())
            {
                //Server.PrintToChatAll("player is not in list");
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
                //Server.PrintToChatAll($"Successful instant defuse by {player.PlayerName}");
            });
        }
        else
            Server.PrintToChatAll($"{PowerName} : player is null or not valid");
        return HookResult.Continue;
    }
    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
    public void ParseCfg(Dictionary<string, string> cfg) { }
    private string PowerName => this.GetType().ToString().Split(".").Last();
}

public class InstantPlant : ISuperPower
{
    public Type TriggerEventType => typeof(EventBombBeginplant);
    public HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventBombBeginplant)gameEvent;
        var player = realEvent.Userid;
        if (player != null && player.IsValid && player.PawnIsAlive)
        {
            if (!Users.Where(p => p.UserId == player.UserId).Any())
            {
                //Server.PrintToChatAll("player is not in list");
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
            Server.PrintToChatAll($"{PowerName} : player is null or not valid");

        return HookResult.Continue;
    }
    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
    public void ParseCfg(Dictionary<string, string> cfg) { }
    private string PowerName => this.GetType().ToString().Split(".").Last();
}

public class FoodSpawner : ISuperPower
{
    public Type TriggerEventType => typeof(EventRoundStart);
    public HookResult Execute(GameEvent gameEvent)
    {
        var realEvent = (EventRoundStart)gameEvent;
        foreach (var user in Users)
        {
            if (user == null || !user.IsValid)
            {
                Server.PrintToChatAll("user is null or not valid");
                continue;
            }
            var pawn = user.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
            {
                Server.PrintToChatAll("pawn is null or not valid");
                continue;
            }
            var prop = Utilities.CreateEntityByName<CPhysicsPropMultiplayer>("prop_physics_multiplayer");
            if (prop == null)
            {
                Server.PrintToChatAll("Failed to create prop");
                continue;
            }
            var pizza_id = TemUtils.RandomString(12);

            prop.Globalname = pizza_id;
            prop.SetModel("models/food/fruits/banana01a.vmdl");
            prop.Teleport(pawn.AbsOrigin, pawn.AbsRotation, pawn.AbsVelocity);

            if (prop.Collision != null && prop.Collision.EnablePhysics != 1)
            {
                Server.PrintToChatAll("prop.Collision.EnablePhysics != 1");
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
    public void ParseCfg(Dictionary<string, string> cfg) { }
    private string PowerName => this.GetType().ToString().Split(".").Last();
}

public class InfiniteAmmo : ISuperPower
{
    public Type TriggerEventType => typeof(EventWeaponFire);
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
            Server.PrintToChatAll($"{PowerName} : player is null or not valid");
        return HookResult.Continue;
    }
    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
    public void ParseCfg(Dictionary<string, string> cfg) { }
    private string PowerName => this.GetType().ToString().Split(".").Last();
}

public class SonicSpeed : ISuperPower
{
    public Type TriggerEventType => typeof(EventRoundStart);
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
    public void ParseCfg(Dictionary<string, string> cfg) { value = int.Parse(cfg["value"]); }
    private string PowerName => this.GetType().ToString().Split(".").Last();
    private int value = 404;
}

public class SteelHead : ISuperPower
{
    public Type TriggerEventType => typeof(EventPlayerHurt);
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
            pawn.Health += realEvent.DmgHealth;
            pawn.ArmorValue += realEvent.DmgArmor;
        }

        return HookResult.Continue;
    }
    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
    public void ParseCfg(Dictionary<string, string> cfg) { }
    private string PowerName => this.GetType().ToString().Split(".").Last();
}

public class InfiniteMoney : ISuperPower
{
    public Type TriggerEventType => typeof(EventRoundStart);
    public HookResult Execute(GameEvent gameEvent)
    {
        return HookResult.Continue;
    }
    public void ParseCfg(Dictionary<string, string> cfg) { }
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
    public Type TriggerEventType => typeof(EventGrenadeThrown);
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
    public void ParseCfg(Dictionary<string, string> cfg) { }
    public void Update() { }
    public List<CCSPlayerController> Users { get; set; } = new List<CCSPlayerController>();
}

public class EvilAura : ISuperPower
{
    public Type TriggerEventType => typeof(EventRoundStart);
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
                if (harm_pawn.Health <= 1)
                    continue;

                harm_pawn.Health -= (int)damage;
                Utilities.SetStateChanged(harm_pawn, "CBaseEntity", "m_iHealth");
                user.PrintToCenter($"You have harmed {player_to_harm.PlayerName} for {damage} damage");
                player_to_harm.PrintToCenter($"You have been hurt by {user.PlayerName}'s evil aura");
                if (harm_pawn.Health <= 0)
                    harm_pawn.Health = 1;
            }
        }
    }
    public void ParseCfg(Dictionary<string, string> cfg)
    {
        distance = float.Parse(cfg["distance"]);
        damage = float.Parse(cfg["damage"]);
        period = int.Parse(cfg["period"]);
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
    public Type TriggerEventType => typeof(EventRoundStart);
    public HookResult Execute(GameEvent gameEvent)
    {
        if (gameEvent.Handle == 0)
            return HookResult.Continue; // prevent recursive call

        var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;

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
    private Dictionary<int, HashSet<string>> dormant_power_rules = [];
    public void ParseCfg(Dictionary<string, string> cfg)
    {
        foreach (var entry_line in cfg)
        {
            var round_number = int.Parse(entry_line.Key);
            var rules = entry_line.Value;
            var power_commands = rules.Split(";").ToHashSet();

            dormant_power_rules.Add(round_number, power_commands);
        }
    }
}



