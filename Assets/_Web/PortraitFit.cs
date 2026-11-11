using System.Collections.Generic;
using ALIyerEdon;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MicroCar.Web
{
    /// <summary>
    /// Keeps the game playable on screens narrower than 16:9, all the way down to a 9:16 phone held
    /// upright, which is how YouTube Playables are often shown. Ported in spirit from Car Simulator's
    /// PortraitFit; the numbers are this game's own.
    ///
    /// <para>
    /// <b>The UI.</b> Every canvas is authored for 1280x720 with CanvasScaler match 0.5. That match
    /// only fits the 1280-unit-wide layout at exactly 16:9: on anything narrower the sides are cut
    /// (on 4:3 already; in portrait "SELECT LEVEL" loses its first letters). Matching the width
    /// instead fits the whole layout on every narrower screen, and at 16:9 both give the same scale,
    /// so switching there is seamless. Wider than 16:9 the authored match is left alone.
    /// </para>
    ///
    /// <para>
    /// <b>The cameras.</b> Unity's field of view is vertical; the horizontal view is what the aspect
    /// leaves. The race camera's 55 degrees are 85 across at 16:9 but 33 at 9:16 — a keyhole. So in
    /// portrait the vertical FOV is opened (up to <see cref="MaxVerticalFov"/>, past which it looks
    /// like a fisheye) and the chase camera stands further back and higher. The race camera's FOV is
    /// driven every frame by EasyCarController (default / nitro), so its two targets are what get
    /// changed, never the camera itself. Other cameras (garage, alternative race cameras) get their
    /// FOV opened directly.
    /// </para>
    ///
    /// <para>
    /// <b>Race HUD and touch controls.</b> See <see cref="FitHud"/>: moved to the top and bottom of a
    /// tall screen and scaled back up (fitting the width makes the UI ~56% of its landscape size).
    /// </para>
    ///
    /// Everything blends continuously with the aspect and is written back to the authored values at
    /// 16:9 and wider. Runs on every platform (a 4:3 tablet benefits too).
    /// </summary>
    public sealed class PortraitFit : MonoBehaviour
    {
        const float LandscapeAspect = 16f / 9f;
        const float PortraitAspect = 9f / 16f;

        /// <summary>Vertical FOV ceiling: wider than this stops looking like a camera.</summary>
        const float MaxVerticalFov = 78f;

        /// <summary>Nitro widens the view on top of the default; it may go a little further.</summary>
        const float MaxNitroFov = 86f;

        /// <summary>Chase camera at 9:16: this much further back (x distance) and higher (+metres).</summary>
        const float PortraitDistanceGain = 0.35f;
        const float PortraitHeightGain = 0.45f;


        /// <summary>Clear space kept between the left-hand and right-hand touch controls (canvas units).</summary>
        const float ControlsGap = 24f;


        /// <summary>0 at 16:9 and wider, 1 at 9:16 and narrower.</summary>
        public static float Portraitness { get; private set; }

        readonly Dictionary<CanvasScaler, float> authoredMatch = new Dictionary<CanvasScaler, float>();
        readonly Dictionary<Camera, float> authoredFov = new Dictionary<Camera, float>();
        readonly Dictionary<EasyCarController, Vector2> authoredCarFov = new Dictionary<EasyCarController, Vector2>();
        readonly Dictionary<SmoothFollow2, Vector2> authoredFollow = new Dictionary<SmoothFollow2, Vector2>();

        int lastWidth, lastHeight;
        float nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var go = new GameObject("PortraitFit");
            DontDestroyOnLoad(go);
            go.AddComponent<PortraitFit>();
        }

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Everything from the previous scene is gone; forget it and look again right away.
            authoredMatch.Clear();
            authoredFov.Clear();
            authoredCarFov.Clear();
            authoredFollow.Clear();
            authoredPlacement.Clear();
            authoredSize.Clear();
            nextScan = 0f;
        }

        // Late, so the player's car (spawned in Race_Manager's Start coroutine) and its controller
        // exist, and after EasyCarController has written its FOV for the frame.
        void LateUpdate()
        {
            bool resized = Screen.width != lastWidth || Screen.height != lastHeight;
            if (!resized && Time.unscaledTime < nextScan)
                return;

            lastWidth = Screen.width;
            lastHeight = Screen.height;
            // Rescan now and then: cars, cameras and canvases appear after the scene has loaded.
            nextScan = Time.unscaledTime + 0.5f;
            Apply();
        }

        void Apply()
        {
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : LandscapeAspect;
            float t = Mathf.InverseLerp(LandscapeAspect, PortraitAspect, aspect);
            Portraitness = t;

            FitCanvases(aspect);
            FitRaceCamera(aspect, t);
            FitOtherCameras(aspect);
            FitHud(t);
        }

        void FitCanvases(float aspect)
        {
            foreach (var scaler in FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                    continue;
                // The Telemetry UI sizes and lays itself out (MicroCar.UI.TeleCanvas).
                if (scaler.GetComponent<MicroCar.UI.TeleCanvas>() != null)
                    continue;

                if (scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
                {
                    if (!authoredMatch.TryGetValue(scaler, out float authored))
                        authoredMatch[scaler] = authored = scaler.matchWidthOrHeight;

                    // Narrower than the reference shape: fit the width. Otherwise what was authored.
                    var r = scaler.referenceResolution;
                    float referenceAspect = r.y > 0 ? r.x / r.y : LandscapeAspect;
                    float match = aspect < referenceAspect ? 0f : authored;
                    if (!Mathf.Approximately(scaler.matchWidthOrHeight, match))
                        scaler.matchWidthOrHeight = match;
                }
                // Expand (the splash) already fits the whole reference on any shape.
            }
        }

        /// <summary>The vertical FOV that keeps a landscape-authored view's width, within limits.</summary>
        static float Opened(float authoredVertical, float aspect, float ceiling)
        {
            if (aspect >= LandscapeAspect)
                return authoredVertical;

            // Horizontal FOV the authored value had at 16:9 ...
            float halfH = Mathf.Atan(Mathf.Tan(authoredVertical * 0.5f * Mathf.Deg2Rad) * LandscapeAspect);
            // ... and the vertical one that would keep it at this aspect.
            float needed = 2f * Mathf.Atan(Mathf.Tan(halfH) / aspect) * Mathf.Rad2Deg;
            return Mathf.Clamp(needed, authoredVertical, Mathf.Max(authoredVertical, ceiling));
        }

        void FitRaceCamera(float aspect, float t)
        {
            foreach (var car in FindObjectsByType<EasyCarController>(FindObjectsSortMode.None))
            {
                if (!car.isPlayer)
                    continue;

                if (!authoredCarFov.TryGetValue(car, out var fov))
                    authoredCarFov[car] = fov = new Vector2(car.defaultFOV, car.nitroFOV);

                float opened = Opened(fov.x, aspect, MaxVerticalFov);
                car.defaultFOV = opened;
                car.nitroFOV = Mathf.Min(Mathf.Max(fov.y, MaxNitroFov), fov.y + (opened - fov.x));
            }

            foreach (var follow in FindObjectsByType<SmoothFollow2>(FindObjectsSortMode.None))
            {
                if (!authoredFollow.TryGetValue(follow, out var dh))
                    authoredFollow[follow] = dh = new Vector2(follow.distance, follow.height);

                follow.distance = dh.x * (1f + PortraitDistanceGain * t);
                follow.height = dh.y + PortraitHeightGain * t;
            }
        }

        void FitOtherCameras(float aspect)
        {
            bool raceCameraDriven = false;
            foreach (var car in FindObjectsByType<EasyCarController>(FindObjectsSortMode.None))
                raceCameraDriven |= car.isPlayer;

            foreach (var cam in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (cam.orthographic || cam.targetTexture != null)
                    continue;

                // The race camera belongs to EasyCarController (see FitRaceCamera).
                if (raceCameraDriven && cam.CompareTag("MainCamera"))
                    continue;

                if (!authoredFov.TryGetValue(cam, out float authored))
                    authoredFov[cam] = authored = cam.fieldOfView;

                float fov = Opened(authored, aspect, MaxVerticalFov);
                if (!Mathf.Approximately(cam.fieldOfView, fov))
                    cam.fieldOfView = fov;
            }
        }

        enum Spread { Split, InPlace, Whole }

        /// <summary>
        /// The menu and HUD holders laid out as offsets from the canvas centre (all direct children of
        /// a root canvas). Split: what is above the centre goes to the top, below to the bottom, the
        /// middle band stays. InPlace: only scaled, about the vertical middle (the garage's main menu,
        /// whose buttons straddle the centre and must stay together). Whole: a centred panel scaled as
        /// one piece (the pre-race / finish panel, the pause menu).
        /// </summary>
        static readonly (string name, float scale, Spread spread, bool handsApart)[] Holders =
        {
            ("Mobile_Controls", 1.6f, Spread.Split, true),
            ("Race_UI", 1.3f, Spread.Split, false),
            ("Select_Level", 1.25f, Spread.Split, false),
            ("Car_Select", 1.3f, Spread.Split, false),
            ("Main_Menu", 1.4f, Spread.InPlace, false),
            ("Start_UI", 1.35f, Spread.Whole, false),
            ("Pause_Menu", 1.5f, Spread.Whole, false),
        };

        /// <summary>
        /// Extra moves at full portrait, by element name. The racer list (Race_UI/Position_Info) shares
        /// the top-right corner with the touch Pause and Camera buttons; in landscape they overlap a
        /// little, in portrait (different scales) a lot, and there is height to spare below them.
        /// </summary>
        static readonly Dictionary<string, Vector2> PortraitNudge = new Dictionary<string, Vector2>
        {
            { "Position_Info", new Vector2(0f, -230f) },
        };

        /// <summary>
        /// Full-screen dimmers authored as fixed-size centred images; stretched to cover a tall canvas.
        /// Levels_List is the level screen's dimmer (and the row holding the level cards, which its
        /// HorizontalLayoutGroup keeps centred).
        /// </summary>
        static readonly string[] Covers = { "Background", "Loading", "Levels_List" };

        /// <summary>Elements this close to the centre line (canvas units) count as the middle band.</summary>
        const float MiddleBandY = 100f;
        const float MiddleBandX = 120f;

        /// <summary>
        /// Every element in the template's menus and HUD sits at a fixed offset from the canvas centre
        /// (inside 100x100 holders), so on a tall canvas all of it bunches into a 720-high band across
        /// the middle of the screen, over the car. In portrait:
        /// <list type="number">
        /// <item>elements above the middle band move up and those below move down by half of the extra
        /// height: the landscape layout's top half lands at the top, its bottom half at the bottom;</item>
        /// <item>each is scaled about its own screen corner (or the middle line / centre for middle-band
        /// elements), since fitting the width leaves the UI at ~56% of its landscape size; the touch
        /// controls only as far as the left and right hands stay apart.</item>
        /// </list>
        /// Works on localPosition, which for children of a centred holder is the offset from the canvas
        /// centre. At 16:9 the extra height and the scale are both nothing.
        /// </summary>
        void FitHud(float t)
        {
            foreach (var rt in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var canvas = rt.GetComponentInParent<Canvas>(true);
                if (canvas == null || rt.parent != canvas.rootCanvas.transform)
                    continue;
                if (canvas.rootCanvas.GetComponent<MicroCar.UI.TeleCanvas>() != null)
                    continue;

                var root = (RectTransform)canvas.rootCanvas.transform;
                var size = root.rect.size;

                if (System.Array.IndexOf(Covers, rt.name) >= 0)
                {
                    Cover(rt, size);
                    continue;
                }

                int holder = System.Array.FindIndex(Holders, h => h.name == rt.name);
                if (holder < 0)
                    continue;

                var entry = Holders[holder];
                if (entry.spread == Spread.Whole)
                {
                    // One centred panel: scale it about its own centre.
                    if (rt.anchorMin != new Vector2(0.5f, 0.5f) || rt.anchorMax != new Vector2(0.5f, 0.5f))
                        continue;
                    var whole = Authored(rt);
                    var wholeScale = whole.scale * Mathf.Lerp(1f, entry.scale, t);
                    if (rt.localScale != wholeScale) rt.localScale = wholeScale;
                    continue;
                }

                if (!IsCentredContainer(rt))
                    continue;
                var scaler = canvas.rootCanvas.GetComponent<CanvasScaler>();
                float referenceHeight = scaler != null ? scaler.referenceResolution.y : 720f;
                float lift = entry.spread == Spread.Split ? Mathf.Max(0f, (size.y - referenceHeight) * 0.5f) : 0f;

                var items = new List<RectTransform>();
                Collect(rt, items);

                float scale = Mathf.Lerp(1f, entry.scale, t);
                if (entry.handsApart)
                {
                    // Widest reach of the bottom-left and bottom-right groups at scale 1.
                    float left = 0f, right = 0f;
                    foreach (var c in items)
                    {
                        var a = Authored(c);
                        if (a.position.y > -MiddleBandY) continue; // only the bottom row competes for width
                        float halfW = c.rect.width * Mathf.Abs(a.scale.x) * 0.5f;
                        if (a.position.x < 0f) left = Mathf.Max(left, a.position.x + halfW + size.x * 0.5f);
                        else right = Mathf.Max(right, size.x * 0.5f - (a.position.x - halfW));
                    }
                    if (left + right > 0f)
                        scale = Mathf.Max(1f, Mathf.Min(scale, (size.x - ControlsGap) / (left + right)));
                }

                foreach (var c in items)
                {
                    var a = Authored(c);
                    bool middleX = Mathf.Abs(a.position.x) < MiddleBandX;
                    bool middleY = entry.spread == Spread.InPlace || Mathf.Abs(a.position.y) < MiddleBandY;
                    float sx = Mathf.Sign(a.position.x == 0f ? 1f : a.position.x);
                    float sy = Mathf.Sign(a.position.y == 0f ? 1f : a.position.y);

                    var corner = new Vector2(middleX ? 0f : sx * size.x * 0.5f, middleY ? 0f : sy * size.y * 0.5f);
                    var moved = new Vector2(a.position.x, a.position.y + (middleY ? 0f : sy * lift));
                    var placed = corner + (moved - corner) * scale;
                    if (PortraitNudge.TryGetValue(c.name, out var nudge))
                        placed += nudge * t;
                    var target = new Vector3(placed.x, placed.y, a.position.z);
                    if (c.localPosition != target) c.localPosition = target;
                    var targetScale = a.scale * scale;
                    if (c.localScale != targetScale) c.localScale = targetScale;

                    if (System.Array.IndexOf(Covers, c.name) >= 0)
                        Cover(c, size);
                }
            }
        }

        /// <summary>Grows a centred full-screen image until it covers the whole (tall) canvas; never shrinks it.</summary>
        void Cover(RectTransform rt, Vector2 canvasSize)
        {
            if (!authoredSize.TryGetValue(rt, out var authored))
                authoredSize[rt] = authored = rt.sizeDelta;

            float s = Mathf.Abs(rt.localScale.y) > 0.0001f ? Mathf.Abs(rt.localScale.y) : 1f;
            float neededHeight = canvasSize.y / s + 40f;
            var target = new Vector2(authored.x, Mathf.Max(authored.y, neededHeight));
            if (rt.sizeDelta != target) rt.sizeDelta = target;
        }

        readonly Dictionary<RectTransform, Vector2> authoredSize = new Dictionary<RectTransform, Vector2>();

        struct Placement { public Vector3 position, scale; }
        readonly Dictionary<RectTransform, Placement> authoredPlacement = new Dictionary<RectTransform, Placement>();

        Placement Authored(RectTransform c)
        {
            if (!authoredPlacement.TryGetValue(c, out var a))
                authoredPlacement[c] = a = new Placement { position = c.localPosition, scale = c.localScale };
            return a;
        }

        /// <summary>A 100x100-style holder sitting at the canvas centre whose children are the real elements.</summary>
        static bool IsCentredContainer(RectTransform rt) =>
            rt.anchorMin == new Vector2(0.5f, 0.5f) && rt.anchorMax == new Vector2(0.5f, 0.5f)
            && rt.pivot == new Vector2(0.5f, 0.5f) && rt.localScale == Vector3.one
            && Mathf.Abs(rt.anchoredPosition.x) < 20f && Mathf.Abs(rt.anchoredPosition.y) < 40f;

        /// <summary>The placeable elements under a container; nested centred holders (Arrow Keys) are opened.</summary>
        static void Collect(RectTransform container, List<RectTransform> into)
        {
            foreach (Transform child in container)
            {
                var c = (RectTransform)child;
                if (IsCentredContainer(c) && c.childCount > 0 && c.rect.width <= 101f)
                    Collect(c, into);
                else
                    into.Add(c);
            }
        }
    }
}
