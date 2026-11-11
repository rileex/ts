using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MicroCar.UI.EditorTools
{
    /// <summary>
    /// Building blocks for the Telemetry UI: nodes, texts, glass panels, buttons, segments, sliders.
    /// Sizes are canvas units (16 = the base text size; the canvas is 720 units tall on a desktop,
    /// 534 on touch).
    /// </summary>
    static class K
    {
        public const string Root = "Assets/_UI/Telemetry/";
        static Font semi, bold, monoMed, monoBold;

        public static Font Semi => semi ? semi : semi = AssetDatabase.LoadAssetAtPath<Font>(Root + "Fonts/ChakraPetch-SemiBold.ttf");
        public static Font Bold => bold ? bold : bold = AssetDatabase.LoadAssetAtPath<Font>(Root + "Fonts/ChakraPetch-Bold.ttf");
        public static Font MonoMed => monoMed ? monoMed : monoMed = AssetDatabase.LoadAssetAtPath<Font>(Root + "Fonts/IBMPlexMono-Medium.ttf");
        public static Font MonoBold => monoBold ? monoBold : monoBold = AssetDatabase.LoadAssetAtPath<Font>(Root + "Fonts/IBMPlexMono-Bold.ttf");

        public static Sprite S(string name)
        {
            var path = Root + "Sprites/" + name + (name.EndsWith(".jpg") ? "" : ".png");
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogError("Telemetry UI: missing sprite " + path);
            return s;
        }

        // --- placement --------------------------------------------------------------------------

        public static TeleSpot Spot(Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 offMin, Vector2 offMax) =>
            new TeleSpot { set = true, anchorMin = aMin, anchorMax = aMax, pivot = pivot, offsetMin = offMin, offsetMax = offMax };

        public static TeleSpot Pin(Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size) => TeleSpot.Box(anchor, pivot, pos, size);

        public static TeleSpot Full(float l = 0, float b = 0, float r = 0, float t = 0) =>
            Spot(Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(l, b), new Vector2(-r, -t));

        public static TeleSpot Hide => TeleSpot.Hidden;

        /// <summary>The component, added if missing (no "??": Unity's fake nulls defeat it).</summary>
        public static T Get<T>(Component c) where T : Component
        {
            var t = c.gameObject.GetComponent<T>();
            if (t == null) t = c.gameObject.AddComponent<T>();
            return t;
        }

        public static TeleLayout Lay(Component c, TeleSpot pc, TeleSpot? land = null, TeleSpot? port = null)
        {
            var l = Get<TeleLayout>(c);
            l.pc = pc;
            l.land = land ?? default;
            l.port = port ?? default;
            if (pc.set && !pc.hide) pc.ApplyTo((RectTransform)c.transform);
            return l;
        }

        /// <summary>Visible only with a keyboard in landscape (the key hints).</summary>
        public static void KeysOnly(Component c)
        {
            var l = Get<TeleLayout>(c);
            l.keysOnly = true;
            l.port = TeleSpot.Hidden;
        }

        public static void TouchOnly(Component c)
        {
            var l = Get<TeleLayout>(c);
            l.touchOnly = true;
        }

        // --- nodes -------------------------------------------------------------------------------

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(.5f, .5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            return rt;
        }

        public static Image Img(Component c, Sprite sprite, Color color, bool raycast = false)
        {
            var img = Get<Image>(c);
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero)
                img.type = Image.Type.Sliced;
            return img;
        }

        public static Image Pic(string name, Transform parent, Sprite sprite, Color color, float w, float h)
        {
            var rt = Node(name, parent);
            rt.sizeDelta = new Vector2(w, h);
            var img = Img(rt, sprite, color);
            img.preserveAspect = true;
            Size(rt, w, h);
            return img;
        }

        public static Image Glass(Component c, bool raycast = false) => Img(c, S("glass"), Color.white, raycast);

        public static Image Fill(string name, Transform parent, Color color, bool raycast = false)
        {
            var rt = Stretch(Node(name, parent));
            var img = Img(rt, null, color, raycast);
            return img;
        }

        public static Text Txt(string name, Transform parent, string text, Font font, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var rt = Node(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = false;
            t.raycastTarget = false;
            t.lineSpacing = 1f;
            rt.sizeDelta = new Vector2(10, size * 1.25f);
            return t;
        }

        /// <summary>Small mono caps label (the "lbl" of the mock-ups).</summary>
        public static Text Lbl(string name, Transform parent, string text, Color? color = null, int size = 12) =>
            Txt(name, parent, text, MonoMed, size, color ?? Tele.Muted);

        public static Text Num(string name, Transform parent, string text, int size, Color? color = null, TextAnchor anchor = TextAnchor.MiddleLeft) =>
            Txt(name, parent, text, MonoBold, size, color ?? Tele.Ink, anchor);

        public static Text Head(string name, Transform parent, string text, int size, Color? color = null) =>
            Txt(name, parent, text, Bold, size, color ?? Tele.Ink);

        public static LayoutElement Size(Component c, float w = -1, float h = -1, float flexW = -1, float flexH = -1)
        {
            var le = Get<LayoutElement>(c);
            if (w >= 0) { le.minWidth = w; le.preferredWidth = w; }
            if (h >= 0) { le.minHeight = h; le.preferredHeight = h; }
            le.flexibleWidth = flexW;
            le.flexibleHeight = flexH;
            return le;
        }

        public static HorizontalLayoutGroup Row(Component c, float spacing, TextAnchor align = TextAnchor.MiddleLeft, RectOffset pad = null)
        {
            var g = c.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.childAlignment = align;
            g.padding = pad ?? new RectOffset();
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = g.childForceExpandHeight = false;
            return g;
        }

        public static VerticalLayoutGroup Col(Component c, float spacing, TextAnchor align = TextAnchor.UpperLeft, RectOffset pad = null)
        {
            var g = c.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.childAlignment = align;
            g.padding = pad ?? new RectOffset();
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = false;
            return g;
        }

        public static void FitW(Component c) => Fit(c, true, false);
        public static void FitH(Component c) => Fit(c, false, true);

        public static void Fit(Component c, bool w, bool h)
        {
            var f = Get<ContentSizeFitter>(c);
            f.horizontalFit = w ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            f.verticalFit = h ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
        }

        public static RectTransform Spacer(Transform parent, float flex = 1f)
        {
            var rt = Node("Spacer", parent);
            Size(rt, 0, -1, flex);
            return rt;
        }

        public static Image Line(Transform parent, bool vertical, float length = -1)
        {
            var rt = Node("Line", parent);
            var img = Img(rt, null, Tele.Line);
            if (vertical) Size(rt, 1, length);
            else Size(rt, length, 1, length < 0 ? 1 : -1);
            return img;
        }

        // --- buttons ----------------------------------------------------------------------------

        public enum Look { Glass, Lime, Amber, Ghost }

        static void Behave(Button b, Graphic target)
        {
            b.targetGraphic = target;
            b.navigation = new Navigation { mode = Navigation.Mode.None }; // no Submit/arrow-key focus: shortcuts are the views'
            var cb = ColorBlock.defaultColorBlock;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(.86f, .9f, .82f, 1f);
            cb.pressedColor = new Color(.7f, .74f, .68f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(1f, 1f, 1f, .35f);
            cb.fadeDuration = .08f;
            b.colors = cb;
        }

        public static Button Btn(string name, Transform parent, Look look, string label, string index = null,
            string key = null, Sprite icon = null, float height = 48f, int fontSize = 16)
        {
            var rt = Node(name, parent);
            Image bg;
            Color ink;
            switch (look)
            {
                case Look.Lime: bg = Img(rt, S("solid"), Tele.Lime, true); ink = Tele.OnLime; break;
                case Look.Amber: bg = Img(rt, S("amber"), Color.white, true); ink = Tele.Amber; break;
                case Look.Ghost: bg = Img(rt, S("row"), Color.white, true); ink = Tele.Ink; break;
                default: bg = Glass(rt, true); ink = Tele.Ink; break;
            }
            var b = rt.gameObject.AddComponent<Button>();
            Behave(b, bg);
            Row(rt, 12, TextAnchor.MiddleLeft, new RectOffset(16, 14, 0, 0));
            Size(rt, -1, height, 1);

            if (index != null)
                Lbl("Index", rt, index, look == Look.Lime ? new Color(Tele.OnLime.r, Tele.OnLime.g, Tele.OnLime.b, .6f) : Tele.Muted);
            if (icon != null)
                Pic("Icon", rt, icon, ink, 18, 18);
            var text = Txt("Label", rt, label.ToUpperInvariant(), Semi, fontSize, ink);
            Size(text, -1, -1, 1);
            if (key != null)
                Key(rt, key, ink);
            return b;
        }

        /// <summary>A keyboard key hint, shown only with a keyboard in landscape.</summary>
        public static RectTransform Key(Transform parent, string key, Color ink)
        {
            var rt = Node("Key", parent);
            Img(rt, S("keycap"), new Color(ink.r, ink.g, ink.b, .75f));
            Row(rt, 0, TextAnchor.MiddleCenter, new RectOffset(7, 7, 0, 0));
            Size(rt, -1, 24);
            var t = Txt("Text", rt, key.ToUpperInvariant(), MonoMed, 11, new Color(ink.r, ink.g, ink.b, .85f), TextAnchor.MiddleCenter);
            KeysOnly(rt);
            return rt;
        }

        public static Button IconBtn(string name, Transform parent, Sprite icon, float size = 46f, float iconSize = 20f)
        {
            var rt = Node(name, parent);
            var bg = Glass(rt, true);
            var b = rt.gameObject.AddComponent<Button>();
            Behave(b, bg);
            rt.sizeDelta = new Vector2(size, size);
            Size(rt, size, size);
            var i = Pic("Icon", rt, icon, Tele.Ink, iconSize, iconSize);
            var irt = (RectTransform)i.transform;
            irt.anchorMin = irt.anchorMax = new Vector2(.5f, .5f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = new Vector2(iconSize, iconSize);
            return b;
        }

        public static void On(Button b, UnityAction call) => UnityEventTools.AddPersistentListener(b.onClick, call);

        public static void On(Button b, UnityAction<GameObject> call, GameObject arg) =>
            UnityEventTools.AddObjectPersistentListener(b.onClick, call, arg);

        public static void On(Button b, UnityAction<int> call, int arg) =>
            UnityEventTools.AddIntPersistentListener(b.onClick, call, arg);

        // --- controls ---------------------------------------------------------------------------

        public static TeleSegment Segment(string name, Transform parent, params string[] options)
        {
            var rt = Node(name, parent);
            Img(rt, S("row"), Color.white);
            Row(rt, 2, TextAnchor.MiddleCenter, new RectOffset(2, 2, 2, 2));
            Size(rt, -1, 32);
            var seg = rt.gameObject.AddComponent<TeleSegment>();
            seg.options = new Button[options.Length];
            seg.backs = new Image[options.Length];
            seg.labels = new Text[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                var o = Node(options[i], rt);
                var back = Img(o, S("solid"), Color.clear, true);
                var b = o.gameObject.AddComponent<Button>();
                Behave(b, back);
                Row(o, 0, TextAnchor.MiddleCenter, new RectOffset(11, 11, 0, 0));
                Size(o, -1, 28);
                var t = Txt("Text", o, options[i].ToUpperInvariant(), MonoMed, 12, Tele.Muted, TextAnchor.MiddleCenter);
                seg.options[i] = b;
                seg.backs[i] = back;
                seg.labels[i] = t;
            }
            return seg;
        }

        public static Slider MakeSlider(string name, Transform parent, float width, out Text value)
        {
            var row = Node(name, parent);
            Row(row, 12, TextAnchor.MiddleRight);
            Size(row, width + 48, 32);

            var rt = Node("Slider", row);
            Size(rt, width, 28);
            var slider = rt.gameObject.AddComponent<Slider>();
            slider.navigation = new Navigation { mode = Navigation.Mode.None };

            var back = Node("Background", rt);
            back.anchorMin = new Vector2(0, .5f); back.anchorMax = new Vector2(1, .5f);
            back.sizeDelta = new Vector2(0, 2); back.anchoredPosition = Vector2.zero;
            Img(back, null, Tele.Dim, true);

            var area = Node("Fill Area", rt);
            area.anchorMin = new Vector2(0, .5f); area.anchorMax = new Vector2(1, .5f);
            area.sizeDelta = new Vector2(0, 2); area.anchoredPosition = Vector2.zero;
            var fill = Node("Fill", area);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1); fill.sizeDelta = Vector2.zero;
            Img(fill, null, Tele.Lime);

            var handleArea = Node("Handle Slide Area", rt);
            Stretch(handleArea);
            var handle = Node("Handle", handleArea);
            handle.sizeDelta = new Vector2(7, 20);
            var hImg = Img(handle, null, Tele.Ink, true);

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = hImg;
            slider.direction = Slider.Direction.LeftToRight;

            value = Num("Value", row, "100", 14, Tele.Ink, TextAnchor.MiddleRight);
            Size(value, 36, -1);
            return slider;
        }
    }
}
