using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using PanoramaManager;
using super_powers_plugin.src;

namespace super_powers_plugin.src.hud;

public class SuperPowerHudManager : IDisposable
{
    private PanelHandle? _hud;
    private super_powers_plugin? _plugin;
    private const int MAX_POWER_SLOTS = 8;
    private const int UPDATE_INTERVAL = 8;

    private readonly HashSet<ulong> _openPlayers = [];

    public void Init(super_powers_plugin plugin)
    {
        _plugin = plugin;

        // Panorama.Init() is now called once in main.cs Load()
        // Just spawn the panel
        try
        {
            _hud = Panorama.Spawn(
                "panorama/layout/custom_game/super_powers_hud.vxml_c",
                new LayoutContract
                {
                    RootPanelId = "PanoramaRoot",
                    RevealClass = "show",
                    CaptureInput = false,
                });

            Console.WriteLine($"[SP-HUD] Panorama.Spawn OK. _hud is null={_hud == null}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SP-HUD] Panorama.Spawn FAILED: {ex}");
            return;
        }

        if (_hud != null)
        {
            _hud.OnEvent += OnHudEvent;
            Console.WriteLine($"[SP-HUD] Spawned panel OK");
        }
        else
        {
            Console.WriteLine("[SP-HUD] ERROR: _hud is null after Spawn");
        }
    }

    public void Shutdown()
    {
        if (_hud != null)
        {
            _hud.OnEvent -= OnHudEvent;
            _hud.Dispose();
            _hud = null;
        }

        Panorama.Shutdown();
        _openPlayers.Clear();
    }

    public void Toggle(CCSPlayerController player)
    {
        if (_hud == null) return;

        if (_openPlayers.Contains(player.SteamID))
            Close(player);
        else
            Open(player);
    }

    public void Open(CCSPlayerController player)
    {
        if (_hud == null) {
            Console.WriteLine("[SP-HUD] Cannot open: _hud is null");
            return;
        }

        Console.WriteLine($"[SP-HUD] Opening for {player.PlayerName} (steamid={player.SteamID})");
        _openPlayers.Add(player.SteamID);
        _hud.Open(player);
        UpdatePlayer(player);
    }

    public void Close(CCSPlayerController player)
    {
        if (_hud == null) return;

        _openPlayers.Remove(player.SteamID);
        _hud.Close(player);
    }

    public void UpdateAll()
    {
        if (_hud == null) return;

        foreach (var steamId in _openPlayers)
        {
            var player = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == steamId);
            if (player != null)
                UpdatePlayer(player);
        }
    }

    public void UpdatePlayer(CCSPlayerController player)
    {
        if (_hud == null || !_openPlayers.Contains(player.SteamID)) return;

        var powers = SuperPowerController.GetPowers()
            .Where(p => p.IsUser(player))
            .ToList();

        Console.WriteLine($"[SP-HUD] UpdatePlayer {player.PlayerName}: {powers.Count} powers found");

        int slot = 0;
        for (int i = 0; i < powers.Count && slot < MAX_POWER_SLOTS; i++)
        {
            var state = powers[i].GetHudState(player);
            if (!state.IsActive) continue;

            string prefix = slot switch
            {
                0 => "[1]",
                1 => "[2]",
                2 => "[3]",
                3 => "[4]",
                4 => "[5]",
                5 => "[6]",
                6 => "[7]",
                7 => "[8]",
                _ => ""
            };

            _hud.SetVariableFor(player, $"power{slot}_name", $"{prefix} {state.Name}");
            _hud.SetVariableFor(player, $"power{slot}_bar", state.Bar);
            _hud.SetVariableFor(player, $"power{slot}_info", state.Info);
            _hud.SetClassFor(player, $"power{slot}", "hidden", false);

            string rarityClass = state.Rarity.ToLowerInvariant();
            _hud.SetClassFor(player, $"power{slot}", "rarity-common", rarityClass == "common");
            _hud.SetClassFor(player, $"power{slot}", "rarity-uncommon", rarityClass == "uncommon");
            _hud.SetClassFor(player, $"power{slot}", "rarity-rare", rarityClass == "rare");
            _hud.SetClassFor(player, $"power{slot}", "rarity-legendary", rarityClass == "legendary");

            if (state.ShowButton)
            {
                _hud.SetClassFor(player, $"power{slot}_btn", "hidden", false);
                _hud.SetVariableFor(player, $"power{slot}_action", state.ActionText);
            }
            else
            {
                _hud.SetClassFor(player, $"power{slot}_btn", "hidden", true);
            }

            slot++;
        }

        for (int i = slot; i < MAX_POWER_SLOTS; i++)
        {
            _hud.SetClassFor(player, $"power{i}", "hidden", true);
        }

        _hud.SetVariableFor(player, "sp_title", "SUPER POWERS");
        _hud.SetVariableFor(player, "sp_tag", $"{powers.Count} active");

        if (powers.Count == 0)
        {
            _hud.SetVariableFor(player, "sp_hint", "no powers active");
        }
        else
        {
            _hud.SetVariableFor(player, "sp_hint", "sp_hud to toggle");
        }
    }

    private void OnHudEvent(PanelEvent e)
    {
        if (e.Action == PanelAction.Close)
        {
            _openPlayers.Remove(e.Player.SteamID);
            return;
        }

        if (e.Action != PanelAction.Click) return;

        if (string.IsNullOrEmpty(e.ElementId)) return;

        if (e.ElementId.EndsWith("_btn"))
        {
            var slotStr = e.ElementId.Replace("power", "").Replace("_btn", "");
            if (!int.TryParse(slotStr, out int slot)) return;

            var powers = SuperPowerController.GetPowers()
                .Where(p => p.IsUser(e.Player))
                .ToList();

            int activeSlot = 0;
            foreach (var power in powers)
            {
                var state = power.GetHudState(e.Player);
                if (!state.IsActive) continue;

                if (activeSlot == slot)
                {
                    power.OnHudButton(e.Player);
                    break;
                }
                activeSlot++;
            }
        }
    }

    public static string BuildBar(float progress, int width = 16)
    {
        progress = Math.Clamp(progress, 0f, 1f);
        int filled = (int)(progress * width);

        var sb = new System.Text.StringBuilder(width + 4);
        sb.Append('[');
        for (int i = 0; i < width; i++)
            sb.Append(i < filled ? '\u2588' : '\u2591');
        sb.Append(']');
        sb.Append($" {progress * 100:F0}%");
        return sb.ToString();
    }

    public void Dispose()
    {
        Shutdown();
    }
}
