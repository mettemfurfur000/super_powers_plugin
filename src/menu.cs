
// using System.Drawing;
// using CounterStrikeSharp.API;
// using CounterStrikeSharp.API.Core;
// using CounterStrikeSharp.API.Modules.Utils;
// namespace super_powers_plugin.src;

// public class ChoiceMenu(CCSPlayerController trg)
// {
//     public string name = "emptyname";
//     public List<Tuple<string, Action>> choices = [];
//     public CCSPlayerController target = trg;

//     public int selected_option = 0;
//     public bool is_inactive = false;

//     public bool ignore_left = false;
//     public bool ignore_right = false;

//     public void Update()
//     {
//         if (is_inactive)
//             return;

//         PlayerButtons buttons = target.Buttons;

//         if ((buttons & PlayerButtons.Back) != 0)
//         {
//             if (!ignore_left)
//                 selected_option = Math.Max(0, selected_option - 1);
//             ignore_left = true;
//         }
//         else
//             ignore_left = false;

//         if ((buttons & PlayerButtons.Forward) != 0)
//         {
//             if (!ignore_right)
//                 selected_option = Math.Max(0, selected_option + 1);
//             ignore_right = true;
//         }
//         else
//             ignore_right = false;

//         selected_option = Math.Min(selected_option, choices.Count - 1);

//         if ((buttons & PlayerButtons.Use) != 0) // Selected
//         {
//             choices[selected_option].Item2.Invoke();
//             is_inactive = true;
//         }
//     }
// }

// public static class MenuManager
// {
//     public static IGameHUDAPI? HudApi = null;
//     public static SuperPowerConfig? Config = null;

//     private const int spacing_y = 10;
//     private const int offset_x = -75;
//     private const int arrow_distance = 10;
//     private const int distance_z = 70;
//     private const int font_size = 36;
//     private const string font = "Arial Bold";

//     public const byte menuOffsetChannel = 8;

//     public static void MenuClose(ChoiceMenu m)
//     {
//         // int total = m.choices.Count;

//         // for (int i = 0; i < total + 2; i++)
//         // {
//         //     HudApi!.Native_GameHUD_Remove(m.target, (byte)(i + menuOffsetChannel));
//         // }
//     }

//     public static void ChoiceMenuDisplay(ChoiceMenu m, float duration)
//     {
//         int total = m.choices.Count;
//         float start_x = -(total - 1) * spacing_y / 2;

//         //

//         float desc_offset = start_x + -1 * spacing_y; // options offsets x

//         HudApi!.Native_GameHUD_SetParams(m.target, (byte)(menuOffsetChannel + 1), new Vector(offset_x, desc_offset, distance_z), Color.White, font_size - 10, font, 0.1f,
//             PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT, PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_BOTTOM, PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE, 0.6f, 1.2f);
//         HudApi!.Native_GameHUD_Show(m.target, (byte)(menuOffsetChannel + 1), "W-S - select, E - confirm", duration);

//         ChoiceMenuDisplayArrow(m, 0.5f);

//         byte cur_offset = menuOffsetChannel + 2;

//         for (int i = 0; i < total; i++)
//         {
//             float offset = start_x + i * spacing_y; // options offsets x
//             HudApi!.Native_GameHUD_SetParams(m.target, (byte)(i + cur_offset), new Vector(offset_x, offset, distance_z), Color.White, font_size, font, 0.1f,
//             PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT, PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_BOTTOM, PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE, 0.6f, 1.2f);
//             HudApi!.Native_GameHUD_Show(m.target, (byte)(i + cur_offset), m.choices[i].Item1, duration);
//         }
//     }

//     public static void ChoiceMenuDisplayArrow(ChoiceMenu m, float duration)
//     {
//         int total = m.choices.Count;
//         float start_x = -(total - 1) * spacing_y / 2;
//         float offset = start_x + m.selected_option * spacing_y;

//         HudApi!.Native_GameHUD_SetParams(m.target, (byte)(menuOffsetChannel), new Vector(offset_x - arrow_distance, offset, distance_z), Color.Green, font_size + 40, font, 0.1f,
//         PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT, PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_BOTTOM, PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE, 0.6f, 1.2f);
//         HudApi!.Native_GameHUD_Show(m.target, (byte)(menuOffsetChannel), "↦", duration);
//     }
// }