using System.Collections.Generic;
using System.Linq;
using ALIyerEdon;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MicroCar.UI.EditorTools
{
    public static partial class TeleBuilder
    {
        // --------------------------------------------------------------------------------------------
        // Race (Village)
        // --------------------------------------------------------------------------------------------

        static void BuildRace()
        {
            var race = Find<Race_Manager>();
            var results = Find<Start_Finish_UI>();
            var pause = Find<Pause_Menu>();
            var input = Find<InputSystem>();
            var nitro = Find<Nitro>();
            var counter = Find<Start_Counter>();
            var fps = Find<FPSCounter>();
            var menuSettings = Find<SettingsMenu>();
            var mud = Find<Screen_Mud>();

            var canvas = race.raceUI.GetComponentInParent<Canvas>(true).rootCanvas;
            var root = canvas.transform;
            var tc = PrepareCanvas(canvas);
            var safes = new List<RectTransform>();

            foreach (var child in root.Cast<Transform>().ToArray())
                if (child.name.StartsWith("T_"))
                    Object.DestroyImmediate(child.gameObject);

            MoveToLegacy(menuSettings.transform);
            menuSettings.transform.SetParent(pause.pauseMenu.transform, false);
            var hud = Holder(race.raceUI.transform);
            // The template's joystick is reused: out of the old (or last build's) layout before it goes.
            if (input.joystick)
                input.joystick.transform.SetParent(race.mobileControls.transform, false);
            var touch = Holder(race.mobileControls.transform, input.joystick ? input.joystick.name : "Joystick");
            var grid = Holder(race.startUI.transform);
            var pauseRoot = Holder(pause.pauseMenu.transform, menuSettings.name);
            var settings = Holder(menuSettings.transform, "Legacy");
            settings.SetAsLastSibling();
            settings.gameObject.SetActive(false);
            var countdown = counter ? Holder(counter.transform) : null;
            var fpsRoot = fps ? Holder(fps.transform) : null;
            if (mud) K.Stretch((RectTransform)mud.transform);

            BuildHud(hud, race, pause, input, nitro, safes, out var hv, out var posValue, out var lapValue);
            BuildTouch(touch, input, nitro, safes, out var n2oButton);
            BuildGrid(grid, race, results, pause, hud, safes);
            BuildPause(pauseRoot, pause, settings, new[] { hud, touch }, posValue, lapValue, safes);
            BuildSettings(settings, menuSettings, null, "PAUSE /", null, safes);
            if (countdown) BuildCountdown(countdown, counter);
            if (fpsRoot) BuildFps(fpsRoot, fps, safes);

            // Order, back to front.
            int i = 0;
            if (mud) mud.transform.SetSiblingIndex(i++);
            hud.SetSiblingIndex(i++);
            touch.SetSiblingIndex(i++);
            if (countdown) countdown.SetSiblingIndex(i++);
            grid.SetSiblingIndex(i++);
            pauseRoot.SetSiblingIndex(i++);
            if (fpsRoot) fpsRoot.SetSiblingIndex(i++);
            var es = root.Find("EventSystem");
            if (es) es.SetAsLastSibling();

            race.playerInfo = posValue;
            race.lapInfo = lapValue;
            race.racerInfo = hv.standingNames;
            nitro.mobileUI = n2oButton;
            foreach (var o in new Object[] { race, results, pause, input, nitro, counter, fps, hv })
                if (o) EditorUtility.SetDirty(o);

            Finish(tc, safes);
        }

        static Text Block(Transform parent, string name, string label, string value, Color color)
        {
            var b = K.Node(name, parent);
            K.Col(b, 3, TextAnchor.MiddleCenter, new RectOffset(18, 18, 8, 8));
            b.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
            K.Lbl("Label", b, label).alignment = TextAnchor.MiddleCenter;
            return K.Num("Value", b, value, 19, color, TextAnchor.MiddleCenter);
        }

        static void BuildHud(RectTransform hud, Race_Manager race, Pause_Menu pause, InputSystem input, Nitro nitro,
            List<RectTransform> safes, out HudView hv, out Text posValue, out Text lapValue)
        {
            hv = K.Get<HudView>(hud);
            Scrim("T_Scrim", hud, "scrim_hud_v");
            var safe = Safe(hud, safes);

            // Standings: Race_Manager switches the outer object (setting "Standings"); the panel
            // inside is desktop-only (no room on a phone).
            var tower = K.Stretch(K.Node("Tower", safe));
            var panel = K.Node("Panel", tower);
            K.Glass(panel);
            K.Col(panel, 0, TextAnchor.UpperLeft, new RectOffset(0, 0, 0, 6));
            K.FitH(panel);
            K.Lay(panel, K.Pin(new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(236, 220)), K.Hide, K.Hide);
            var th = K.Node("Head", panel);
            K.Row(th, 8, TextAnchor.MiddleLeft, new RectOffset(14, 14, 0, 0));
            K.Size(th, -1, 36);
            K.Lbl("Label", th, "STANDINGS");
            K.Spacer(th);
            K.Lbl("Count", th, "5 CARS");
            K.Line(panel, false);
            int n = Mathf.Max(5, race.racerPrefabs != null ? race.racerPrefabs.Length : 5);
            var names = new Text[n];
            var places = new Text[n];
            var rows = new Image[n];
            for (int i = 0; i < n; i++)
            {
                var row = K.Node("P" + (i + 1), panel);
                rows[i] = K.Img(row, K.S("solid"), Color.clear);
                K.Row(row, 10, TextAnchor.MiddleLeft, new RectOffset(14, 14, 0, 0));
                K.Size(row, -1, 31);
                places[i] = K.Num("Place", row, "P" + (i + 1), 13, Tele.Muted);
                K.Size(places[i], 30, -1);
                names[i] = K.Txt("Name", row, "RACER", K.Semi, 15, Tele.Ink);
                K.Size(names[i], -1, -1, 1);
            }

            // Position, lap, time.
            var top = K.Node("Top", safe);
            K.Glass(top);
            K.Row(top, 0, TextAnchor.MiddleCenter);
            K.FitW(top);
            K.Lay(top,
                K.Pin(new Vector2(.5f, 1), new Vector2(.5f, 1), Vector2.zero, new Vector2(300, 58)),
                K.Pin(new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(300, 56)),
                K.Pin(new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(300, 56)));
            posValue = Block(top, "Pos", "POS", "P1/5", Tele.Lime);
            K.Line(top, true, 32);
            lapValue = Block(top, "Lap", "LAP", "1/2", Tele.Ink);
            K.Line(top, true, 32);
            var time = Block(top, "Time", "TIME", "0:00.00", Tele.Ink);
            K.Size(time, 84, -1);

            var buttons = K.Node("Buttons", safe);
            K.Row(buttons, 8, TextAnchor.MiddleRight);
            K.FitW(buttons);
            K.Lay(buttons, K.Pin(Vector2.one, Vector2.one, Vector2.zero, new Vector2(100, 46)));
            var cam = K.IconBtn("Camera", buttons, K.S("icon_camera"));
            K.On(cam, input.Switch_Camera);
            var pauseBtn = K.IconBtn("Pause", buttons, K.S("icon_pause"));
            K.On(pauseBtn, input.Pause_Game);

            var legend = K.Node("Keys", safe);
            K.Glass(legend);
            K.Col(legend, 7, TextAnchor.UpperLeft, new RectOffset(14, 16, 12, 12));
            legend.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
            K.Fit(legend, true, true);
            K.Lay(legend, K.Pin(Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(220, 170)));
            K.KeysOnly(legend);
            foreach (var (caps, label) in new[]
            {
                (new[] { "W", "A", "S", "D" }, "DRIVE"), (new[] { "Space" }, "HANDBRAKE"),
                (new[] { "Shift" }, "NITRO"), (new[] { "C" }, "CAMERA"), (new[] { "Esc" }, "PAUSE"),
            })
            {
                var r = K.Node(label, legend);
                K.Row(r, 5, TextAnchor.MiddleLeft);
                foreach (var cap in caps)
                    Object.DestroyImmediate(K.Key(r, cap, Tele.Ink).GetComponent<TeleLayout>());
                var gap = K.Node("Gap", r);
                K.Size(gap, 6, -1);
                K.Lbl("Label", r, label, Tele.Muted);
            }

            // Speed, gear, revs, nitro.
            var speed = K.Node("Speed", safe);
            K.Col(speed, 8, TextAnchor.LowerRight, new RectOffset(18, 18, 14, 14));
            speed.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
            K.Fit(speed, true, true);
            K.Lay(speed,
                K.Pin(new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(260, 170)),
                K.Pin(new Vector2(.5f, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(240, 150)),
                K.Pin(new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 226), new Vector2(240, 150)));
            var back = K.Stretch(K.Node("Back", speed));
            K.Glass(back);
            K.Get<LayoutElement>(back).ignoreLayout = true;
            K.Lay(back, K.Full(), K.Hide, K.Hide);

            var readout = K.Node("Readout", speed);
            K.Row(readout, 14, TextAnchor.LowerRight);
            var kmhCol = K.Node("Kmh", readout);
            K.Col(kmhCol, 0, TextAnchor.LowerRight);
            var kmh = K.Num("Value", kmhCol, "000", 74, Tele.Ink, TextAnchor.LowerRight);
            FontSizes(kmh, 74, 58, 58);
            kmh.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, .5f);
            K.Lbl("Unit", kmhCol, "KM/H").alignment = TextAnchor.MiddleRight;
            K.Line(readout, true, 56);
            var gearCol = K.Node("Gear", readout);
            K.Col(gearCol, 2, TextAnchor.LowerCenter, new RectOffset(0, 0, 0, 4));
            K.Lbl("Label", gearCol, "GEAR").alignment = TextAnchor.MiddleCenter;
            var gear = K.Num("Value", gearCol, "1", 36, Tele.Lime, TextAnchor.MiddleCenter);

            var revRow = K.Node("Revs", speed);
            K.Row(revRow, 2, TextAnchor.MiddleRight);
            var revs = new Image[20];
            for (int i = 0; i < revs.Length; i++)
            {
                var t = K.Node("Tick", revRow);
                revs[i] = K.Img(t, null, Tele.Dim);
                K.Size(t, 7, 16);
            }

            var n2o = K.Node("Nitro", speed);
            K.Row(n2o, 10, TextAnchor.MiddleLeft);
            K.Lbl("Label", n2o, "N2O", Tele.Amber);
            var bar = K.Node("Bar", n2o);
            K.Img(bar, null, Tele.Dim);
            K.Size(bar, 150, 6);
            var fill = K.Stretch(K.Node("Fill", bar));
            var fillImg = K.Img(fill, K.S("white"), Tele.Amber);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = .65f;
            var shift = K.Key(n2o, "Shift", Tele.Amber);

            hv.race = race;
            hv.pause = pause;
            hv.time = time;
            hv.speed = kmh;
            hv.gear = gear;
            hv.revs = revs;
            hv.standingNames = names;
            hv.standingPlaces = places;
            hv.standingRows = rows;

            race.positionUI = tower.gameObject;
            nitro.nitroSliderMobile = fillImg;
            nitro.nitroSliderPC = fillImg;
            nitro.PcUI = shift.gameObject;
        }

        static RectTransform TouchButton(Transform parent, string name, string sprite, float size, TeleTouch.Kind kind,
            string icon = null, string text = null, Color? ink = null)
        {
            var rt = K.Node(name, parent);
            K.Img(rt, K.S(sprite), Color.white, true);
            K.Size(rt, size, size);
            rt.sizeDelta = new Vector2(size, size);
            K.Get<TeleTouch>(rt).kind = kind;
            var color = ink ?? Tele.Ink;
            if (icon != null)
            {
                var i = K.Pic("Icon", rt, K.S(icon), color, size * .4f, size * .4f);
                var irt = (RectTransform)i.transform;
                irt.anchorMin = irt.anchorMax = Half;
                irt.anchoredPosition = Vector2.zero;
                irt.sizeDelta = new Vector2(size * .4f, size * .4f);
            }
            if (text != null)
            {
                var t = K.Num("Text", rt, text, Mathf.RoundToInt(size * .19f), color, TextAnchor.MiddleCenter);
                K.Stretch((RectTransform)t.transform);
            }
            return rt;
        }

        static void BuildTouch(RectTransform touch, InputSystem input, Nitro nitro, List<RectTransform> safes, out GameObject n2oButton)
        {
            var safe = Safe(touch, safes);

            var steer = K.Node("Steer", safe);
            K.Row(steer, 14, TextAnchor.LowerLeft);
            K.Fit(steer, true, true);
            K.Lay(steer, K.Pin(Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(180, 83)));
            Scales(steer, 1f, 1f, .9f);
            TouchButton(steer, "Left", "touch", 83, TeleTouch.Kind.Left, "icon_chev_l");
            TouchButton(steer, "Right", "touch", 83, TeleTouch.Kind.Right, "icon_chev_r");

            // The template's joystick (control type "Stick"), restyled and pinned bottom-left.
            if (input.joystick)
            {
                var joy = (RectTransform)input.joystick.transform;
                joy.SetParent(safe, false);
                joy.localScale = Vector3.one;
                K.Lay(joy, K.Pin(Vector2.zero, Half, new Vector2(85, 85), new Vector2(170, 170)));
                var j = input.joystick.GetComponent<ALIyerEdon.Joystick>();
                if (j && j.background) { j.background.sprite = K.S("touch"); j.background.color = Color.white; j.background.type = Image.Type.Simple; }
                if (j && j.handle)
                {
                    j.handle.sprite = K.S("circle");
                    j.handle.color = new Color(Tele.Lime.r, Tele.Lime.g, Tele.Lime.b, .55f);
                    j.handle.rectTransform.sizeDelta = new Vector2(66, 66);
                }
            }

            var pedals = K.Node("Pedals", safe);
            K.Row(pedals, 11, TextAnchor.LowerRight);
            K.Fit(pedals, true, true);
            K.Lay(pedals, K.Pin(new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(300, 99)));
            Scales(pedals, 1f, 1f, .88f);
            var small = K.Node("Small", pedals);
            K.Col(small, 11, TextAnchor.LowerCenter);
            small.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
            n2oButton = TouchButton(small, "Nitro", "touch_amber", 59, TeleTouch.Kind.Nitro, "icon_bolt", null, Tele.Amber).gameObject;
            TouchButton(small, "Handbrake", "touch", 59, TeleTouch.Kind.Handbrake, null, "HB");
            TouchButton(pedals, "Brake", "touch", 83, TeleTouch.Kind.Brake, null, "BRK");
            TouchButton(pedals, "Gas", "touch_gas", 99, TeleTouch.Kind.Gas, null, "GAS", Tele.Lime);

            input.arrowKeys = steer.gameObject;
        }

        static void BuildGrid(RectTransform grid, Race_Manager race, Start_Finish_UI results, Pause_Menu pause,
            RectTransform hud, List<RectTransform> safes)
        {
            var rv = K.Get<ResultsView>(grid);
            var scrim = Scrim("T_Scrim", grid, "scrim_results_h");
            K.Lay(scrim, K.Full(), null, K.Hide);
            var dim = K.Fill("T_Dim", grid, new Color(Tele.Bg.r, Tele.Bg.g, Tele.Bg.b, .97f));
            K.Lay(dim, K.Hide, K.Hide, K.Full());
            var safe = Safe(grid, safes);

            var header = K.Node("Header", safe);
            K.Row(header, 12, TextAnchor.MiddleLeft);
            K.Lay(header, K.Spot(new Vector2(0, 1), Vector2.one, new Vector2(.5f, 1), new Vector2(0, -40), Vector2.zero));
            K.Pic("Dot", header, K.S("circle"), Tele.Lime, 11, 11);
            var title = K.Head("Title", header, "STARTING GRID", 22);
            var subtitle = K.Lbl("Subtitle", header, "VILLAGE · 2 LAPS");
            K.Lay(subtitle, default, null, K.Hide);

            // The big number and the facts under it.
            var res = K.Node("Result", safe);
            K.Col(res, 16, TextAnchor.MiddleLeft);
            K.Lay(res,
                K.Spot(Vector2.zero, new Vector2(.46f, 1), Half, new Vector2(0, 112), new Vector2(0, -56)),
                K.Spot(Vector2.zero, new Vector2(.44f, 1), Half, new Vector2(0, 64), new Vector2(0, -48)),
                K.Spot(new Vector2(0, 1), Vector2.one, new Vector2(.5f, 1), new Vector2(0, -330), new Vector2(0, -52)));
            var heroLabel = K.Lbl("HeroLabel", res, "GRID SLOT");
            var heroRow = K.Node("Hero", res);
            K.Row(heroRow, 10, TextAnchor.LowerLeft);
            var hero = K.Num("Place", heroRow, "P5", 150, Tele.Ink);
            FontSizes(hero, 150, 92, 104);
            var heroOf = K.Num("Of", heroRow, "/5", 32, Tele.Muted);
            var medal = K.Pic("Medal", heroRow, K.S("medal_gold"), Color.white, 52, 52);
            var facts = K.Node("Facts", res);
            K.Col(facts, 0);
            K.Line(facts, false);
            var factsRow = K.Node("Row", facts);
            K.Row(factsRow, 0);
            factsRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            var kvLabels = new Text[3];
            var kvValues = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                var cell = K.Node("Fact" + i, factsRow);
                K.Col(cell, 6, TextAnchor.UpperLeft, new RectOffset(0, 8, 12, 4));
                K.Size(cell, -1, -1, 1);
                kvLabels[i] = K.Lbl("Label", cell, "—");
                kvValues[i] = K.Num("Value", cell, "—", 24);
            }

            // The racers.
            var sheet = K.Node("Sheet", safe);
            K.Glass(sheet);
            K.Col(sheet, 0, TextAnchor.UpperLeft, new RectOffset(0, 0, 0, 6));
            K.FitH(sheet);
            K.Lay(sheet,
                K.Spot(new Vector2(.5f, 1), Vector2.one, new Vector2(.5f, 1), new Vector2(24, -56), new Vector2(0, -56)),
                K.Spot(new Vector2(.48f, 1), Vector2.one, new Vector2(.5f, 1), new Vector2(0, -48), new Vector2(0, -48)),
                K.Spot(new Vector2(0, 1), Vector2.one, new Vector2(.5f, 1), new Vector2(0, -346), new Vector2(0, -346)));
            var sh = K.Node("Head", sheet);
            K.Row(sh, 0, TextAnchor.MiddleLeft, new RectOffset(16, 16, 0, 0));
            K.Size(sh, -1, 36);
            K.Size(K.Lbl("Pos", sh, "POS"), 48, -1);
            K.Size(K.Lbl("Driver", sh, "DRIVER"), -1, -1, 1);
            var aw = K.Lbl("Award", sh, "AWARD");
            aw.alignment = TextAnchor.MiddleRight;
            K.Size(aw, 120, -1);
            K.Line(sheet, false);
            int n = Mathf.Max(5, results.driversName != null ? results.driversName.Length : 5);
            var names = new Text[n];
            var places = new Text[n];
            var awards = new Text[n];
            var rows = new Image[n];
            var medals = new Image[n];
            for (int i = 0; i < n; i++)
            {
                var row = K.Node("P" + (i + 1), sheet);
                rows[i] = K.Img(row, K.S("solid"), Color.clear);
                K.Row(row, 0, TextAnchor.MiddleLeft, new RectOffset(16, 16, 0, 0));
                K.Size(row, -1, 38);
                places[i] = K.Num("Place", row, "P" + (i + 1), 13, Tele.Muted);
                K.Size(places[i], 48, -1);
                names[i] = K.Txt("Name", row, "RACER", K.Semi, 16, Tele.Ink);
                K.Size(names[i], -1, -1, 1);
                var a = K.Node("Award", row);
                K.Row(a, 6, TextAnchor.MiddleRight);
                K.Size(a, 120, -1);
                medals[i] = K.Pic("Medal", a, K.S("medal_gold"), Color.white, 22, 22);
                awards[i] = K.Num("Text", a, "—", 13, Tele.Muted, TextAnchor.MiddleRight);
            }

            // After the finish: ×2 (rewarded ad), restart, garage.
            var finish = K.Node("Finish", safe);
            K.Col(finish, 8, TextAnchor.LowerRight);
            K.Lay(finish,
                K.Pin(new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(420, 104)),
                K.Pin(new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(400, 104)),
                K.Spot(Vector2.zero, new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 104)));
            var dbl = K.Btn("DoublePrize", finish, K.Look.Amber, "Double prize · Ad", null, null, K.S("icon_tv"), 48, 15);
            K.Num("Amount", dbl.transform, "+3 000", 15, Tele.Amber);
            dbl.gameObject.SetActive(false);
            var finishRow = K.Node("Row", finish);
            K.Row(finishRow, 8);
            finishRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            K.Size(finishRow, -1, 48);
            var restart = K.Btn("Restart", finishRow, K.Look.Glass, "Restart", null, "R", K.S("icon_restart"));
            K.On(restart, pause.Restart);
            var toGarage = K.Btn("Garage", finishRow, K.Look.Lime, "Garage", null, "Enter", K.S("icon_home"));
            K.On(toGarage, pause.Exit);
            finish.gameObject.SetActive(false);

            // Before the start: back to the garage, or go.
            var start = K.Node("Start", safe);
            K.Row(start, 8, TextAnchor.MiddleRight);
            start.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            K.Lay(start,
                K.Pin(new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(420, 56)),
                K.Pin(new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(400, 56)),
                K.Spot(Vector2.zero, new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 56)));
            var leave = K.Btn("Garage", start, K.Look.Glass, "Garage", null, null, K.S("icon_home"), 56);
            K.On(leave, pause.Exit);
            var go = K.Btn("StartRace", start, K.Look.Lime, "Start race", null, race.startKey.ToString(), K.S("icon_play"), 56, 18);
            K.On(go, race.StartRace_Button);

            // Start_Finish_UI switches a medal object per place; the medal here is the view's.
            var stubs = K.Node("T_Stubs", grid);
            stubs.gameObject.SetActive(false);
            GameObject Stub(string name) => K.Node(name, stubs).gameObject;
            var credits = K.Num("Credits", stubs, "", 12);

            results.finishRaceMenu = finish.gameObject;
            results.startButton = start.gameObject;
            results.raceUI = hud.gameObject;
            results.totalScores = credits;
            results.goldIcon = Stub("Gold");
            results.silverIcon = Stub("Silver");
            results.bronzeIcon = Stub("Bronze");
            results.noAward = Stub("None");
            results.positions = places;
            results.driversName = names;

            rv.results = results;
            rv.race = race;
            rv.pause = pause;
            rv.title = title;
            rv.subtitle = subtitle;
            rv.heroLabel = heroLabel;
            rv.hero = hero;
            rv.heroOf = heroOf;
            rv.heroMedal = medal;
            rv.gold = K.S("medal_gold");
            rv.silver = K.S("medal_silver");
            rv.bronze = K.S("medal_bronze");
            rv.kvLabels = kvLabels;
            rv.kvValues = kvValues;
            rv.names = names;
            rv.places = places;
            rv.awards = awards;
            rv.rows = rows;
            rv.awardMedals = medals;
            EditorUtility.SetDirty(rv);
        }

        static void BuildPause(RectTransform root, Pause_Menu pause, RectTransform settings, RectTransform[] hide, Text hudPos, Text hudLap, List<RectTransform> safes)
        {
            var pv = K.Get<PauseView>(root);
            var bg = K.Fill("T_Dim", root, new Color(Tele.Bg.r, Tele.Bg.g, Tele.Bg.b, .82f), true);
            bg.transform.SetAsFirstSibling();
            var safe = Safe(root, safes);

            var head = K.Node("Head", safe);
            K.Row(head, 12, TextAnchor.MiddleLeft);
            K.Lay(head, K.Spot(new Vector2(0, 1), Vector2.one, new Vector2(.5f, 1), new Vector2(0, -46), Vector2.zero));
            K.Pic("Dot", head, K.S("circle"), Tele.Amber, 11, 11);
            K.Head("Title", head, "PAUSED", 38);
            var sub = K.Lbl("Subtitle", head, "VILLAGE · RACE IN PROGRESS");
            K.Lay(sub, default, null, K.Hide);

            var menu = K.Node("Menu", safe);
            K.Col(menu, 7);
            K.Lay(menu,
                K.Pin(new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, -20), new Vector2(352, 226)),
                K.Pin(new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, -20), new Vector2(300, 226)),
                K.Spot(Vector2.zero, new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 226)));
            var resume = K.Btn("Resume", menu, K.Look.Lime, "Resume", "01", "Esc", null, 60, 18);
            K.On(resume, pause.Resume);
            var restart = K.Btn("Restart", menu, K.Look.Glass, "Restart", "02", "R");
            K.On(restart, pause.Restart);
            var set = K.Btn("Settings", menu, K.Look.Glass, "Settings", "03");
            K.On(set, pause.SetTrue, settings.gameObject);
            var garage = K.Btn("Garage", menu, K.Look.Glass, "Garage", "04", "G");
            K.On(garage, pause.Exit);

            var stats = K.Node("Stats", safe);
            K.Glass(stats);
            K.Col(stats, 0, TextAnchor.UpperLeft, new RectOffset(18, 18, 4, 4));
            K.Lay(stats,
                K.Pin(new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(0, -20), new Vector2(384, 170)),
                K.Pin(new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(0, -20), new Vector2(320, 160)),
                K.Spot(new Vector2(0, 1), Vector2.one, new Vector2(.5f, 1), new Vector2(0, -236), new Vector2(0, -66)));
            var s1 = K.Node("Row", stats);
            K.Row(s1, 0);
            s1.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            Cell(s1, "Position", "P1/5", out var pos, 26);
            pos.color = Tele.Lime;
            Cell(s1, "Lap", "1/2", out var lap, 26);
            K.Line(stats, false);
            var s2 = K.Node("Row", stats);
            K.Row(s2, 0);
            s2.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            Cell(s2, "Time", "0:00.00", out var time, 26);
            Cell(s2, "Nitro", "100%", out var nitroText, 26);
            nitroText.color = Tele.Amber;

            var loading = K.Lbl("Loading", safe, "", Tele.Ink, 14);
            loading.alignment = TextAnchor.MiddleRight;
            K.Lay(loading, K.Pin(Vector2.one, Vector2.one, Vector2.zero, new Vector2(240, 24)));

            pause.Loading = loading;
            pv.pause = pause;
            pv.position = pos;
            pv.lap = lap;
            pv.time = time;
            pv.nitro = nitroText;
            pv.hudPosition = hudPos;
            pv.hudLap = hudLap;
            pv.settings = settings.gameObject;
            pv.hide = System.Array.ConvertAll(hide, h => K.Get<CanvasGroup>(h));
            settings.SetAsLastSibling();
            EditorUtility.SetDirty(pv);
        }

        static void BuildCountdown(RectTransform root, Start_Counter counter)
        {
            GameObject Big(string text, Color color)
            {
                var node = K.Stretch(K.Node("Count_" + text, root));
                var t = K.Num("Text", node, text, 170, color, TextAnchor.MiddleCenter);
                var rt = K.Stretch((RectTransform)t.transform);
                rt.offsetMin = new Vector2(0, 80);
                rt.offsetMax = new Vector2(0, 80);
                t.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, .55f);
                node.gameObject.SetActive(false);
                return node.gameObject;
            }
            counter.counter_0 = Big("3", Tele.Ink);
            counter.counter_1 = Big("2", Tele.Ink);
            counter.counter_2 = Big("1", Tele.Ink);
            counter.counter_3 = Big("GO", Tele.Lime);
        }

        static void BuildFps(RectTransform root, FPSCounter fps, List<RectTransform> safes)
        {
            var safe = Safe(root, safes);
            var panel = K.Node("Panel", safe);
            K.Glass(panel);
            K.Lay(panel,
                K.Pin(new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -70), new Vector2(104, 28)),
                K.Pin(new Vector2(.5f, 1), new Vector2(.5f, 1), Vector2.zero, new Vector2(104, 28)),
                K.Pin(new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -66), new Vector2(104, 28)));
            var text = K.Num("Text", panel, "60 fps", 13, Tele.Muted, TextAnchor.MiddleCenter);
            K.Stretch((RectTransform)text.transform);
            panel.gameObject.SetActive(false);
            fps.panel = panel.gameObject;
            fps.text = text;
        }
    }
}
