using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Utils;

namespace super_powers_plugin.src.hud;

/// <summary>
/// A reusable, project-agnostic ASCII/TUI overlay manager.
/// Drives a 160x40 character grid via a CS2 custom_hud_layout entity
/// using the official CounterStrikeSharp API (no PanoramaManager dependency).
/// 
/// Usage:
///   var overlay = new AsciiOverlayManager();
///   overlay.Init(plugin);
///   overlay.Open(player);
///   overlay.SetLine(player, 0, "╔══════════════════════════════╗");
///   overlay.SetLine(player, 1, "║     ASCII OVERLAY DEMO      ║");
///   overlay.SetLine(player, 2, "╚══════════════════════════════╝");
///   overlay.Close(player);
///   overlay.Shutdown();
/// 
/// Grid: 160 columns × 40 rows
/// Font: Consolas (monospace)
/// Background: Semi-transparent black
/// </summary>
public class AsciiOverlayManager : IDisposable
{
    private super_powers_plugin? _plugin;
    private CCSCustomHudLayout? _layout;
    private bool _spawning;
    private const int GRID_WIDTH = 160;
    private const int GRID_HEIGHT = 40;
    private const string ROOT_PANEL = "AsciiOverlayRoot";
    private const string REVEAL_CLASS = "show";
    private const string LAYOUT_PATH = "panorama/layout/custom_game/ascii_overlay.vxml_c";

    private readonly HashSet<ulong> _openPlayers = [];
    private readonly Dictionary<ulong, string[]> _lineBuffers = [];

    /// <summary>
    /// Initialize the ASCII overlay.
    /// Must be called once during plugin load.
    /// Does NOT touch the engine: the layout entity is spawned lazily
    /// on first use or when the world is ready.
    /// </summary>
    public void Init(super_powers_plugin plugin)
    {
        _plugin = plugin;
        Console.WriteLine("[ASCII-Overlay] Manager initialized (official CCSCustomHudLayout API)");
    }

    /// <summary>
    /// Called when the server world is ready (EventServerSpawn) so the
    /// layout entity is (re)spawned on every map.
    /// </summary>
    public void OnWorldReady()
    {
        EnsureSpawned();
    }

    /// <summary>
    /// Ensure the custom_hud_layout entity exists. Reuses an already-spawned
    /// one; otherwise creates it with the layout path as a spawn keyvalue.
    /// </summary>
    private void EnsureSpawned()
    {
        if (_layout != null && _layout.IsValid) return;
        if (_spawning) return;
        _spawning = true;
        try
        {
            _layout = Utilities.FindAllEntitiesByDesignerName<CCSCustomHudLayout>("custom_hud_layout").FirstOrDefault();
            if (_layout != null && _layout.IsValid)
                return;

            var e = Utilities.CreateEntityByName<CCSCustomHudLayout>("custom_hud_layout");
            if (e == null || e.Handle == IntPtr.Zero)
            {
                Console.WriteLine("[ASCII-Overlay] ERROR: CreateEntityByName returned null");
                return;
            }

            var kv = new CEntityKeyValues();
            kv.SetVector("origin", 0f, 0f, 0f);
            kv.SetString("layout", LAYOUT_PATH);
            e.DispatchSpawn(kv);
            kv.Dispose();

            _layout = e;
            Console.WriteLine($"[ASCII-Overlay] custom_hud_layout spawned (index {e.Index})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ASCII-Overlay] EnsureSpawned FAILED: {ex}");
            _layout = null;
        }
        finally
        {
            _spawning = false;
        }
    }

    /// <summary>
    /// Returns true when the layout entity is alive (creating it if needed).
    /// </summary>
    private bool HasLayout()
    {
        EnsureSpawned();
        return _layout != null && _layout.IsValid;
    }

    /// <summary>
    /// Whether the engine has allocated per-player layout state for this slot.
    /// The managed per-player setters write into m_vecPlayerLayoutStates[slot];
    /// writing when that vector is empty/out-of-range is unsafe.
    /// </summary>
    private bool HasPlayerState(CCSPlayerController? player)
    {
        if (_layout == null || !_layout.IsValid) return false;
        if (player == null || !player.IsValid) return false;

        var states = _layout.PlayerLayoutStates;
        int slot = player.Slot;
        return slot >= 0 && states.Count > slot;
    }

    /// <summary>
    /// Write a dialog variable globally (shared text across all viewers).
    /// Matches the old "UseGlobalDialogVariables = true" behavior.
    /// </summary>
    private void SetVariable(string name, string value)
    {
        if (!HasLayout()) return;
        _layout!.SetDialogVariableString(ROOT_PANEL, name, value);
    }

    /// <summary>
    /// Toggle a class on a panel. Uses the per-player setter when the engine
    /// has per-player state for the slot; otherwise falls back to the global
    /// setter so the overlay still renders.
    /// </summary>
    private void SetClass(CCSPlayerController? player, string panel, string className, bool on)
    {
        if (!HasLayout()) return;
        if (HasPlayerState(player))
            _layout!.SetHasClassForPlayer(player!, panel, className, on);
        else
            _layout!.SetHasClass(panel, className, on);
    }

    /// <summary>
    /// Add the layout entity to a player's transmit set so their client
    /// actually receives and renders it. Called from the CheckTransmit listener.
    /// </summary>
    public void AddToTransmit(CCheckTransmitInfo info)
    {
        if (HasLayout())
            info.TransmitEntities.Add(_layout!);
    }

    /// <summary>
    /// Shutdown the ASCII overlay and release resources.
    /// Must be called during plugin unload.
    /// </summary>
    public void Shutdown()
    {
        if (_layout != null && _layout.IsValid)
        {
            try { _layout.Remove(); }
            catch (Exception ex) { Console.WriteLine($"[ASCII-Overlay] Remove FAILED: {ex}"); }
        }

        _layout = null;
        _openPlayers.Clear();
        _lineBuffers.Clear();
    }

    /// <summary>
    /// Toggle the overlay for a player.
    /// </summary>
    public void Toggle(CCSPlayerController player)
    {
        if (_openPlayers.Contains(player.SteamID))
            Close(player);
        else
            Open(player);
    }

    /// <summary>
    /// Open the overlay for a player.
    /// </summary>
    public void Open(CCSPlayerController player)
    {
        if (!HasLayout())
        {
            Console.WriteLine("[ASCII-Overlay] Cannot open: layout entity unavailable");
            return;
        }

        Console.WriteLine($"[ASCII-Overlay] Opening for {player.PlayerName}");
        _openPlayers.Add(player.SteamID);
        _lineBuffers[player.SteamID] = new string[GRID_HEIGHT];
        SetClass(player, ROOT_PANEL, REVEAL_CLASS, true);
        ClearScreen(player);
    }

    /// <summary>
    /// Close the overlay for a player.
    /// </summary>
    public void Close(CCSPlayerController player)
    {
        _openPlayers.Remove(player.SteamID);
        _lineBuffers.Remove(player.SteamID);
        SetClass(player, ROOT_PANEL, REVEAL_CLASS, false);
    }

    /// <summary>
    /// Check if the overlay is open for a player.
    /// </summary>
    public bool IsOpen(CCSPlayerController player)
    {
        return _openPlayers.Contains(player.SteamID);
    }

    /// <summary>
    /// Clear the entire screen for a player.
    /// </summary>
    public void ClearScreen(CCSPlayerController player)
    {
        if (!_openPlayers.Contains(player.SteamID)) return;

        for (int i = 0; i < GRID_HEIGHT; i++)
        {
            SetVariable($"line_{i}", string.Empty.PadRight(GRID_WIDTH));
        }

        if (_lineBuffers.ContainsKey(player.SteamID))
        {
            Array.Clear(_lineBuffers[player.SteamID]);
        }
    }

    /// <summary>
    /// Set a single line of text for a player.
    /// </summary>
    /// <param name="player">Target player</param>
    /// <param name="row">Row index (0-39)</param>
    /// <param name="text">Text to display (will be truncated or padded to GRID_WIDTH)</param>
    public void SetLine(CCSPlayerController player, int row, string text)
    {
        if (!_openPlayers.Contains(player.SteamID)) return;
        if (row < 0 || row >= GRID_HEIGHT) return;

        // Pad or truncate to exact width
        string padded = text.Length > GRID_WIDTH
            ? text.Substring(0, GRID_WIDTH)
            : text.PadRight(GRID_WIDTH);

        SetVariable($"line_{row}", padded);

        if (_lineBuffers.ContainsKey(player.SteamID))
        {
            _lineBuffers[player.SteamID][row] = padded;
        }
    }

    /// <summary>
    /// Set a single line with a CSS class for a player.
    /// </summary>
    public void SetLineWithClass(CCSPlayerController player, int row, string text, string cssClass, bool enable)
    {
        if (!_openPlayers.Contains(player.SteamID)) return;
        if (row < 0 || row >= GRID_HEIGHT) return;

        SetLine(player, row, text);
        SetClass(player, $"line_{row}", cssClass, enable);
    }

    /// <summary>
    /// Set a single line with a color variant for a player.
    /// </summary>
    public void SetLineColored(CCSPlayerController player, int row, string text, string color)
    {
        if (!_openPlayers.Contains(player.SteamID)) return;
        if (row < 0 || row >= GRID_HEIGHT) return;

        SetLine(player, row, text);

        // Clear all color classes first
        string[] colors = ["green", "red", "blue", "gold", "purple", "white", "gray"];
        foreach (var c in colors)
        {
            SetClass(player, $"line_{row}", $"color-{c}", false);
        }

        // Apply requested color
        if (!string.IsNullOrEmpty(color))
        {
            SetClass(player, $"line_{row}", $"color-{color}", true);
        }
    }

    /// <summary>
    /// Set multiple lines at once from an array.
    /// </summary>
    public void SetLines(CCSPlayerController player, int startRow, string[] lines)
    {
        if (!_openPlayers.Contains(player.SteamID)) return;

        for (int i = 0; i < lines.Length && (startRow + i) < GRID_HEIGHT; i++)
        {
            SetLine(player, startRow + i, lines[i]);
        }
    }

    /// <summary>
    /// Set the entire screen from a 2D character array.
    /// </summary>
    public void SetScreen(CCSPlayerController player, char[,] screen)
    {
        if (!_openPlayers.Contains(player.SteamID)) return;

        int rows = Math.Min(screen.GetLength(0), GRID_HEIGHT);
        int cols = Math.Min(screen.GetLength(1), GRID_WIDTH);

        for (int row = 0; row < rows; row++)
        {
            var sb = new System.Text.StringBuilder(GRID_WIDTH);
            for (int col = 0; col < cols; col++)
            {
                sb.Append(screen[row, col]);
            }
            // Pad remaining columns with spaces
            for (int col = cols; col < GRID_WIDTH; col++)
            {
                sb.Append(' ');
            }
            SetLine(player, row, sb.ToString());
        }
    }

    /// <summary>
    /// Set the entire screen from a string array (one string per line).
    /// </summary>
    public void SetScreen(CCSPlayerController player, string[] lines)
    {
        if (!_openPlayers.Contains(player.SteamID)) return;

        for (int row = 0; row < GRID_HEIGHT; row++)
        {
            if (row < lines.Length)
            {
                SetLine(player, row, lines[row]);
            }
            else
            {
                SetLine(player, row, string.Empty);
            }
        }
    }

    /// <summary>
    /// Draw a filled rectangle on the screen.
    /// </summary>
    public void DrawRect(CCSPlayerController player, int x, int y, int width, int height, char fillChar = ' ')
    {
        if (!_openPlayers.Contains(player.SteamID)) return;

        for (int row = y; row < y + height && row < GRID_HEIGHT; row++)
        {
            if (row < 0) continue;

            var sb = new System.Text.StringBuilder(GRID_WIDTH);
            for (int col = 0; col < GRID_WIDTH; col++)
            {
                if (col >= x && col < x + width)
                    sb.Append(fillChar);
                else if (_lineBuffers.ContainsKey(player.SteamID) && _lineBuffers[player.SteamID][row] != null)
                    sb.Append(_lineBuffers[player.SteamID][row][col]);
                else
                    sb.Append(' ');
            }
            SetLine(player, row, sb.ToString());
        }
    }

    /// <summary>
    /// Draw a box outline on the screen.
    /// </summary>
    public void DrawBox(CCSPlayerController player, int x, int y, int width, int height, char borderChar = '*')
    {
        if (!_openPlayers.Contains(player.SteamID)) return;

        // Top and bottom borders
        for (int col = x; col < x + width && col < GRID_WIDTH; col++)
        {
            if (col < 0) continue;

            if (y >= 0 && y < GRID_HEIGHT)
                DrawCharAt(player, col, y, borderChar);
            if (y + height - 1 >= 0 && y + height - 1 < GRID_HEIGHT)
                DrawCharAt(player, col, y + height - 1, borderChar);
        }

        // Side borders
        for (int row = y; row < y + height && row < GRID_HEIGHT; row++)
        {
            if (row < 0) continue;

            if (x >= 0 && x < GRID_WIDTH)
                DrawCharAt(player, x, row, borderChar);
            if (x + width - 1 >= 0 && x + width - 1 < GRID_WIDTH)
                DrawCharAt(player, x + width - 1, row, borderChar);
        }
    }

    /// <summary>
    /// Draw a single character at a specific position.
    /// </summary>
    public void DrawCharAt(CCSPlayerController player, int x, int y, char c)
    {
        if (!_openPlayers.Contains(player.SteamID)) return;
        if (x < 0 || x >= GRID_WIDTH || y < 0 || y >= GRID_HEIGHT) return;

        string line = _lineBuffers.ContainsKey(player.SteamID) && _lineBuffers[player.SteamID][y] != null
            ? _lineBuffers[player.SteamID][y]
            : new string(' ', GRID_WIDTH);

        if (x < line.Length)
        {
            line = line.Substring(0, x) + c + line.Substring(x + 1);
            SetLine(player, y, line);
        }
    }

    /// <summary>
    /// Draw text at a specific position.
    /// </summary>
    public void DrawTextAt(CCSPlayerController player, int x, int y, string text)
    {
        if (!_openPlayers.Contains(player.SteamID)) return;

        for (int i = 0; i < text.Length && (x + i) < GRID_WIDTH; i++)
        {
            if (x + i >= 0)
            {
                DrawCharAt(player, x + i, y, text[i]);
            }
        }
    }

    /// <summary>
    /// Draw a horizontal line.
    /// </summary>
    public void DrawHorizontalLine(CCSPlayerController player, int x, int y, int length, char c = '-')
    {
        DrawTextAt(player, x, y, new string(c, length));
    }

    /// <summary>
    /// Draw a vertical line.
    /// </summary>
    public void DrawVerticalLine(CCSPlayerController player, int x, int y, int length, char c = '|')
    {
        for (int i = 0; i < length; i++)
        {
            DrawCharAt(player, x, y + i, c);
        }
    }

    /// <summary>
    /// Draw a progress bar at a specific position.
    /// </summary>
    public void DrawProgressBar(CCSPlayerController player, int x, int y, int width, float progress, char filledChar = '=', char emptyChar = '-')
    {
        progress = Math.Clamp(progress, 0f, 1f);
        int filled = (int)(progress * (width - 2)); // -2 for brackets

        var sb = new System.Text.StringBuilder(width);
        sb.Append('[');
        for (int i = 0; i < width - 2; i++)
        {
            sb.Append(i < filled ? filledChar : emptyChar);
        }
        sb.Append(']');

        DrawTextAt(player, x, y, sb.ToString());
    }

    /// <summary>
    /// Draw a centered text line.
    /// </summary>
    public void DrawCenteredText(CCSPlayerController player, int y, string text)
    {
        int x = Math.Max(0, (GRID_WIDTH - text.Length) / 2);
        DrawTextAt(player, x, y, text);
    }

    /// <summary>
    /// Draw a box with text inside.
    /// </summary>
    public void DrawTextBox(CCSPlayerController player, int x, int y, int width, int height, string[] text, char borderChar = '*', char fillChar = ' ')
    {
        // Draw the box
        DrawBox(player, x, y, width, height, borderChar);

        // Fill interior with spaces
        for (int row = y + 1; row < y + height - 1 && row < GRID_HEIGHT; row++)
        {
            for (int col = x + 1; col < x + width - 1 && col < GRID_WIDTH; col++)
            {
                DrawCharAt(player, col, row, fillChar);
            }
        }

        // Draw text inside
        for (int i = 0; i < text.Length && i < height - 2; i++)
        {
            int textX = x + 1;
            int textY = y + 1 + i;
            string line = text[i].Length > width - 2
                ? text[i].Substring(0, width - 2)
                : text[i];
            DrawTextAt(player, textX, textY, line);
        }
    }

    /// <summary>
    /// Update all open players.
    /// </summary>
    public void UpdateAll()
    {
        if (!HasLayout()) return;

        foreach (var steamId in _openPlayers)
        {
            var player = Utilities.GetPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == steamId);
            if (player != null)
            {
                // Trigger a re-render by re-setting all lines
                if (_lineBuffers.ContainsKey(steamId))
                {
                    var lines = _lineBuffers[steamId];
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(lines[i]))
                        {
                            SetVariable($"line_{i}", lines[i]);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Get the grid dimensions.
    /// </summary>
    public int Width => GRID_WIDTH;
    public int Height => GRID_HEIGHT;

    public void Dispose()
    {
        Shutdown();
    }
}