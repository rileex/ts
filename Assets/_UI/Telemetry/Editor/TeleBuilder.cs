using System.Collections.Generic;
using System.Linq;
using ALIyerEdon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MicroCar.UI.EditorTools
{
    /// <summary>
    /// Builds the Telemetry UI into the Garage and Village scenes, in place: the template's logic
    /// objects (CarSelect, LevelSelect, SettingsMenu, Start_Finish_UI, Pause_Menu …) stay where they
    /// are and become the screen roots; their old visuals are removed, the new ones are created and
    /// the template's UI fields are pointed at them. Safe to run again: everything it made is rebuilt.
    /// </summary>
    public static partial class TeleBuilder
    {
        const string GarageScene = "Assets/Off_Road_Racing_3/Scene/Garage.unity";
        const string RaceScene = "Assets/Off_Road_Racing_3/Scene/Village.unity";
        static readonly Vector2 Half = new Vector2(.5f, .5f);

        [MenuItem("MicroCar/UI/Build Telemetry UI (open scene)")]
        public static void BuildOpenScene()
        {
            ImportSettings();
            if (Object.FindFirstObjectByType<CarSelect>(FindObjectsInactive.Include) != null)
                BuildGarage();
            else if (Object.FindFirstObjectByType<Race_Manager>(FindObjectsInactive.Include) != null)
                BuildRace();
            else
            {
                Debug.LogError("Telemetry UI: open the Garage or the Village scene first.");
                return;
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        /// <summary>Both scenes, saved. For batch mode: -executeMethod MicroCar.UI.EditorTools.TeleBuilder.BuildAll</summary>
        public static void BuildAll()
        {
            ImportSettings();
            foreach (var path in new[] { GarageScene, RaceScene })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (path == GarageScene) BuildGarage(); else BuildRace();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Telemetry UI: built " + path);
            }
        }

        const string TouchMenu = "MicroCar/UI/Simulate Touch (editor)";

        [MenuItem(TouchMenu)]
        static void ToggleTouch() => EditorPrefs.SetBool(TeleScreen.ForceTouchKey, !EditorPrefs.GetBool(TeleScreen.ForceTouchKey, false));

        [MenuItem(TouchMenu, true)]
        static bool ToggleTouchCheck()
        {
            Menu.SetChecked(TouchMenu, EditorPrefs.GetBool(TeleScreen.ForceTouchKey, false));
            return true;
        }

        // --------------------------------------------------------------------------------------------
        // Assets
        // --------------------------------------------------------------------------------------------

        static readonly string[] Sliced = { "glass", "row", "row_sel", "solid", "outline", "amber", "keycap" };

        /// <summary>Sprite import settings for everything in Sprites/ (2x art: 200 pixels per unit).</summary>
        public static void ImportSettings()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { K.Root + "Sprites" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                bool big = name == "logo" || name == "track_village";
                bool changed = imp.textureType != TextureImporterType.Sprite;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.filterMode = FilterMode.Bilinear;
                imp.spritePixelsPerUnit = big ? 100 : 200;
                imp.textureCompression = big ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
                imp.maxTextureSize = big ? 1024 : 256;
                imp.spriteBorder = Sliced.Contains(name) ? new Vector4(12, 12, 12, 12) : Vector4.zero;
                if (changed || EditorUtility.IsDirty(imp))
                    imp.SaveAndReimport();
            }
        }

        // --------------------------------------------------------------------------------------------
        // Shared helpers
        // --------------------------------------------------------------------------------------------

        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        /// <summary>Turns a template object into a full-screen screen root: no image, no old children.</summary>
        static RectTransform Holder(Transform t, params string[] keep)
        {
            var img = t.GetComponent<Image>();
            if (img) Object.DestroyImmediate(img);
            foreach (var child in t.Cast<Transform>().ToArray())
                if (!keep.Contains(child.name))
                    Object.DestroyImmediate(child.gameObject);
            return K.Stretch((RectTransform)t);
        }

        static TeleCanvas PrepareCanvas(Canvas canvas)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            var tc = K.Get<TeleCanvas>(canvas);
            return tc;
        }

        /// <summary>The scene left in the desktop layout (what the editor shows).</summary>
        static void Finish(TeleCanvas tc, List<RectTransform> safes)
        {
            tc.safeRects = safes.ToArray();
            foreach (var rt in safes)
            {
                K.Stretch(rt);
                rt.offsetMin = new Vector2(TeleCanvas.Margins[0].x, TeleCanvas.Margins[0].y);
                rt.offsetMax = new Vector2(-TeleCanvas.Margins[0].z, -TeleCanvas.Margins[0].w);
            }
            foreach (var l in tc.GetComponentsInChildren<TeleLayout>(true))
                l.Apply(TelePreset.Pc);
        }

        static RectTransform Safe(Transform parent, List<RectTransform> safes)
        {
            var rt = K.Stretch(K.Node("T_Safe", parent));
            safes.Add(rt);
            return rt;
        }

        static Image Scrim(string name, Transform parent, string sprite)
        {
            var img = K.Fill(name, parent, Color.white);
            img.sprite = K.S(sprite);
            img.type = Image.Type.Simple;
            return img;
        }

        static void FontSizes(Component c, float pc, float land, float port) =>
            K.Get<TeleLayout>(c).fontSize = new Vector3(pc, land, port);

        static void Scales(Component c, float pc, float land, float port) =>
            K.Get<TeleLayout>(c).scale = new Vector3(pc, land, port);

        static RectTransform CreditsChip(Transform parent, out Text value, bool inLayout = false)
        {
            var chip = K.Node("Credits", parent);
            K.Glass(chip);
            K.Row(chip, 10, TextAnchor.MiddleCenter, new RectOffset(14, 16, 0, 0));
            if (inLayout) K.Size(chip, -1, 46);
            else K.FitW(chip);
            K.Lbl("Label", chip, "CR");
            value = K.Num("Value", chip, "1 400", 18, Tele.Amber);
            return chip;
        }

        static RectTransform Head(Transform parent, string crumb, string title, UnityAction back, UnityAction click, out Button backButton)
        {
            var head = K.Node("Head", parent);
            K.Row(head, 14, TextAnchor.MiddleLeft);
            backButton = K.IconBtn("Back", head, K.S("icon_chev_l"), 46, 22);
            K.On(backButton, back);
            if (click != null) K.On(backButton, click);
            var c = K.Lbl("Crumb", head, crumb);
            K.Lay(c, default, null, K.Hide);
            var t = K.Head("Title", head, title, 30);
            FontSizes(t, 30, 26, 26);
            K.Spacer(head);
            return head;
        }

        static RectTransform Dialog(Transform parent, string name, string title, string body, out RectTransform buttons, out Text bodyText)
        {
            var root = K.Stretch(K.Node(name, parent));
            K.Img(root, null, new Color(Tele.Bg.r, Tele.Bg.g, Tele.Bg.b, .9f), true);
            var card = K.Node("Card", root);
            K.Glass(card, true);
            K.Col(card, 16, TextAnchor.UpperLeft, new RectOffset(24, 24, 22, 22));
            K.FitH(card);
            card.anchorMin = card.anchorMax = Half;
            card.pivot = Half;
            card.sizeDelta = new Vector2(440, 200);
            K.Head("Title", card, title, 22);
            bodyText = K.Txt("Body", card, body, K.Semi, 15, Tele.Muted);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            buttons = K.Node("Buttons", card);
            K.Row(buttons, 10, TextAnchor.MiddleRight);
            buttons.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            root.gameObject.SetActive(false);
            return root;
        }

        static RectTransform Cell(Transform parent, string label, string value, out Text valueText, int size = 22)
        {
            var cell = K.Node(label, parent);
            K.Col(cell, 6, TextAnchor.UpperLeft, new RectOffset(0, 8, 12, 12));
            K.Size(cell, -1, -1, 1);
            K.Lbl("Label", cell, label.ToUpperInvariant());
            valueText = K.Num("Value", cell, value, size);
            return cell;
        }

        // --------------------------------------------------------------------------------------------
        // Garage
        // --------------------------------------------------------------------------------------------

        static void BuildGarage()
        {
            var carSelect = Find<CarSelect>();
            var levelSelect = Find<LevelSelect>();
            var menuSettings = Find<SettingsMenu>();
            var util = Find<MainUtility>();
            var canvas = util.GetComponent<Canvas>();
            if (canvas == null) canvas = carSelect.GetComponentInParent<Canvas>(true).rootCanvas;
            var root = canvas.transform;
            var tc = PrepareCanvas(canvas);
            var safes = new List<RectTransform>();
            UnityAction click = util.Click_Sound;

            foreach (var child in root.Cast<Transform>().ToArray())
                if (child.name == "Background" || child.name == "Main_Menu" || child.name == "Exit_Menu"
                    || child.name == "Loading" || child.name.StartsWith("T_"))
                    Object.DestroyImmediate(child.gameObject);

            MoveToLegacy(menuSettings.transform);
            var garage = Holder(carSelect.transform);
            var tracks = Holder(levelSelect.transform);
            var settings = Holder(menuSettings.transform, "Legacy");
            garage.gameObject.SetActive(true);
            tracks.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);

            var gv = K.Get<GarageView>(garage);
            var tv = K.Get<TracksView>(tracks);

            // ---- Garage ------------------------------------------------------------------------
            var scrimH = Scrim("T_ScrimH", garage, "scrim_garage_h");
            K.Lay(scrimH, K.Full(), null, K.Hide);
            var scrimV = Scrim("T_ScrimV", garage, "scrim_garage_v");
            K.Lay(scrimV, K.Hide, K.Hide, K.Full());

            // The viewfinder around the car on the shelf (measured: the car fills x .29-.69, y .15-.8).
            var frame = K.Node("T_CarFrame", garage);
            K.Lay(frame,
                K.Spot(new Vector2(.28f, .12f), new Vector2(.74f, .84f), Half, Vector2.zero, Vector2.zero),
                K.Spot(new Vector2(.30f, .10f), new Vector2(.76f, .86f), Half, Vector2.zero, Vector2.zero),
                K.Spot(new Vector2(.04f, .46f), new Vector2(.96f, .84f), Half, Vector2.zero, Vector2.zero));
            var tl = K.Node("Corner", frame);
            K.Img(tl, K.S("bracket"), Color.white);
            K.Pin(new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(38, 38)).ApplyTo(tl);
            var br = K.Node("Corner", frame);
            K.Img(br, K.S("bracket"), Color.white);
            K.Pin(new Vector2(1, 0), new Vector2(0, 1), Vector2.zero, new Vector2(38, 38)).ApplyTo(br);
            br.localScale = new Vector3(-1, -1, 1);

            var lockBadge = K.Node("Locked", frame);
            K.Glass(lockBadge);
            K.Row(lockBadge, 8, TextAnchor.MiddleCenter, new RectOffset(12, 14, 0, 0));
            K.FitW(lockBadge);
            K.Pin(new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -4), new Vector2(110, 34)).ApplyTo(lockBadge);
            K.Pic("Icon", lockBadge, K.S("icon_lock"), Tele.Amber, 16, 16);
            K.Lbl("Text", lockBadge, "LOCKED", Tele.Amber);

            var safe = Safe(garage, safes);
            var logo = K.Pic("Logo", safe, K.S("logo"), Color.white, 224, 66);
            K.Lay(logo,
                K.Pin(new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(224, 66)),
                K.Pin(new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(160, 47)),
                K.Pin(new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(150, 44)));

            var chip = CreditsChip(safe, out var credits);
            K.Lay(chip, K.Pin(Vector2.one, Vector2.one, Vector2.zero, new Vector2(130, 46)));

            var rail = K.Node("Rail", safe);
            K.Col(rail, 18, TextAnchor.LowerLeft);
            K.Lay(rail,
                K.Spot(Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero, new Vector2(320, -96)),
                K.Spot(Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero, new Vector2(270, -64)),
                K.Spot(Vector2.zero, new Vector2(1, .6f), new Vector2(.5f, 0), Vector2.zero, Vector2.zero));
            Scales(rail, 1f, .86f, 1f);

            var info = K.Node("Info", rail);
            K.Col(info, 6);
            var status = K.Node("Status", info);
            K.Row(status, 8);
            K.Lbl("Label", status, "YOUR CAR");
            K.Spacer(status);
            var statusIcon = K.Pic("Icon", status, K.S("icon_check"), Tele.Lime, 14, 14);
            var statusText = K.Lbl("Text", status, "OWNED", Tele.Lime);
            var carName = K.Head("Name", info, "FIREWALL", 42);
            FontSizes(carName, 42, 34, 36);
            var carType = K.Lbl("Type", info, "BUGGY", Tele.Muted, 13);
            var stepper = K.Node("Stepper", info);
            K.Row(stepper, 12, TextAnchor.MiddleLeft, new RectOffset(0, 0, 8, 0));
            var prev = K.IconBtn("Prev", stepper, K.S("icon_chev_l"), 54, 22);
            K.On(prev, carSelect.PrevCar);
            K.On(prev, click);
            var carIndex = K.Num("Index", stepper, "01/05", 17);
            K.Size(carIndex, 60, -1);
            var pagerRow = K.Node("Pager", stepper);
            K.Row(pagerRow, 5, TextAnchor.MiddleCenter);
            K.Size(pagerRow, -1, -1, 1);
            var pager = new Image[carSelect.cars.Length];
            for (int i = 0; i < pager.Length; i++)
            {
                var bar = K.Node("Bar", pagerRow);
                pager[i] = K.Img(bar, null, i == 0 ? Tele.Lime : Tele.Dim);
                K.Size(bar, 24, 3);
            }
            var next = K.IconBtn("Next", stepper, K.S("icon_chev_r"), 54, 22);
            K.On(next, carSelect.NextCar);
            K.On(next, click);

            var menu = K.Node("Menu", rail);
            K.Col(menu, 7);
            var buy = K.Btn("Buy", menu, K.Look.Amber, "Buy car", null, null, K.S("icon_lock"), 52, 16);
            var price = K.Num("Price", buy.transform, "2 000 CR", 15, Tele.Amber);
            K.Key(buy.transform, "Enter", Tele.Amber);
            K.On(buy, carSelect.Buy_CurrentCar);
            K.On(buy, click);
            var startGroup = K.Node("StartGroup", menu);
            K.Col(startGroup, 0);
            var group = startGroup.gameObject.AddComponent<CanvasGroup>();
            var start = K.Btn("Start", startGroup, K.Look.Lime, "Start race", "01", "Enter", null, 60, 18);
            K.On(start, carSelect.SelectCar);
            K.On(start, click);
            var tracksBtn = K.Btn("Tracks", menu, K.Look.Glass, "Tracks", "02", "T");
            K.On(tracksBtn, gv.OpenTracks);
            K.On(tracksBtn, click);
            var settingsBtn = K.Btn("Settings", menu, K.Look.Glass, "Settings", "03", "S");
            K.On(settingsBtn, gv.OpenSettings);
            K.On(settingsBtn, click);
            var exitBtn = K.Btn("Exit", menu, K.Look.Glass, "Exit", "04");

            var card = K.Node("Track", rail);
            K.Glass(card);
            K.Col(card, 6, TextAnchor.UpperLeft, new RectOffset(16, 16, 14, 14));
            K.Lay(card, default, K.Hide, K.Hide);
            var cardTop = K.Node("Top", card);
            K.Row(cardTop, 8);
            K.Lbl("Label", cardTop, "TRACK");
            K.Spacer(cardTop);
            var trackBest = K.Lbl("Best", cardTop, "BEST P1", Tele.Lime);
            var trackName = K.Head("Name", card, "VILLAGE", 21);
            var trackInfo = K.Lbl("Info", card, "2 LAPS · 5 RACERS", Tele.Muted, 11);

            // Dialogs over the garage.
            var buyDialog = Dialog(garage, "T_BuyDialog", "BUY THIS CAR?", "The price is taken from your credits.", out var buyButtons, out _);
            var cancel = K.Btn("Cancel", buyButtons, K.Look.Glass, "Cancel");
            K.On(cancel, util.SetFalse, buyDialog.gameObject);
            K.On(cancel, click);
            var confirm = K.Btn("Buy", buyButtons, K.Look.Lime, "Buy");
            K.On(confirm, carSelect.BuyCar);
            K.On(confirm, click);

            var noMoney = Dialog(garage, "T_NoMoney", "NOT ENOUGH CREDITS", "Win races to earn more.", out var noButtons, out _);
            var ok = K.Btn("OK", noButtons, K.Look.Lime, "OK");
            K.On(ok, util.SetFalse, noMoney.gameObject);
            K.On(ok, util.SetFalse, buyDialog.gameObject);
            K.On(ok, click);

            // ---- Tracks ------------------------------------------------------------------------
            K.Fill("T_Dim", tracks, new Color(Tele.Bg.r, Tele.Bg.g, Tele.Bg.b, .985f), true); // linear blending: .985 reads as ~.9
            var tSafe = Safe(tracks, safes);
            var tHead = Head(tSafe, "GARAGE /", "TRACKS", tv.Back, click, out _);
            K.Lay(tHead, K.Spot(new Vector2(0, 1), Vector2.one, new Vector2(.5f, 1), new Vector2(0, -46), Vector2.zero));
            CreditsChip(tHead, out var tCredits, true);

            var list = K.Node("List", tSafe);
            K.Col(list, 7);
            K.Lay(list,
                K.Spot(Vector2.zero, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(360, -66)),
                K.Spot(Vector2.zero, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(230, -58)),
                K.Spot(Vector2.zero, new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 123)));
            var row1 = K.Node("Village", list);
            K.Img(row1, K.S("row_sel"), Color.white);
            K.Row(row1, 14, TextAnchor.MiddleLeft, new RectOffset(16, 14, 0, 0));
            K.Size(row1, -1, 58);
            K.Lay(row1, default, null, K.Hide);
            K.Lbl("Index", row1, "01", Tele.Lime, 13);
            var rowName = K.Txt("Name", row1, "VILLAGE", K.Semi, 17, Tele.Ink);
            K.Size(rowName, -1, -1, 1);
            var levelPrice = K.Num("Price", row1, "", 13, Tele.Amber);
            var levelLock = K.Pic("Lock", row1, K.S("icon_lock"), Tele.Amber, 16, 16);
            var levelMedal = K.Pic("Medal", row1, K.S("medal_none"), Color.white, 28, 28);
            for (int i = 2; i <= 3; i++)
            {
                var r = K.Node("Locked" + i, list);
                K.Img(r, K.S("row"), Color.white);
                K.Row(r, 14, TextAnchor.MiddleLeft, new RectOffset(16, 16, 0, 0));
                K.Size(r, -1, 58);
                K.Lbl("Index", r, Tele.Two(i), Tele.Muted, 13);
                var n = K.Txt("Name", r, "COMING SOON", K.Semi, 17, Tele.Muted);
                K.Size(n, -1, -1, 1);
                K.Pic("Lock", r, K.S("icon_lock"), Tele.Muted, 16, 16);
            }

            var detail = K.Node("Detail", tSafe);
            K.Lay(detail,
                K.Spot(Vector2.zero, Vector2.one, Half, new Vector2(376, 0), new Vector2(0, -66)),
                K.Spot(Vector2.zero, Vector2.one, Half, new Vector2(246, 0), new Vector2(0, -58)),
                K.Spot(Vector2.zero, Vector2.one, Half, new Vector2(0, 139), new Vector2(0, -62)));
            var pic = K.Node("Picture", detail);
            pic.gameObject.AddComponent<RectMask2D>();
            K.Lay(pic,
                K.Spot(Vector2.zero, Vector2.one, Half, Vector2.zero, new Vector2(-288, 0)),
                K.Spot(Vector2.zero, Vector2.one, Half, Vector2.zero, new Vector2(-240, 0)),
                K.Spot(new Vector2(0, 1), Vector2.one, new Vector2(.5f, 1), new Vector2(0, -250), Vector2.zero));
            var photo = K.Stretch(K.Node("Photo", pic));
            K.Img(photo, K.S("track_village.jpg"), Color.white);
            var fitter = photo.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            var frameLine = K.Stretch(K.Node("Border", pic));
            K.Img(frameLine, K.S("outline"), Tele.Line);
            var caption = K.Node("Caption", pic);
            K.Col(caption, 2);
            K.Fit(caption, true, true);
            K.Pin(Vector2.zero, Vector2.zero, new Vector2(18, 16), new Vector2(200, 60)).ApplyTo(caption);
            K.Lbl("Label", caption, "TRACK 01", Tele.Ink);
            var capName = K.Head("Name", caption, "VILLAGE", 42);
            FontSizes(capName, 42, 30, 34);
            capName.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, .6f);

            var stats = K.Node("Stats", detail);
            K.Glass(stats);
            K.Col(stats, 0, TextAnchor.UpperLeft, new RectOffset(16, 16, 4, 4));
            K.Lay(stats,
                K.Spot(new Vector2(1, 0), Vector2.one, new Vector2(1, 1), new Vector2(-272, 74), Vector2.zero),
                K.Spot(new Vector2(1, 0), Vector2.one, new Vector2(1, 1), new Vector2(-226, 64), Vector2.zero),
                K.Spot(Vector2.zero, Vector2.one, new Vector2(.5f, 1), new Vector2(0, 74), new Vector2(0, -266)));
            var sRow1 = K.Node("Row", stats);
            K.Row(sRow1, 0);
            sRow1.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            Cell(sRow1, "Laps", "2", out _);
            Cell(sRow1, "Racers", "5", out _);
            K.Line(stats, false);
            var sRow2 = K.Node("Row", stats);
            K.Row(sRow2, 0);
            sRow2.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            Cell(sRow2, "Best", "—", out var bestText);
            Cell(sRow2, "Status", "OPEN", out var statusValue);
            K.Line(stats, false);
            var prize = K.Node("Prize", stats);
            K.Col(prize, 8, TextAnchor.UpperLeft, new RectOffset(0, 0, 12, 12));
            K.Lay(prize, default, K.Hide, null);
            K.Lbl("Label", prize, "PRIZE · CR");
            var prizeRow = K.Node("Places", prize);
            K.Row(prizeRow, 8);
            foreach (var (medal, amount) in new[] { ("medal_gold", "3 000"), ("medal_silver", "2 000"), ("medal_bronze", "1 000") })
            {
                K.Pic(medal, prizeRow, K.S(medal), Color.white, 20, 20);
                var a = K.Num("Amount", prizeRow, amount, 14);
                K.Size(a, -1, -1, 1);
            }

            var cta = K.Btn("Start", detail, K.Look.Lime, "Start race", null, "Enter", K.S("icon_play"), 60, 18);
            K.Lay(cta,
                K.Pin(new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(272, 60)),
                K.Pin(new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(226, 56)),
                K.Spot(Vector2.zero, new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 60)));
            K.On(cta, tv.StartRace);
            K.On(cta, click);

            var levelDialog = Dialog(tracks, "T_UnlockDialog", "UNLOCK THIS TRACK?", "", out var lvButtons, out var lvInfo);
            var lvCancel = K.Btn("Cancel", lvButtons, K.Look.Glass, "Cancel");
            K.On(lvCancel, util.SetFalse, levelDialog.gameObject);
            var lvBuy = K.Btn("Unlock", lvButtons, K.Look.Lime, "Unlock");
            K.On(lvBuy, levelSelect.BuyLevel);
            K.On(lvBuy, click);

            // ---- Settings -----------------------------------------------------------------------
            BuildSettings(settings, menuSettings, garage.gameObject, "GARAGE /", click, safes);

            // ---- Canvas-level: exit question, loading ------------------------------------------
            var exitDialog = Dialog(root, "T_ExitDialog", "QUIT THE GAME?", "", out var exButtons, out _);
            var no = K.Btn("No", exButtons, K.Look.Glass, "No");
            K.On(no, util.SetFalse, exitDialog.gameObject);
            var yes = K.Btn("Yes", exButtons, K.Look.Lime, "Quit");
            K.On(yes, util.Exit);
            K.On(exitBtn, util.Exit); // WebHost hides buttons wired to MainUtility.Exit in the browser
            K.On(exitBtn, click);

            var loading = K.Stretch(K.Node("T_Loading", root));
            K.Img(loading, null, Tele.Bg, true);
            var loadCol = K.Node("Text", loading);
            K.Col(loadCol, 8, TextAnchor.MiddleCenter);
            K.Fit(loadCol, true, true);
            loadCol.anchorMin = loadCol.anchorMax = loadCol.pivot = Half;
            K.Head("Title", loadCol, "LOADING", 30).alignment = TextAnchor.MiddleCenter;
            K.Lbl("Info", loadCol, "GETTING THE TRACK READY").alignment = TextAnchor.MiddleCenter;
            loading.gameObject.SetActive(false);

            // A sink for LevelSelect's "background" switch (the old dimmer behind its menus).
            var stub = K.Node("T_BackgroundStub", root);
            stub.gameObject.SetActive(false);

            // Order, back to front.
            garage.SetSiblingIndex(0);
            tracks.SetSiblingIndex(1);
            settings.SetSiblingIndex(2);
            exitDialog.SetAsLastSibling();
            loading.SetAsLastSibling();
            var es = root.Find("EventSystem");
            if (es) es.SetAsLastSibling();

            // ---- Wiring ------------------------------------------------------------------------
            carSelect.lockIcon = lockBadge.gameObject;
            carSelect.buyButton = buy.gameObject;
            carSelect.Price = price.gameObject;
            carSelect.noEnoughScores = noMoney.gameObject;
            carSelect.purchaseUI = buyDialog.gameObject;
            carSelect.TotalScores = credits;
            carSelect.carPriceText = price;
            carSelect.Loading = loading.gameObject;
            util.Loading = loading.gameObject;
            util.exitMenu = exitDialog.gameObject;
            util.carSelect = carSelect;

            levelSelect.backgroundUI = stub.gameObject;
            levelSelect.carSelectUI = garage.gameObject;
            levelSelect.purchaseUI = levelDialog.gameObject;
            levelSelect.TotalScores = tCredits;
            levelSelect.purchaseInfo = lvInfo;
            levelSelect.levelPriceText = new[] { levelPrice };
            levelSelect.levelLocks = new[] { levelLock.gameObject };
            levelSelect.awardIcons = new[] { levelMedal };
            levelSelect.goldAward = K.S("medal_gold");
            levelSelect.silverAward = K.S("medal_silver");
            levelSelect.bronzeAward = K.S("medal_bronze");
            levelSelect.noneAward = K.S("medal_none");

            gv.carSelect = carSelect;
            gv.levelSelect = levelSelect;
            gv.tracksScreen = tracks.gameObject;
            gv.settingsScreen = settings.gameObject;
            gv.blockers = new[] { buyDialog.gameObject, noMoney.gameObject, exitDialog.gameObject, loading.gameObject };
            gv.carName = carName;
            gv.carType = carType;
            gv.carIndex = carIndex;
            gv.status = statusText;
            gv.statusIcon = statusIcon;
            gv.ownedIcon = K.S("icon_check");
            gv.lockedIcon = K.S("icon_lock");
            gv.pager = pager;
            gv.startGroup = group;
            gv.trackName = trackName;
            gv.trackBest = trackBest;
            gv.trackInfo = trackInfo;

            tv.levelSelect = levelSelect;
            tv.carSelect = carSelect;
            tv.garageScreen = garage.gameObject;
            tv.blockers = new[] { levelDialog.gameObject, loading.gameObject };
            tv.bestText = bestText;
            tv.statusText = statusValue;

            foreach (var o in new Object[] { carSelect, levelSelect, util, gv, tv })
                EditorUtility.SetDirty(o);

            Finish(tc, safes);
        }

        // --------------------------------------------------------------------------------------------
        // Settings (garage and race)
        // --------------------------------------------------------------------------------------------

        /// <summary>Puts the template's settings controls in an inactive "Legacy" child (first run only).</summary>
        static void MoveToLegacy(Transform settings)
        {
            if (settings.Find("Legacy") != null) return;
            var legacy = K.Node("Legacy", settings);
            foreach (var child in settings.Cast<Transform>().ToArray())
                if (child != legacy)
                    child.SetParent(legacy, true);
            legacy.gameObject.SetActive(false);
        }

        const float SetRow = 46f, SetHead = 30f, SetGap = 18f;

        static RectTransform Group(Transform parent, string title)
        {
            var g = K.Node(title, parent);
            K.Col(g, 0);
            var h = K.Lbl("Title", g, "// " + title.ToUpperInvariant(), Tele.Lime);
            K.Size(h, -1, SetHead);
            return g;
        }

        static RectTransform SetLine(Transform group, string label)
        {
            var row = K.Node(label, group);
            K.Row(row, 12, TextAnchor.MiddleLeft);
            K.Size(row, -1, SetRow);
            var line = K.Node("Line", row);
            K.Img(line, null, Tele.Line);
            K.Get<LayoutElement>(line).ignoreLayout = true;
            line.anchorMin = new Vector2(0, 1);
            line.anchorMax = Vector2.one;
            line.pivot = new Vector2(.5f, 1);
            line.sizeDelta = new Vector2(0, 1);
            line.anchoredPosition = Vector2.zero;
            var t = K.Txt("Label", row, label.ToUpperInvariant(), K.Semi, 15, Tele.Ink);
            K.Size(t, -1, -1, 1);
            return row;
        }

        static void Gap(Transform parent)
        {
            var g = K.Node("Gap", parent);
            K.Size(g, -1, SetGap);
        }

        static void BuildSettings(RectTransform root, SettingsMenu menu, GameObject returnTo, string crumb, UnityAction click, List<RectTransform> safes)
        {
            var sv = K.Get<SettingsView>(root);
            K.Fill("T_Dim", root, Tele.Bg, true); // a page of its own: in linear blending even 1% of the pause shows through
            var safe = Safe(root, safes);
            var head = Head(safe, crumb, "SETTINGS", sv.Close, click, out _);
            K.Lay(head, K.Spot(new Vector2(0, 1), Vector2.one, new Vector2(.5f, 1), new Vector2(0, -46), Vector2.zero));

            float aHeight = SetHead + 4 * SetRow + SetGap + SetHead + 2 * SetRow;
            var colA = K.Node("ColA", safe);
            K.Col(colA, 0);
            K.FitH(colA);
            K.Lay(colA,
                K.Spot(new Vector2(0, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -66 - aHeight), new Vector2(-20, -66)),
                K.Spot(new Vector2(0, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -56 - aHeight), new Vector2(-14, -56)),
                K.Spot(new Vector2(0, 1), Vector2.one, new Vector2(.5f, 1), new Vector2(0, -62 - aHeight), new Vector2(0, -62)));
            var colB = K.Node("ColB", safe);
            K.Col(colB, 0);
            K.FitH(colB);
            float bTop = 62 + aHeight + SetGap;
            K.Lay(colB,
                K.Spot(new Vector2(.5f, 1), Vector2.one, new Vector2(.5f, 1), new Vector2(20, -366), new Vector2(0, -66)),
                K.Spot(new Vector2(.5f, 1), Vector2.one, new Vector2(.5f, 1), new Vector2(14, -356), new Vector2(0, -56)),
                K.Spot(new Vector2(0, 1), Vector2.one, new Vector2(.5f, 1), new Vector2(0, -bTop - 300), new Vector2(0, -bTop)));

            // Graphics
            var gfx = Group(colA, "Graphics");
            var q = SetLine(gfx, "Quality");
            var stepper = K.Node("Stepper", q);
            K.Row(stepper, 10, TextAnchor.MiddleRight);
            var down = QualityArrow("Lower", stepper, "icon_chev_l");
            var qv = K.Node("Value", stepper);
            K.Col(qv, 5, TextAnchor.MiddleCenter);
            K.Size(qv, 104, -1);
            var qText = K.Num("Text", qv, "HIGH", 14, Tele.Ink, TextAnchor.MiddleCenter);
            var ticksRow = K.Node("Ticks", qv);
            K.Row(ticksRow, 3, TextAnchor.MiddleCenter);
            var ticks = new Image[6];
            for (int i = 0; i < ticks.Length; i++)
            {
                var tick = K.Node("Tick", ticksRow);
                ticks[i] = K.Img(tick, null, Tele.Dim);
                K.Size(tick, 9, 4);
            }
            var up = QualityArrow("Higher", stepper, "icon_chev_r");
            var sun = K.Segment("Seg", SetLine(gfx, "Sun shafts"), "Off", "On");
            var smoke = K.Segment("Seg", SetLine(gfx, "Wheel smoke"), "Off", "On");
            var balloons = K.Segment("Seg", SetLine(gfx, "Balloons"), "Off", "On");
            Gap(colA);
            var sound = Group(colA, "Sound");
            var music = K.MakeSlider("Slider", SetLine(sound, "Music"), 150, out var musicValue);
            var effects = K.MakeSlider("Slider", SetLine(sound, "Effects"), 150, out var effectsValue);

            // Race screen
            var race = Group(colB, "Race screen");
            var standings = K.Segment("Seg", SetLine(race, "Standings"), "Off", "On");
            var fps = K.Segment("Seg", SetLine(race, "FPS counter"), "Off", "On");
            var labels = K.Segment("Seg", SetLine(race, "Car labels"), "Off", "On");

            var gapC = K.Node("Gap", colB);
            K.Size(gapC, -1, SetGap);
            var controls = Group(colB, "Controls");
            K.TouchOnly(controls);
            var steering = K.Segment("Seg", SetLine(controls, "Steering"), "Buttons", "Stick", "Tilt");
            var tilt = K.MakeSlider("Slider", SetLine(controls, "Tilt sensitivity"), 130, out var tiltValue);

            var keys = Group(colB, "Keyboard");
            K.KeysOnly(keys);
            foreach (var (label, caps) in new[]
            {
                ("Drive", new[] { "WASD", "Arrows" }), ("Handbrake", new[] { "Space" }),
                ("Nitro", new[] { "Shift" }), ("Camera · Pause", new[] { "C", "Esc" }),
            })
            {
                var row = SetLine(keys, label);
                foreach (var cap in caps)
                {
                    var key = K.Key(row, cap, Tele.Ink);
                    Object.DestroyImmediate(key.GetComponent<TeleLayout>()); // the whole group is keyboard-only
                }
            }

            sv.menu = menu;
            sv.returnTo = returnTo;
            sv.qualityValue = qText;
            sv.qualityTicks = ticks;
            sv.qualityDown = down;
            sv.qualityUp = up;
            sv.sunShafts = sun;
            sv.wheelSmoke = smoke;
            sv.balloons = balloons;
            sv.fps = fps;
            sv.standings = standings;
            sv.carLabels = labels;
            sv.steering = steering;
            sv.music = music;
            sv.musicValue = musicValue;
            sv.effects = effects;
            sv.effectsValue = effectsValue;
            sv.tilt = tilt;
            sv.tiltValue = tiltValue;
            EditorUtility.SetDirty(sv);
        }

        static Button QualityArrow(string name, Transform parent, string icon)
        {
            var rt = K.Node(name, parent);
            var bg = K.Img(rt, null, Color.clear, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            K.Size(rt, 30, 30);
            var i = K.Pic("Icon", rt, K.S(icon), Tele.Muted, 16, 16);
            var irt = (RectTransform)i.transform;
            irt.anchorMin = irt.anchorMax = Half;
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = new Vector2(16, 16);
            var cb = b.colors;
            cb.disabledColor = new Color(1, 1, 1, .3f);
            b.colors = cb;
            // The arrow, not the invisible square, dims when it can go no further.
            b.transition = Selectable.Transition.ColorTint;
            b.targetGraphic = i;
            return b;
        }
    }
}
