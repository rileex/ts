using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace MicroCar.UI
{
    /// <summary>
    /// The Telemetry style: palette, number formats and the screen mode every Telemetry canvas lays
    /// itself out for. The UI is built by the editor tool TeleBuilder (menu MicroCar / UI); these are
    /// the pieces that run in the game.
    /// </summary>
    public static class Tele
    {
        public static readonly Color Bg = Hex(0x090D12);
        public static readonly Color Ink = Hex(0xEAF0F6);
        public static readonly Color Muted = Hex(0x98A4B5);
        public static readonly Color Lime = Hex(0xC6F432);
        public static readonly Color Amber = Hex(0xFFB547);
        public static readonly Color OnLime = Hex(0x0B1004);
        public static readonly Color Line = new Color32(234, 240, 246, 41);
        public static readonly Color Dim = new Color32(234, 240, 246, 46);
        public static readonly Color Gold = Hex(0xE8C24A);
        public static readonly Color Silver = Hex(0xC9D1DC);
        public static readonly Color Bronze = Hex(0xD08A4E);

        public static Color Hex(int rgb) =>
            new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

        static readonly NumberFormatInfo Grouping = new NumberFormatInfo { NumberGroupSeparator = " ", NumberDecimalDigits = 0 };

        /// <summary>1400 → "1 400".</summary>
        public static string Credits(int value) => value.ToString("N0", Grouping);

        /// <summary>Seconds → "1:42.37".</summary>
        public static string Clock(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int minutes = (int)(seconds / 60f);
            return minutes + ":" + (seconds - minutes * 60f).ToString("00.00", CultureInfo.InvariantCulture);
        }

        /// <summary>A two-digit index: 1 → "01".</summary>
        public static string Two(int value) => value.ToString("00", CultureInfo.InvariantCulture);
    }

    /// <summary>Which of the three layouts is showing.</summary>
    public enum TelePreset { Pc = 0, Land = 1, Port = 2 }

    /// <summary>
    /// Touch or keyboard, landscape or portrait. Touch follows the same rule as the race's
    /// InputSystem (a phone or tablet browser on WebGL, always on Android/iOS, never on desktop), so
    /// the menus and the race agree. In the editor the menu item MicroCar/UI/Simulate Touch forces it.
    /// </summary>
    public static class TeleScreen
    {
        public const string ForceTouchKey = "MicroCar.Tele.ForceTouch";

        public static bool Touch
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool(ForceTouchKey, false);
#elif UNITY_WEBGL
                return Application.isMobilePlatform;
#elif UNITY_ANDROID || UNITY_IOS
                return true;
#else
                return false;
#endif
            }
        }

        public static bool Portrait => Screen.height > Screen.width;

        public static TelePreset Preset => Portrait ? TelePreset.Port : Touch ? TelePreset.Land : TelePreset.Pc;

        /// <summary>Keyboard hints make sense only with a keyboard and the room of a landscape screen.</summary>
        public static bool ShowKeys => !Touch && !Portrait;
    }

    /// <summary>
    /// One placement of a RectTransform: anchors, pivot and offsets, or hidden.
    /// </summary>
    [System.Serializable]
    public struct TeleSpot
    {
        public bool set;
        public bool hide;
        public Vector2 anchorMin, anchorMax, pivot, offsetMin, offsetMax;

        public static TeleSpot Hidden => new TeleSpot { set = true, hide = true };

        /// <summary>A fixed-size box pinned to an anchor point; <paramref name="pos"/> is from the anchor to the pivot.</summary>
        public static TeleSpot Box(Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size) => new TeleSpot
        {
            set = true,
            anchorMin = anchor,
            anchorMax = anchor,
            pivot = pivot,
            offsetMin = pos - Vector2.Scale(pivot, size),
            offsetMax = pos + Vector2.Scale(Vector2.one - pivot, size),
        };

        /// <summary>Stretched between two anchors, inset by the offsets (left/bottom and right/top, positive inwards).</summary>
        public static TeleSpot Fill(Vector2 anchorMin, Vector2 anchorMax, Vector2 insetMin, Vector2 insetMax, Vector2? pivot = null) => new TeleSpot
        {
            set = true,
            anchorMin = anchorMin,
            anchorMax = anchorMax,
            pivot = pivot ?? new Vector2(0.5f, 0.5f),
            offsetMin = insetMin,
            offsetMax = -insetMax,
        };

        public void ApplyTo(RectTransform rt)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }

}
