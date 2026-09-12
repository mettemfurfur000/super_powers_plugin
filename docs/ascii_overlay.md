# ASCII Overlay - Reusable TUI for CS2 Panorama

A reusable, project-agnostic ASCII/TUI overlay for CS2 that renders text-based UIs using PanoramaManager.

## Features

- **160×40 character grid** (configurable via CSS)
- **Monospace font** (Consolas) for perfect character alignment
- **Semi-transparent background** for overlay effect
- **Full-screen coverage** with z-index above game UI
- **Drawing primitives**: lines, boxes, progress bars, centered text
- **Per-player rendering** via dialog variables
- **Color variants** via CSS classes

## Quick Start

### 1. Include in Your Plugin

```csharp
using super_powers_plugin.src.hud;

public class MyPlugin : BasePlugin
{
    private AsciiOverlayManager? _overlay;

    public override void Load(bool hotReload)
    {
        _overlay = new AsciiOverlayManager();
        _overlay.Init(this);
    }

    public override void Unload(bool hotReload)
    {
        _overlay?.Shutdown();
    }
}
```

### 2. Basic Usage

```csharp
// Open overlay for a player
_overlay.Open(player);

// Clear screen
_overlay.ClearScreen(player);

// Draw text at position
_overlay.DrawTextAt(player, 10, 5, "Hello, World!");

// Draw centered text
_overlay.DrawCenteredText(player, 10, "CENTERED TEXT");

// Draw a box
_overlay.DrawBox(player, 5, 5, 30, 10, '*');

// Draw a progress bar
_overlay.DrawProgressBar(player, 10, 20, 40, 0.75f);

// Close overlay
_overlay.Close(player);
```

## API Reference

### Lifecycle

| Method | Description |
|--------|-------------|
| `Init(plugin)` | Initialize the overlay. Call once during plugin load. |
| `Shutdown()` | Shutdown and release resources. Call during plugin unload. |
| `Open(player)` | Open overlay for a player. |
| `Close(player)` | Close overlay for a player. |
| `Toggle(player)` | Toggle overlay visibility. |
| `IsOpen(player)` | Check if overlay is open for a player. |

### Drawing Primitives

| Method | Description |
|--------|-------------|
| `ClearScreen(player)` | Clear the entire screen. |
| `SetLine(player, row, text)` | Set a single line of text. |
| `SetLines(player, startRow, lines)` | Set multiple lines from array. |
| `SetScreen(player, lines)` | Set entire screen from string array. |
| `SetScreen(player, charArray)` | Set entire screen from 2D char array. |

### Drawing Operations

| Method | Description |
|--------|-------------|
| `DrawCharAt(player, x, y, char)` | Draw a single character. |
| `DrawTextAt(player, x, y, text)` | Draw text at position. |
| `DrawCenteredText(player, y, text)` | Draw centered text. |
| `DrawHorizontalLine(player, x, y, length, char)` | Draw horizontal line. |
| `DrawVerticalLine(player, x, y, length, char)` | Draw vertical line. |
| `DrawRect(player, x, y, width, height, char)` | Draw filled rectangle. |
| `DrawBox(player, x, y, width, height, char)` | Draw box outline. |
| `DrawTextBox(player, x, y, width, height, text[], char)` | Draw box with text inside. |
| `DrawProgressBar(player, x, y, width, progress, filled, empty)` | Draw progress bar. |

### Color Support

| Method | Description |
|--------|-------------|
| `SetLineColored(player, row, text, color)` | Set line with color. |

Available colors: `green`, `red`, `blue`, `gold`, `purple`, `white`, `gray`

### Grid Dimensions

```csharp
int width = _overlay.Width;   // 160
int height = _overlay.Height; // 40
```

## Example: Simple Menu

```csharp
public void ShowMenu(CCSPlayerController player)
{
    _overlay.Open(player);
    _overlay.ClearScreen(player);

    // Draw border
    _overlay.DrawBox(player, 20, 5, 60, 20, '+');

    // Draw title
    _overlay.DrawCenteredText(player, 7, "SUPER POWERS MENU");
    _overlay.DrawHorizontalLine(player, 21, 8, 58, '-');

    // Draw menu items
    _overlay.DrawTextAt(player, 22, 10, "1. Blood Fury");
    _overlay.DrawTextAt(player, 22, 12, "2. Radiation");
    _overlay.DrawTextAt(player, 22, 14, "3. Invisibility");
    _overlay.DrawTextAt(player, 22, 16, "4. Regeneration");

    // Draw footer
    _overlay.DrawHorizontalLine(player, 21, 22, 58, '-');
    _overlay.DrawCenteredText(player, 23, "Press 1-4 to select");

    // Add color to title
    _overlay.SetLineColored(player, 7, "SUPER POWERS MENU", "gold");
}
```

## Example: Status Display

```csharp
public void ShowStatus(CCSPlayerController player, float health, float armor, int money)
{
    _overlay.Open(player);

    // Health bar
    _overlay.DrawTextAt(player, 0, 0, "HEALTH:");
    _overlay.DrawProgressBar(player, 8, 0, 30, health / 100f);

    // Armor bar
    _overlay.DrawTextAt(player, 0, 1, "ARMOR:");
    _overlay.DrawProgressBar(player, 8, 1, 30, armor / 100f);

    // Money
    _overlay.DrawTextAt(player, 40, 0, $"MONEY: ${money}");
}
```

## Example: ASCII Art

```csharp
public void ShowAsciiArt(CCSPlayerController player)
{
    string[] art = [
        "    /\\_/\\  ",
        "   ( o.o ) ",
        "    > ^ <  ",
        "   /|   |\\",
        "  (_|   |_)",
    ];

    _overlay.Open(player);
    _overlay.SetLines(player, 10, art);
}
```

## CSS Customization

The overlay CSS is in `panorama/styles/custom_game/ascii_overlay.css`. You can customize:

- **Background color**: Change `.ascii-container` background-color
- **Font size**: Change `.ascii-line` font-size (affects grid density)
- **Colors**: Add new color variants in the CSS
- **Blur effect**: Uncomment `world-blur` in `.ascii-container` for frosted glass

## Technical Details

- **Grid**: 160 columns × 40 rows
- **Font**: Consolas (monospace)
- **Dialog variables**: `line_0` through `line_39`
- **CSS classes**: `.hidden`, `.show`, `.color-*`, `.bright-*`
- **Update rate**: Every 8 ticks (configurable in plugin)

## Limitations

1. **No dynamic panel creation** - All 40 lines must be pre-compiled in VXML
2. **No client resolution query** - Grid is fixed size, percentage-based positioning
3. **No JavaScript** - Server drives everything via dialog variables
4. **Monospace only** - Variable-width fonts won't align properly

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Text not aligned | Ensure using monospace font (Consolas) |
| Overlay not visible | Check z-index is high enough, verify workshop addon is mounted |
| Characters cut off | Adjust font-size in CSS to fit your resolution |
| Colors not working | Verify CSS classes are applied correctly |

## Workshop Addon

To distribute as a workshop addon:

1. Run `.\build_hud.ps1 -Workshop`
2. The compiled files will be in `content/csgo_addons/ascii_overlay/`
3. Use CS2 Workshop Tools to publish

## License

This is a reusable component. Feel free to use in other projects.
