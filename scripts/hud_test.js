import { Instance } from "cs_script/point_script";

// ─── Constants ───────────────────────────────────────────────────────────────
const GRID_W = 160;
const GRID_H = 40;
// The panelId for SetDialogVariableString is the root Panel's id from the layout.
// In ascii_overlay.xml: <Panel id="AsciiOverlayRoot" class="ascii-container">
const ROOT_PANEL = "AsciiOverlayRoot";

// ─── State ───────────────────────────────────────────────────────────────────
let hud = null;
let currentTest = 0;
const TEST_COUNT = 13;
const TEST_INTERVAL = 5.0; // seconds between auto-cycling tests
// Local line buffer — JS can't read back dialog variables, so we track state here.
const lineBuf = [];
for (let i = 0; i < GRID_H; i++) lineBuf[i] = " ".repeat(GRID_W);

// ─── HudHelper ───────────────────────────────────────────────────────────────
// Mirrors the C# AsciiOverlayManager API, but drives custom_hud_layout
// via SetDialogVariableString / SetDialogVariableStringForPlayer.

function setLine(row, text) {
    if (!hud || row < 0 || row >= GRID_H) return;
    const padded = text.length > GRID_W
        ? text.substring(0, GRID_W)
        : text.padEnd(GRID_W, " ");
    lineBuf[row] = padded;
    hud.SetDialogVariableString(ROOT_PANEL, `line_${row}`, padded);
}

function setLineForPlayer(slot, row, text) {
    if (!hud || row < 0 || row >= GRID_H) return;
    const padded = text.length > GRID_W
        ? text.substring(0, GRID_W)
        : text.padEnd(GRID_W, " ");
    hud.SetDialogVariableStringForPlayer(slot, ROOT_PANEL, `line_${row}`, padded);
}

function clearScreen() {
    for (let r = 0; r < GRID_H; r++) {
        lineBuf[r] = " ".repeat(GRID_W);
        setLine(r, "");
    }
}

function drawTextAt(x, y, text) {
    if (!hud || y < 0 || y >= GRID_H || x < 0 || x >= GRID_W || !text) return;
    // Splice text into the line buffer at position x
    let line = lineBuf[y];
    const before = x > 0 ? line.substring(0, x) : "";
    const after = x + text.length < GRID_W ? line.substring(x + text.length) : "";
    line = (before + text + after).substring(0, GRID_W).padEnd(GRID_W, " ");
    lineBuf[y] = line;
    setLine(y, line);
}

function drawCenteredText(y, text) {
    const x = Math.max(0, Math.floor((GRID_W - text.length) / 2));
    drawTextAt(x, y, text);
}

function drawCharAt(x, y, ch) {
    drawTextAt(x, y, ch);
}

function drawHorizontalLine(x, y, length, ch = "-") {
    drawTextAt(x, y, ch.repeat(length));
}

function drawVerticalLine(x, y, length, ch = "|") {
    for (let i = 0; i < length; i++) {
        drawCharAt(x, y + i, ch);
    }
}

function drawProgressBar(x, y, width, progress, filledCh = "=", emptyCh = "-") {
    progress = Math.max(0, Math.min(1, progress));
    const inner = width - 2; // -2 for brackets
    const filled = Math.floor(progress * inner);
    let bar = "[";
    for (let i = 0; i < inner; i++) bar += i < filled ? filledCh : emptyCh;
    bar += "]";
    drawTextAt(x, y, bar);
}

function drawBox(x, y, width, height, borderCh = "*") {
    // Top border
    drawTextAt(x, y, borderCh.repeat(width));
    // Side borders
    for (let row = 1; row < height - 1; row++) {
        let line = lineBuf[y + row] || " ".repeat(GRID_W);
        // Replace chars at x and x+width-1
        const chars = line.split("");
        if (x < GRID_W) chars[x] = borderCh;
        if (x + width - 1 < GRID_W) chars[x + width - 1] = borderCh;
        line = chars.join("").substring(0, GRID_W);
        lineBuf[y + row] = line;
        setLine(y + row, line);
    }
    // Bottom border
    drawTextAt(x, y + height - 1, borderCh.repeat(width));
}

function drawTextBox(x, y, width, height, lines, borderCh = "*") {
    drawBox(x, y, width, height, borderCh);
    for (let i = 0; i < lines.length && i < height - 2; i++) {
        const text = lines[i].substring(0, width - 2);
        drawTextAt(x + 1, y + 1 + i, text);
    }
}

function drawRect(x, y, width, height, fillCh = " ") {
    for (let row = 0; row < height; row++) {
        drawTextAt(x, y + row, fillCh.repeat(width));
    }
}

// ─── Test Scenarios ──────────────────────────────────────────────────────────

function test0_BasicText() {
    setLine(0, "TEST 0: BASIC TEXT RENDERING".padEnd(GRID_W, " "));
    drawHorizontalLine(0, 1, 160, "-");

    drawTextAt(0, 3, "Position (0,3): Left-aligned text");
    drawTextAt(50, 4, "Position (50,4): Middle of screen");
    drawTextAt(100, 5, "Position (100,5): Right side");

    drawCenteredText(7, "CENTERED TEXT AT ROW 7");
    drawCenteredText(8, "Another centered line");

    drawCharAt(0, 10, "A");
    drawCharAt(79, 10, "M");
    drawCharAt(159, 10, "Z");

    drawVerticalLine(40, 3, 10, "|");
    drawVerticalLine(120, 3, 10, "|");

    setLine(12, "Single chars: A at (0,10), M at (79,10), Z at (159,10)".padEnd(GRID_W, " "));
    setLine(13, "Vertical lines drawn at columns 40 and 120".padEnd(GRID_W, " "));
}

function test1_DrawingPrimitives() {
    setLine(0, "TEST 1: DRAWING PRIMITIVES".padEnd(GRID_W, " "));
    drawHorizontalLine(0, 1, 160, "-");

    drawHorizontalLine(5, 3, 40, "-");
    drawHorizontalLine(5, 4, 40, "=");
    drawHorizontalLine(5, 5, 40, "#");
    setLine(6, "Horizontal lines: '-', '=', '#'".padEnd(GRID_W, " "));

    drawVerticalLine(5, 8, 8, "|");
    drawVerticalLine(10, 8, 8, "!");
    drawVerticalLine(15, 8, 8, "+");
    setLine(17, "Vertical lines: '|', '!', '+'".padEnd(GRID_W, " "));

    drawBox(50, 3, 30, 10, "*");
    drawTextAt(52, 5, "BOX INSIDE");
    drawTextAt(52, 7, "Text at (52,7)");
    setLine(14, "Box outline at (50,3) 30x10 with '*' border".padEnd(GRID_W, " "));

    drawRect(90, 3, 30, 10, ".");
    setLine(15, "Filled rectangle at (90,3) 30x10 with '.' fill".padEnd(GRID_W, " "));

    drawBox(50, 16, 70, 8, "@");
    drawTextAt(52, 18, "Different border char '@'");
    drawTextAt(52, 20, "This box is 70 wide, 8 tall");
}

function test2_ProgressBars() {
    setLine(0, "TEST 2: PROGRESS BARS".padEnd(GRID_W, " "));
    drawHorizontalLine(0, 1, 160, "-");

    const progresses = [0, 0.1, 0.25, 0.33, 0.5, 0.66, 0.75, 0.9, 1.0];
    for (let i = 0; i < progresses.length; i++) {
        const y = 3 + i;
        drawTextAt(0, y, `${(progresses[i] * 100).toFixed(0)}%`.padStart(5));
        drawProgressBar(8, y, 50, progresses[i]);
    }
    setLine(13, "Progress bars from 0% to 100%".padEnd(GRID_W, " "));

    setLine(15, "Custom bar styles:".padEnd(GRID_W, " "));
    drawProgressBar(5, 16, 40, 0.75, "=", "-");
    drawTextAt(48, 16, "Default [=------]");
    drawProgressBar(5, 17, 40, 0.75, "#", " ");
    drawTextAt(48, 17, "Solid [#######   ]");
    drawProgressBar(5, 18, 40, 0.75, "*", ".");
    drawTextAt(48, 18, "Dotted [***.......]");

    drawProgressBar(5, 20, 80, 0.65);
    setLine(21, "Wide bar (80 chars) at 65%".padEnd(GRID_W, " "));

    drawProgressBar(5, 22, 20, 0.45);
    setLine(23, "Narrow bar (20 chars) at 45%".padEnd(GRID_W, " "));
}

function test3_Colors() {
    setLine(0, "TEST 3: COLOR VARIANTS".padEnd(GRID_W, " "));
    drawHorizontalLine(0, 1, 160, "-");

    const colors = ["green", "red", "blue", "gold", "purple", "white", "gray"];
    for (let i = 0; i < colors.length; i++) {
        setLine(3 + i, `Color: ${colors[i].toUpperCase()} - The quick brown fox jumps over the lazy dog`.padEnd(GRID_W, " "));
    }

    setLine(11, "BRIGHT VARIANTS:".padEnd(GRID_W, " "));
    const brightColors = ["green", "red", "blue", "gold"];
    for (let i = 0; i < brightColors.length; i++) {
        setLine(12 + i, `Bright: ${brightColors[i].toUpperCase()} - ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789`.padEnd(GRID_W, " "));
    }

    setLine(20, "COLOR SWATCHES (block characters):".padEnd(GRID_W, " "));
    const block = "\u2588\u2588\u2588\u2588\u2588\u2588\u2588\u2588";
    for (let i = 0; i < colors.length; i++) {
        const x = 2 + (i * 20);
        drawTextAt(x, 21, block);
    }
}

function test4_BoxWithText() {
    setLine(0, "TEST 4: BOXES WITH TEXT".padEnd(GRID_W, " "));
    drawHorizontalLine(0, 1, 160, "-");

    drawTextBox(5, 3, 25, 7, ["Hello!", "This is", "inside a box"]);
    drawTextBox(35, 3, 25, 7, ["Custom", "Border", "Character"], "#");
    drawTextBox(65, 3, 60, 6, ["This is a wider box with more text inside", "It spans multiple lines and is 60 characters wide"], "+");

    drawBox(5, 12, 30, 8, "-");
    setLine(21, "Empty box at (5,12) 30x8".padEnd(GRID_W, " "));

    drawTextBox(40, 12, 30, 5, ["This text is definitely longer than the box width and should be truncated"], "*");
    setLine(18, "Box with overflow text (truncated)".padEnd(GRID_W, " "));

    drawBox(5, 24, 50, 12, "@");
    drawBox(8, 26, 44, 8, "#");
    drawTextAt(10, 28, "Nested boxes!");
    setLine(37, "Nested boxes (outer '@', inner '#')".padEnd(GRID_W, " "));
}

function test5_FullScreenFill() {
    setLine(0, "TEST 5: FULL SCREEN FILL".padEnd(GRID_W, " "));

    for (let row = 0; row < GRID_H; row++) {
        let line = "";
        for (let col = 0; col < GRID_W; col++) {
            line += (row + col) % 2 === 0 ? "\u2588" : " ";
        }
        setLine(row, line);
    }

    drawCenteredText(19, "FULL SCREEN CHECKERBOARD PATTERN");
    drawCenteredText(20, "160x40 grid coverage test");
}

function test6_EdgeCases() {
    setLine(0, "TEST 6: EDGE CASES".padEnd(GRID_W, " "));
    drawHorizontalLine(0, 1, 160, "-");

    setLine(3, "".padEnd(GRID_W, " "));
    setLine(4, "Empty string at row 3 (should be blank)".padEnd(GRID_W, " "));

    setLine(6, "X");
    setLine(7, "Single char 'X' at start of row 6".padEnd(GRID_W, " "));

    setLine(9, "A".repeat(200).substring(0, GRID_W));
    setLine(10, "200 chars 'A' truncated to 160 at row 9".padEnd(GRID_W, " "));

    // Out of bounds (should be ignored)
    drawCharAt(-1, 12, "X");
    drawCharAt(200, 12, "X");
    drawTextAt(-10, 13, "This should not appear");
    drawTextAt(170, 14, "This should not appear");
    setLine(15, "Out-of-bounds draws were ignored".padEnd(GRID_W, " "));

    drawBox(150, 12, 20, 8, "+");
    setLine(21, "Box at edge (150,12) - should clip".padEnd(GRID_W, " "));

    drawBox(200, 25, 10, 5, "*");
    setLine(22, "Box at (200,25) - off-screen, nothing visible".padEnd(GRID_W, " "));

    drawTextAt(5, 24, "Unicode: \u2588\u2591\u2592\u2593 \u2190\u2191\u2192\u2193 \u2605\u2606");
    setLine(25, "Unicode block chars and arrows at row 24".padEnd(GRID_W, " "));

    drawTextAt(5, 27, "T   e   s   t       w   i   t   h       s   p   a   c   e   s");
    setLine(28, "Text with intentional spacing at row 27".padEnd(GRID_W, " "));
}

function test7_AsciiArt() {
    setLine(0, "TEST 7: ASCII ART".padEnd(GRID_W, " "));
    drawHorizontalLine(0, 1, 160, "-");

    const cat = [
        "    /\\_/\\  ",
        "   ( o.o ) ",
        "    > ^ <  ",
        "   /|   |\\",
        "  (_|   |_)",
    ];
    for (let i = 0; i < cat.length; i++) setLine(3 + i, cat[i]);
    setLine(9, "ASCII Cat at (0,3)".padEnd(GRID_W, " "));

    const house = [
        "       /\\       ",
        "      /  \\      ",
        "     /    \\     ",
        "    /      \\    ",
        "   /________\\   ",
        "   |  ____  |   ",
        "   | |    | |   ",
        "   | |____| |   ",
        "   |________|   ",
    ];
    for (let i = 0; i < house.length; i++) setLine(3 + i, house[i]);
    setLine(13, "ASCII House at (0,3)".padEnd(GRID_W, " "));

    const robot = [
        "  _____  ",
        " |     | ",
        " | o o | ",
        " |  ^  | ",
        " | \\_/ | ",
        " |_____| ",
    ];
    for (let i = 0; i < robot.length; i++) setLine(3 + i, robot[i]);
    setLine(10, "ASCII Robot at (0,3)".padEnd(GRID_W, " "));

    const arrow = [
        "      |      ",
        "      |      ",
        "      |      ",
        "  ____|____  ",
        " |         | ",
        " |  FWD -> | ",
        " |_________| ",
    ];
    for (let i = 0; i < arrow.length; i++) setLine(15 + i, arrow[i]);
    setLine(23, "ASCII Arrow at (0,15)".padEnd(GRID_W, " "));
}

function test8_MenuSimulation() {
    setLine(0, "TEST 8: MENU SIMULATION".padEnd(GRID_W, " "));
    drawHorizontalLine(0, 1, 160, "-");

    drawBox(30, 3, 100, 30, "+");

    drawCenteredText(5, "SUPER POWERS - MAIN MENU");
    drawHorizontalLine(31, 6, 98, "=");

    const menuItems = [
        "[1] Blood Fury      - Kills grant stacking damage/speed bonuses",
        "[2] Radiation       - 1 dmg/s to enemies in sight",
        "[3] Invisibility    - You are nearly invisible",
        "[4] Regeneration    - Regenerate 10 HP/s while below 75",
        "[5] Bitcoin Miner   - Random money ticks",
        "[6] Speedy Fella    - Increased walking speed",
        "[7] Golden Bullet   - Kill reward when using last bullet",
        "[8] Evil Aura       - Slowly harm nearby enemies",
    ];
    for (let i = 0; i < menuItems.length; i++) {
        drawTextAt(33, 8 + i, menuItems[i]);
    }

    drawHorizontalLine(31, 17, 98, "=");
    drawTextAt(33, 19, "Press 1-8 to select");
    drawTextAt(33, 20, "Press ESC to cancel");

    drawBox(30, 25, 100, 5, "#");
    drawTextAt(32, 27, "Health: 100  Armor: 100  Money: $16000");
    drawTextAt(32, 28, "Current Power: None");
}

function test9_StatusDisplay() {
    setLine(0, "TEST 9: STATUS DISPLAY".padEnd(GRID_W, " "));
    drawHorizontalLine(0, 1, 160, "-");

    // Player status panel
    drawBox(2, 3, 50, 15, "|");
    drawTextAt(4, 4, "PLAYER STATUS");
    drawHorizontalLine(3, 5, 48, "-");
    drawTextAt(4, 7, "Name: Tem");
    drawTextAt(4, 8, "Health:");
    drawProgressBar(12, 8, 30, 0.75);
    drawTextAt(44, 8, "75/100");
    drawTextAt(4, 9, "Armor:");
    drawProgressBar(12, 9, 30, 0.5);
    drawTextAt(44, 9, "50/100");
    drawTextAt(4, 10, "Money: $12,500");
    drawTextAt(4, 11, "Kills: 5  Deaths: 3");
    drawTextAt(4, 12, "K/D: 1.67");

    // Power status panel
    drawBox(55, 3, 50, 15, "|");
    drawTextAt(57, 4, "POWER STATUS");
    drawHorizontalLine(56, 5, 48, "-");
    drawTextAt(57, 7, "Active Power: Radiation");
    drawTextAt(57, 8, "Level: 3");
    drawTextAt(57, 9, "XP Progress:");
    drawProgressBar(57, 10, 30, 0.45);
    drawTextAt(89, 10, "45%");
    drawTextAt(57, 12, "Damage Dealt: 234");
    drawTextAt(57, 13, "Damage Taken: 89");

    // Round info panel
    drawBox(108, 3, 50, 15, "|");
    drawTextAt(110, 4, "ROUND INFO");
    drawHorizontalLine(109, 5, 48, "-");
    drawTextAt(110, 7, "Round: 12/30");
    drawTextAt(110, 8, "Score: CT 8 - T 4");
    drawTextAt(110, 9, "Time Left: 1:23");
    drawTextAt(110, 11, "Bomb: Not planted");
    drawTextAt(110, 12, "Players: 10/10");

    // Bottom bar
    drawBox(2, 20, 156, 4, "#");
    drawCenteredText(21, "SUPER POWERS v0.5.0 | Server: My Server | Map: de_dust2");
    drawCenteredText(22, "Type !help for commands | Type !powers for your powers");
}

function test10_TableFormatting() {
    setLine(0, "TEST 10: TABLE FORMATTING".padEnd(GRID_W, " "));
    drawHorizontalLine(0, 1, 160, "-");

    drawTextAt(2, 3, "+----+------------------+--------+--------+--------+");
    drawTextAt(2, 4, "| #  | Power            | Price  | Rarity | Status |");
    drawTextAt(2, 5, "+----+------------------+--------+--------+--------+");

    const rows = [
        ["1", "Blood Fury", "$7000", "Rare", "ACTIVE"],
        ["2", "Radiation", "$7000", "Rare", "ACTIVE"],
        ["3", "Invisibility", "$8000", "Legend", "OFF"],
        ["4", "Regeneration", "$5000", "Uncom", "ACTIVE"],
        ["5", "Bitcoin Miner", "$5000", "Uncom", "OFF"],
        ["6", "Speedy Fella", "$3000", "Common", "ACTIVE"],
        ["7", "Golden Bullet", "$4000", "Uncom", "OFF"],
        ["8", "Evil Aura", "$9500", "Rare", "OFF"],
    ];

    for (let i = 0; i < rows.length; i++) {
        const r = rows[i];
        const row = `| ${r[0].padStart(2)} | ${r[1].padEnd(16)} | ${r[2].padEnd(6)} | ${r[3].padEnd(6)} | ${r[4].padEnd(6)} |`;
        drawTextAt(2, 6 + i, row);
    }

    drawTextAt(2, 14, "+----+------------------+--------+--------+--------+");
    setLine(16, "Total powers: 8 | Active: 3 | Total cost: $45,000".padEnd(GRID_W, " "));
}

function test11_AnimationPattern() {
    setLine(0, "TEST 11: ANIMATION PATTERN".padEnd(GRID_W, " "));
    drawHorizontalLine(0, 1, 160, "-");

    // Wave pattern
    for (let row = 3; row < 20; row++) {
        let line = "";
        for (let col = 0; col < GRID_W; col++) {
            const wave = Math.sin((col + row * 5) * 0.1);
            if (wave > 0.5) line += "\u2588";
            else if (wave > 0) line += "\u2592";
            else if (wave > -0.5) line += "\u2591";
            else line += " ";
        }
        setLine(row, line);
    }

    // Diagonal pattern
    for (let row = 22; row < 35; row++) {
        let line = "";
        for (let col = 0; col < GRID_W; col++) {
            const diag = (col + row) % 16;
            if (diag === 0) line += "#";
            else if (diag < 4) line += "=";
            else if (diag < 8) line += "-";
            else if (diag < 12) line += ".";
            else line += " ";
        }
        setLine(row, line);
    }

    setLine(37, "Wave pattern (top) and diagonal gradient (bottom)".padEnd(GRID_W, " "));
    setLine(38, "This demonstrates unicode block character density variations".padEnd(GRID_W, " "));
}

function test12_MegaDemo() {
    clearScreen();

    // Title bar
    drawBox(0, 0, 160, 3, "=");
    setLine(1, " ASCII OVERLAY MEGA DEMO - 160x40 GRID - ALL FEATURES COMBINED".padEnd(GRID_W, " "));

    // Left panel - ASCII Art
    drawBox(1, 4, 40, 20, "+");
    setLine(5, " ASCII ART GALLERY".padEnd(GRID_W, " "));
    drawHorizontalLine(2, 6, 38, "-");

    const cat = [
        "    /\\_/\\  ",
        "   ( o.o ) ",
        "    > ^ <  ",
        "   /|   |\\",
        "  (_|   |_)",
    ];
    for (let i = 0; i < cat.length; i++) setLine(7 + i, cat[i]);

    const heart = [
        "  ****  ****  ",
        "  **********  ",
        "   ********   ",
        "    ******    ",
        "     ****     ",
        "      **      ",
    ];
    for (let i = 0; i < heart.length; i++) setLine(13 + i, heart[i]);

    // Center panel - Status
    drawBox(42, 4, 76, 20, "|");
    setLine(5, " SYSTEM STATUS".padEnd(GRID_W, " "));
    drawHorizontalLine(43, 6, 74, "-");

    drawTextAt(44, 8, "CPU Usage:");
    drawProgressBar(56, 8, 40, 0.65);
    drawTextAt(98, 8, "65%");

    drawTextAt(44, 9, "Memory:");
    drawProgressBar(56, 9, 40, 0.42);
    drawTextAt(98, 9, "42%");

    drawTextAt(44, 10, "Network:");
    drawProgressBar(56, 10, 40, 0.78);
    drawTextAt(98, 10, "78%");

    drawTextAt(44, 12, "Players Online: 24/32");
    drawTextAt(44, 13, "Uptime: 3d 14h 22m");
    drawTextAt(44, 14, "Map: de_dust2");
    drawTextAt(44, 15, "Mode: Super Powers (Random)");

    drawTextAt(44, 17, "Round Progress:");
    drawProgressBar(44, 18, 60, 0.75);
    drawTextAt(106, 18, "75%");

    drawTextAt(44, 20, "Bomb Timer:");
    drawProgressBar(44, 21, 60, 0.33, "!", " ");
    drawTextAt(106, 21, "33%");

    // Right panel - Menu
    drawBox(119, 4, 40, 20, "#");
    setLine(5, " QUICK MENU".padEnd(GRID_W, " "));
    drawHorizontalLine(120, 6, 38, "-");

    drawTextAt(121, 8, "[1] Buy Equipment");
    drawTextAt(121, 9, "[2] Team Chat");
    drawTextAt(121, 10, "[3] Scoreboard");
    drawTextAt(121, 11, "[4] Settings");
    setLine(13, ">".padEnd(GRID_W, " "));
    drawTextAt(122, 13, "Select option:");

    // Bottom status bar
    drawBox(0, 25, 160, 14, "*");
    setLine(26, " BOTTOM STATUS BAR".padEnd(GRID_W, " "));
    drawHorizontalLine(1, 27, 158, "-");

    drawTextAt(2, 29, "Health:");
    drawProgressBar(10, 29, 25, 0.85);
    drawTextAt(37, 29, "85%");

    drawTextAt(2, 30, "Armor:");
    drawProgressBar(10, 30, 25, 0.60);
    drawTextAt(37, 30, "60%");

    drawTextAt(50, 29, "Ammo: 30/90");
    drawTextAt(50, 30, "Money: $8,500");

    drawTextAt(80, 29, "Kills: 12");
    drawTextAt(80, 30, "Deaths: 5");

    drawTextAt(100, 29, "Colors:");
    const colors = ["green", "red", "blue", "gold", "purple"];
    for (let i = 0; i < colors.length; i++) {
        drawCharAt(108 + (i * 4), 29, "\u2588");
    }

    drawCenteredText(32, "SUPER POWERS PLUGIN v0.5.0 | ASCII OVERLAY VIA point_script");
    drawCenteredText(33, "All rendering driven by cs_script - no C# plugin needed");

    drawHorizontalLine(0, 35, 160, "=");
    setLine(36, "This demonstrates the full capabilities of the 160x40 ASCII overlay system.".padEnd(GRID_W, " "));
    setLine(37, "Features: Text, Boxes, Progress Bars, Colors, Unicode, ASCII Art, Tables".padEnd(GRID_W, " "));
    setLine(38, "All rendering is driven by cs_script via custom_hud_layout entity.".padEnd(GRID_W, " "));
    setLine(39, "Semi-transparent background allows game scene to be visible behind the overlay.".padEnd(GRID_W, " "));
}

// ─── Test Runner ─────────────────────────────────────────────────────────────

const tests = [
    test0_BasicText,
    test1_DrawingPrimitives,
    test2_ProgressBars,
    test3_Colors,
    test4_BoxWithText,
    test5_FullScreenFill,
    test6_EdgeCases,
    test7_AsciiArt,
    test8_MenuSimulation,
    test9_StatusDisplay,
    test10_TableFormatting,
    test11_AnimationPattern,
    test12_MegaDemo,
];

function runTest(index) {
    if (index < 0 || index >= TEST_COUNT) return;
    currentTest = index;
    clearScreen();
    tests[index]();
    Instance.Msg(`[HUD-TEST] Running test ${index}/${TEST_COUNT - 1}`);
}

function nextTest() {
    runTest((currentTest + 1) % TEST_COUNT);
}

// ─── Entry Points ────────────────────────────────────────────────────────────

Instance.OnActivate(() => {
    hud = Instance.FindEntityByName("hud");
    if (hud) {
        Instance.Msg("[HUD-TEST] Found custom_hud_layout entity 'hud'");
    } else {
        Instance.Msg("[HUD-TEST] WARNING: custom_hud_layout entity 'hud' not found! "
            + "Add a custom_hud_layout with targetname 'hud' in Hammer.");
    }
});

Instance.OnRoundStart(() => {
    if (!hud) {
        hud = Instance.FindEntityByName("hud");
    }
    // Run first test on round start
    runTest(0);
});

// Allow manual test selection via RunScriptInput on the point_script entity.
// In Hammer, add an input: RunScriptInput → test_N (where N is 0-12)
for (let i = 0; i < TEST_COUNT; i++) {
    Instance.OnScriptInput(`test_${i}`, () => runTest(i));
}

// Cycle to next test
Instance.OnScriptInput("next", () => nextTest());

// Run specific test by number via console: ent_fire point_script RunScriptInput test_0
