using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace MicroCar.UI
{
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
