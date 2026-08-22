using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

/// Tracks players' inventories by polling, since EventItemRemove does not fire reliably.
/// Reports weapons leaving an inventory together with their last known clip count.
public class InventoryTracker
{
    public delegate void WeaponRemovedHandler(CCSPlayerController player, string designerName, int lastClip1);

    /// Fired for each weapon that left a watched player's inventory since the last Update.
    /// lastClip1 is the magazine count the tracker saw right before removal.
    public event WeaponRemovedHandler? WeaponRemoved;

    private sealed class WeaponRecord
    {
        public string DesignerName = "";
        public int Clip1;
    }

    private sealed class PlayerState
    {
        public Dictionary<int, WeaponRecord> Weapons = [];
        public bool WasAlive;
    }

    // more simultaneous removals than this = respawn/round restart, not a drop
    public int cfg_resetThreshold = 2;

    private readonly Dictionary<CCSPlayerController, PlayerState> states = [];

    /// Call as often as possible (every tick is fine), removals are diffed between calls
    public void Update()
    {
        foreach (var entry in states.ToList())
        {
            var player = entry.Key;
            var state = entry.Value;

            if (player == null)
                continue;

            if (!player.IsValid || player.Connected != PlayerConnectedState.Connected)
            {
                states.Remove(player);
                continue;
            }

            var pawn = player.PlayerPawn.Value;
            bool aliveNow = pawn != null && pawn.IsValid && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE;
            var current = CollectCurrent(pawn);

            // dead or not spawned yet - silently refresh so loadout changes while dead never fire events
            if (!aliveNow)
            {
                state.Weapons = current;
                state.WasAlive = false;
                continue;
            }

            // first alive poll after respawn - just establish a baseline
            if (!state.WasAlive)
            {
                state.Weapons = current;
                state.WasAlive = true;
                continue;
            }

            var removed = state.Weapons.Keys.Where(i => !current.ContainsKey(i)).ToList();

            // whole loadout changed - treat as round restart/rebuy, not individual drops
            if (removed.Count > cfg_resetThreshold)
            {
                state.Weapons = current;
                continue;
            }

            foreach (var index in removed)
                WeaponRemoved?.Invoke(player, state.Weapons[index].DesignerName, state.Weapons[index].Clip1);

            state.Weapons = current;
        }
    }

    private static Dictionary<int, WeaponRecord> CollectCurrent(CCSPlayerPawn? pawn)
    {
        Dictionary<int, WeaponRecord> current = [];
        var myWeapons = pawn?.WeaponServices?.MyWeapons;
        if (myWeapons == null)
            return current;

        foreach (var handle in myWeapons)
        {
            var weapon = handle.Value;
            if (weapon == null || !weapon.IsValid)
                continue;

            current[(int)weapon.Index] = new WeaponRecord
            {
                DesignerName = weapon.DesignerName ?? "",
                Clip1 = weapon.Clip1
            };
        }

        return current;
    }

    public void Watch(CCSPlayerController player)
    {
        if (states.ContainsKey(player))
            return;

        states[player] = new PlayerState
        {
            Weapons = CollectCurrent(player.PlayerPawn.Value),
            WasAlive = false
        };
    }

    public void Forget(CCSPlayerController player) => states.Remove(player);

    public void Clear() => states.Clear();
}
