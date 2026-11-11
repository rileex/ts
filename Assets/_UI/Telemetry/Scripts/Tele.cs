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

    /// <summary>
    /// Per-layout placement and visibility of one element, and optionally its text size. A preset
    /// left unset falls back to <see cref="pc"/> (and <see cref="land"/> to <see cref="pc"/>).
    /// Applied by the <see cref="TeleCanvas"/> above it whenever the screen changes shape.
    /// </summary>
    public sealed class TeleLayout : MonoBehaviour
    {
        public TeleSpot pc, land, port;
        [Tooltip("Text size per preset (pc, land, port); 0 leaves it.")]
        public Vector3 fontSize;
        [Tooltip("Uniform scale per preset (pc, land, port); 0 leaves it.")]
        public Vector3 scale;
        [Tooltip("Only with touch controls (phones, tablets).")]
        public bool touchOnly;
        [Tooltip("Only with a keyboard (desktop).")]
        public bool keysOnly;

        [System.NonSerialized] public bool hiddenByLayout;

        public void Apply(TelePreset preset)
        {
            var spot = preset == TelePreset.Port && port.set ? port
                : preset == TelePreset.Land && land.set ? land
                : pc;

            bool touch = TeleScreen.Touch;
            if ((spot.set && spot.hide) || (touchOnly && !touch) || (keysOnly && touch))
            {
                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                    hiddenByLayout = true;
                }
                return;
            }

            if (hiddenByLayout)
            {
                hiddenByLayout = false;
                gameObject.SetActive(true);
            }

            if (spot.set && transform is RectTransform rt)
                spot.ApplyTo(rt);

            float size = preset == TelePreset.Port ? fontSize.z : preset == TelePreset.Land ? fontSize.y : fontSize.x;
            if (size > 0f && TryGetComponent<Text>(out var text))
                text.fontSize = Mathf.RoundToInt(size);

            float s = preset == TelePreset.Port ? scale.z : preset == TelePreset.Land ? scale.y : scale.x;
            if (s > 0f)
                transform.localScale = new Vector3(s, s, 1f);
        }
    }

    /// <summary>
    /// The root of a Telemetry canvas. Sizes the canvas so its short side is always the same number
    /// of units (720 with a keyboard, 534 on touch, where everything must be bigger under a finger),
    /// keeps the "Safe" rect inside the device's safe area, and re-applies every <see cref="TeleLayout"/>
    /// below it when the screen changes shape or between touch and keyboard.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class TeleCanvas : MonoBehaviour
    {
        public const float PcShortSide = 720f;
        public const float TouchShortSide = 534f;

        [Tooltip("Rects kept inside the safe area plus the margin of each preset.")]
        public RectTransform[] safeRects;

        public static readonly Vector4[] Margins =
        {
            new Vector4(32f, 28f, 32f, 28f),   // pc: left, bottom, right, top
            new Vector4(44f, 18f, 44f, 18f),   // land
            new Vector4(20f, 30f, 20f, 44f),   // port
        };

        CanvasScaler scaler;
        int width, height;
        bool touch;
        Rect safe;
        readonly List<TeleLayout> layouts = new List<TeleLayout>();

        public static TelePreset Current { get; private set; } = TelePreset.Pc;

        void Awake() => scaler = GetComponent<CanvasScaler>();

        void OnEnable() => Refresh(true);

        void Update() => Refresh(false);

        /// <summary>Call after adding elements at run time.</summary>
        public void Relayout() => Refresh(true);

        void Refresh(bool force)
        {
            bool t = TeleScreen.Touch;
            if (!force && Screen.width == width && Screen.height == height && t == touch && Screen.safeArea == safe)
                return;
            width = Screen.width;
            height = Screen.height;
            touch = t;
            safe = Screen.safeArea;

            bool portrait = height > width;
            float shortSide = t ? TouchShortSide : PcShortSide;
            if (scaler == null) scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.referenceResolution = portrait ? new Vector2(shortSide, shortSide * 16f / 9f) : new Vector2(shortSide * 16f / 9f, shortSide);
            scaler.matchWidthOrHeight = portrait ? 0f : 1f;

            var preset = TeleScreen.Preset;
            Current = preset;

            // Safe rects: the preset's margin, or the device's safe-area inset plus a little when
            // that is bigger (a notch, a home indicator).
            float unitsPerPixel = shortSide / Mathf.Max(1, Mathf.Min(width, height));
            var m = Margins[(int)preset];
            const float pad = 10f;
            float left = Mathf.Max(m.x, safe.xMin * unitsPerPixel + pad);
            float bottom = Mathf.Max(m.y, safe.yMin * unitsPerPixel + pad);
            float right = Mathf.Max(m.z, (width - safe.xMax) * unitsPerPixel + pad);
            float top = Mathf.Max(m.w, (height - safe.yMax) * unitsPerPixel + pad);
            if (safeRects != null)
            {
                foreach (var rt in safeRects)
                {
                    if (rt == null) continue;
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = new Vector2(left, bottom);
                    rt.offsetMax = new Vector2(-right, -top);
                }
            }

            layouts.Clear();
            GetComponentsInChildren(true, layouts);
            foreach (var l in layouts)
                l.Apply(preset);
        }
    }
}
