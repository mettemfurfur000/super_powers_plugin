using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using PanoramaManager;

namespace super_powers_plugin.src.hud;

/// <summary>
/// A reusable, project-agnostic ASCII/TUI overlay manager.
/// Drives a 160x40 character grid via PanoramaManager dialog variables.
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
    private PanelHandle? _overlay;
    private super_powers_plugin? _plugin;
    private const int GRID_WIDTH = 160;
    private const int GRID_HEIGHT = 40;
    private const string LAYOUT_PATH = "panorama/layout/custom_game/ascii_overlay.vxml_c";

    private readonly HashSet<ulong> _openPlayers = [];
    private readonly Dictionary<ulong, string[]> _lineBuffers = [];

    /// <summary>
    /// Initialize the ASCII overlay.
    /// Must be called once during plugin load.
    /// </summary>
    public void Init(super_powers_plugin plugin)
    {
        _plugin = plugin;

        // Panorama.Init() is now called once in main.cs Load()
        // Just spawn the panel
        try
        {
            _overlay = Panorama.Spawn(
                LAYOUT_PATH,
                new LayoutContract
                {
                    RootPanelId = "AsciiOverlayRoot",
                    RevealClass = "show",
                    CaptureInput = false,
                });

            Console.WriteLine($"[ASCII-Overlay] Panorama.Spawn OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ASCII-Overlay] Panorama.Spawn FAILED: {ex}");
            return;
        }

        if (_overlay != null)
        {
            Console.WriteLine("[ASCII-Overlay] Spawned panel OK");
        }
        else
        {
            Console.WriteLine("[ASCII-Overlay] ERROR: _overlay is null after Spawn");
        }
    }

    /// <summary>
    /// Shutdown the ASCII overlay and release resources.
    /// Must be called during plugin unload.
    /// </summary>
    public void Shutdown()
    {
        if (_overlay != null)
        {
            _overlay.Dispose();
            _overlay = null;
        }

        // Don't call Panorama.Shutdown() here - let the main plugin handle it
        // since HudManager might still be using Panorama
        _openPlayers.Clear();
        _lineBuffers.Clear();
    }

    /// <summary>
    /// Toggle the overlay for a player.
    /// </summary>
    public void Toggle(CCSPlayerController player)
    {
        if (_overlay == null) return;

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
        if (_overlay == null)
        {
            Console.WriteLine("[ASCII-Overlay] Cannot open: _overlay is null");
            return;
        }

        Console.WriteLine($"[ASCII-Overlay] Opening for {player.PlayerName}");
        _openPlayers.Add(player.SteamID);
        _lineBuffers[player.SteamID] = new string[GRID_HEIGHT];
        _overlay.Open(player);
        ClearScreen(player);
    }

    /// <summary>
    /// Close the overlay for a player.
    /// </summary>
    public void Close(CCSPlayerController player)
    {
        if (_overlay == null) return;

        _openPlayers.Remove(player.SteamID);
        _lineBuffers.Remove(player.SteamID);
        _overlay.Close(player);
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
        if (_overlay == null || !_openPlayers.Contains(player.SteamID)) return;

        for (int i = 0; i < GRID_HEIGHT; i++)
        {
            _overlay.SetVariableFor(player, $"line_{i}", string.Empty.PadRight(GRID_WIDTH));
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
        if (_overlay == null || !_openPlayers.Contains(player.SteamID)) return;
        if (row < 0 || row >= GRID_HEIGHT) return;

        // Pad or truncate to exact width
        string padded = text.Length > GRID_WIDTH
            ? text.Substring(0, GRID_WIDTH)
            : text.PadRight(GRID_WIDTH);

        _overlay.SetVariableFor(player, $"line_{row}", padded);

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
        if (_overlay == null || !_openPlayers.Contains(player.SteamID)) return;
        if (row < 0 || row >= GRID_HEIGHT) return;

        SetLine(player, row, text);
        _overlay.SetClassFor(player, $"line_{row}", cssClass, enable);
    }

    /// <summary>
    /// Set a single line with a color variant for a player.
    /// </summary>
    public void SetLineColored(CCSPlayerController player, int row, string text, string color)
    {
        if (_overlay == null || !_openPlayers.Contains(player.SteamID)) return;
        if (row < 0 || row >= GRID_HEIGHT) return;

        SetLine(player, row, text);

        // Clear all color classes first
        string[] colors = ["green", "red", "blue", "gold", "purple", "white", "gray"];
        foreach (var c in colors)
        {
            _overlay.SetClassFor(player, $"line_{row}", $"color-{c}", false);
        }

        // Apply requested color
        if (!string.IsNullOrEmpty(color))
        {
            _overlay.SetClassFor(player, $"line_{row}", $"color-{color}", true);
        }
    }

    /// <summary>
    /// Set multiple lines at once from an array.
    /// </summary>
    public void SetLines(CCSPlayerController player, int startRow, string[] lines)
    {
        if (_overlay == null || !_openPlayers.Contains(player.SteamID)) return;

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
        if (_overlay == null || !_openPlayers.Contains(player.SteamID)) return;

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
        if (_overlay == null || !_openPlayers.Contains(player.SteamID)) return;

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
        if (_overlay == null || !_openPlayers.Contains(player.SteamID)) return;

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
        if (_overlay == null || !_openPlayers.Contains(player.SteamID)) return;

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
        if (_overlay == null || !_openPlayers.Contains(player.SteamID)) return;
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
        if (_overlay == null || !_openPlayers.Contains(player.SteamID)) return;

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
        if (_overlay == null) return;

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
                            _overlay.SetVariableFor(player, $"line_{i}", lines[i]);
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
