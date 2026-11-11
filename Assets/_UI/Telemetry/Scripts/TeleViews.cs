using ALIyerEdon;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MicroCar.UI
{
    /// <summary>
    /// The Telemetry screens. The game logic stays in the template's scripts (CarSelect, LevelSelect,
    /// SettingsMenu, Race_Manager, Start_Finish_UI, Pause_Menu, InputSystem, Nitro); TeleBuilder points
    /// their UI fields at the new elements, and these views add what the template never showed (car
    /// names, the race clock, highlighting the player) and the keyboard shortcuts.
    /// </summary>
    static class Keys
    {
        public static bool Down(KeyCode key) => TeleScreen.ShowKeys && Input.GetKeyDown(key);
    }

    /// <summary>One choice out of a row of buttons (OFF / ON, BUTTONS / STICK / TILT).</summary>
    public sealed class TeleSegment : MonoBehaviour
    {
        public Button[] options;
        public Image[] backs;
        public Text[] labels;
        public System.Action<int> picked;

        void Awake()
        {
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                options[i].onClick.AddListener(() => picked?.Invoke(index));
            }
        }

        public void Show(int selected)
        {
            for (int i = 0; i < options.Length; i++)
            {
                bool on = i == selected;
                if (backs != null && i < backs.Length && backs[i]) backs[i].color = on ? Tele.Lime : Color.clear;
                if (labels != null && i < labels.Length && labels[i]) labels[i].color = on ? Tele.OnLime : Tele.Muted;
            }
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Garage scene
    // ------------------------------------------------------------------------------------------------

    /// <summary>The garage: the car on the shelf, its name and status, and the main menu.</summary>
    public sealed class GarageView : MonoBehaviour
    {
        public CarSelect carSelect;
        public LevelSelect levelSelect;
        public GameObject tracksScreen, settingsScreen;
        [Tooltip("While any of these is open the garage ignores its keyboard shortcuts.")]
        public GameObject[] blockers;

        public Text carName, carType, carIndex, status, trackName, trackBest, trackInfo;
        public Image statusIcon;
        public Sprite ownedIcon, lockedIcon;
        public Image[] pager;
        public CanvasGroup startGroup;

        [Header("Shown names (the models' names; the prefabs have none)")]
        public string[] names = { "Firewall", "Firewheels", "Firejump", "Firejump", "Firewall" };
        public string[] types = { "Buggy", "Truck", "Buggy", "Offroader", "Truck" };

        [Header("Track facts (the race scene knows them, the garage does not)")]
        public int[] trackLaps = { 2 };
        public int[] trackRacers = { 5 };

        void Update()
        {
            if (carSelect == null || carSelect.cars == null || carSelect.cars.Length == 0)
                return;

            int id = Mathf.Clamp(carSelect.ID, 0, carSelect.cars.Length - 1);
            bool owned = PlayerPrefs.GetInt("Car" + id) == 3;

            if (carName) carName.text = (id < names.Length ? names[id] : "Car " + (id + 1)).ToUpperInvariant();
            if (carType) carType.text = (id < types.Length ? types[id] : "").ToUpperInvariant();
            if (carIndex) carIndex.text = Tele.Two(id + 1) + "/" + Tele.Two(carSelect.cars.Length);
            for (int i = 0; pager != null && i < pager.Length; i++)
                if (pager[i]) pager[i].color = i == id ? Tele.Lime : Tele.Dim;

            if (status)
            {
                int price = id < carSelect.carPrices.Length ? carSelect.carPrices[id] : 0;
                status.text = owned ? "OWNED" : "LOCKED · " + Tele.Credits(price) + " CR";
                status.color = owned ? Tele.Lime : Tele.Amber;
            }
            if (statusIcon)
            {
                statusIcon.sprite = owned ? ownedIcon : lockedIcon;
                statusIcon.color = owned ? Tele.Lime : Tele.Amber;
            }
            if (startGroup)
            {
                startGroup.alpha = owned ? 1f : 0.35f;
                startGroup.interactable = owned;
            }

            UpdateTrack();

            if (Blocked()) return;
            if (Keys.Down(KeyCode.LeftArrow) || Keys.Down(KeyCode.A)) carSelect.PrevCar();
            if (Keys.Down(KeyCode.RightArrow) || Keys.Down(KeyCode.D)) carSelect.NextCar();
            if (Keys.Down(KeyCode.Return) || Keys.Down(KeyCode.KeypadEnter))
            {
                if (owned) carSelect.SelectCar();
                else carSelect.Buy_CurrentCar();
            }
            if (Keys.Down(KeyCode.T)) OpenTracks();
            if (Keys.Down(KeyCode.S)) OpenSettings();
        }

        void UpdateTrack()
        {
            if (levelSelect == null) return;
            int level = PlayerPrefs.GetInt("LevelID");
            if (level < 0 || level >= levelSelect.levelNames.Length) level = 0;
            if (trackName) trackName.text = levelSelect.levelNames[level].ToUpperInvariant();
            int best = PlayerPrefs.GetInt("Award_Level_" + level, 3);
            if (trackBest) trackBest.text = best < 3 ? "BEST P" + (best + 1) : "NO FINISH YET";
            if (trackInfo)
            {
                int laps = level < trackLaps.Length ? trackLaps[level] : 0;
                int racers = level < trackRacers.Length ? trackRacers[level] : 0;
                trackInfo.text = laps + " LAPS · " + racers + " RACERS · WIN 3 000 CR";
            }
        }

        bool Blocked()
        {
            if (blockers == null) return false;
            foreach (var b in blockers)
                if (b && b.activeInHierarchy) return true;
            return false;
        }

        public void OpenTracks()
        {
            tracksScreen.SetActive(true);
            gameObject.SetActive(false);
        }

        public void OpenSettings()
        {
            settingsScreen.SetActive(true);
            gameObject.SetActive(false);
        }
    }

    /// <summary>Track select. LevelSelect keeps the locks, prices and medals; this shows the details.</summary>
    public sealed class TracksView : MonoBehaviour
    {
        public LevelSelect levelSelect;
        public CarSelect carSelect;
        public GameObject garageScreen;
        public GameObject[] blockers;
        public Text bestText, statusText;
        public int selected;

        void OnEnable()
        {
            if (levelSelect == null) return;
            int best = PlayerPrefs.GetInt("Award_Level_" + selected, 3);
            if (bestText)
            {
                bestText.text = best < 3 ? "P" + (best + 1) : "—";
                bestText.color = best < 3 ? Tele.Lime : Tele.Muted;
            }
            if (statusText)
                statusText.text = PlayerPrefs.GetInt("Level" + selected) == 3 ? "OPEN" : "LOCKED";
        }

        void Update()
        {
            if (blockers != null)
                foreach (var b in blockers)
                    if (b && b.activeInHierarchy) return;
            if (Keys.Down(KeyCode.Backspace) || Keys.Down(KeyCode.Escape)) Back();
            if (Keys.Down(KeyCode.Return) || Keys.Down(KeyCode.KeypadEnter)) StartRace();
        }

        public void Back()
        {
            garageScreen.SetActive(true);
            gameObject.SetActive(false);
        }

        /// <summary>Picks the track (back to the garage, as LevelSelect does) and starts with the car on the shelf.</summary>
        public void StartRace()
        {
            levelSelect.SelectLevel(selected);
            if (PlayerPrefs.GetInt("Level" + selected) == 3)
                carSelect.SelectCar();
        }
    }

    /// <summary>
    /// Settings, over the template's SettingsMenu: its dropdowns, sliders and toggles stay (hidden) as
    /// the state it reads, and this sets them and calls the same methods their events called.
    /// </summary>
    public sealed class SettingsView : MonoBehaviour
    {
        static readonly string[] Quality = { "VERY LOW", "LOW", "MEDIUM", "HIGH", "ULTRA", "MAX" };

        public SettingsMenu menu;
        [Tooltip("Shown again on close (the garage); empty in the race, where the pause menu stays underneath.")]
        public GameObject returnTo;

        public Text qualityValue;
        public Image[] qualityTicks;
        public Button qualityDown, qualityUp;
        public TeleSegment sunShafts, wheelSmoke, balloons, fps, standings, carLabels, steering;
        public Slider music, tilt;
        public Text musicValue, tiltValue;

        bool wired;

        void Wire()
        {
            if (wired || menu == null) return;
            wired = true;
            qualityDown.onClick.AddListener(() => SetQuality(menu.qualityLevel.value - 1));
            qualityUp.onClick.AddListener(() => SetQuality(menu.qualityLevel.value + 1));
            Bind(sunShafts, v => { menu.sunShaft.SetValueWithoutNotify(v); menu.Set_SunShaft(); });
            Bind(wheelSmoke, v => { menu.wheelSmoke.SetValueWithoutNotify(v); menu.Set_WheelSmoke(); });
            Bind(balloons, v =>
            {
                menu.Balloons.SetValueWithoutNotify(v);
                if (menu.dynamicCamera) menu.dynamicCamera.SetValueWithoutNotify(v);
                menu.Set_Balloons();
            });
            Bind(fps, v => { menu.displayFPS.SetValueWithoutNotify(v); menu.Set_DisplayFPS(); });
            Bind(standings, v => { menu.positionUI.SetIsOnWithoutNotify(v == 1); menu.Toggle_PositionUI(); });
            Bind(carLabels, v => { menu.localPosition.SetIsOnWithoutNotify(v == 1); menu.Toggle_LocalPosition(); });
            Bind(steering, v => { menu.controlType.SetValueWithoutNotify(v); menu.Set_ControlType(); });

            if (music && menu.musicVolume)
            {
                music.minValue = menu.musicVolume.minValue;
                music.maxValue = menu.musicVolume.maxValue;
                music.onValueChanged.AddListener(v =>
                {
                    menu.musicVolume.SetValueWithoutNotify(v);
                    menu.Music_Volume();
                    ShowSliders();
                });
            }
            if (tilt && menu.AccelSensibility)
            {
                tilt.minValue = menu.AccelSensibility.minValue;
                tilt.maxValue = menu.AccelSensibility.maxValue;
                tilt.wholeNumbers = menu.AccelSensibility.wholeNumbers;
                tilt.onValueChanged.AddListener(v =>
                {
                    menu.AccelSensibility.SetValueWithoutNotify(v);
                    menu.Accelometer_Sensibility();
                    ShowSliders();
                });
            }
        }

        void Bind(TeleSegment seg, System.Action<int> set)
        {
            if (seg == null) return;
            seg.picked = v => { set(v); Sync(); };
        }

        void OnEnable()
        {
            Wire();
            Sync();
            // SettingsMenu.Start fills its controls from PlayerPrefs on the first open, after this.
            StartCoroutine(SyncNextFrame());
        }

        System.Collections.IEnumerator SyncNextFrame()
        {
            yield return null; // a frame, not time: the race is paused (timeScale 0) under this screen
            Sync();
        }

        void SetQuality(int value)
        {
            value = Mathf.Clamp(value, 0, Quality.Length - 1);
            menu.qualityLevel.SetValueWithoutNotify(value);
            menu.Set_QualityLevel();
            menu.Close_Save();
            Sync();
        }

        void Sync()
        {
            if (menu == null) return;
            int q = Mathf.Clamp(menu.qualityLevel.value, 0, Quality.Length - 1);
            if (qualityValue) qualityValue.text = Quality[q];
            for (int i = 0; qualityTicks != null && i < qualityTicks.Length; i++)
                if (qualityTicks[i]) qualityTicks[i].color = i <= q ? Tele.Lime : Tele.Dim;
            if (qualityDown) qualityDown.interactable = q > 0;
            if (qualityUp) qualityUp.interactable = q < Quality.Length - 1;

            sunShafts?.Show(menu.sunShaft.value);
            wheelSmoke?.Show(menu.wheelSmoke.value);
            balloons?.Show(menu.Balloons.value);
            fps?.Show(menu.displayFPS.value);
            standings?.Show(menu.positionUI.isOn ? 1 : 0);
            carLabels?.Show(menu.localPosition.isOn ? 1 : 0);
            steering?.Show(menu.controlType.value);

            if (music && menu.musicVolume) music.SetValueWithoutNotify(menu.musicVolume.value);
            if (tilt && menu.AccelSensibility)
            {
                tilt.SetValueWithoutNotify(menu.AccelSensibility.value);
                tilt.interactable = menu.controlType.value == 2;
            }
            ShowSliders();
        }

        void ShowSliders()
        {
            if (musicValue && music)
                musicValue.text = Mathf.RoundToInt(Mathf.InverseLerp(music.minValue, music.maxValue, music.value) * 100f).ToString();
            if (tiltValue && tilt)
                tiltValue.text = Mathf.RoundToInt(tilt.value).ToString();
        }

        void Update()
        {
            if (Keys.Down(KeyCode.Backspace) || (returnTo != null && Keys.Down(KeyCode.Escape)))
                Close();
        }

        public void Close()
        {
            if (menu) menu.Close_Save();
            gameObject.SetActive(false);
            if (returnTo) returnTo.SetActive(true);
        }
    }

    // ------------------------------------------------------------------------------------------------
    // Race scene
    // ------------------------------------------------------------------------------------------------

    /// <summary>Race time, from the green light to the finish (game time: it stops in the pause).</summary>
    public static class RaceClock
    {
        public static float Seconds;
    }

    /// <summary>The race HUD: speed, gear, revs and the clock; the standings with the player marked.</summary>
    public sealed class HudView : MonoBehaviour
    {
        public Race_Manager race;
        public Pause_Menu pause;
        public Text time, speed, gear;
        public Image[] revs;
        public Text[] standingNames;
        public Text[] standingPlaces;
        public Image[] standingRows;
        [Tooltip("km/h per unit of EasyCarController.currentSpeed (RPM.cs used 1.7).")]
        public float speedMultiplier = 1.7f;

        EasyCarController car;
        int lastSpeed = -1, lastGear = -99, lastRevs = -1;

        void Awake() => RaceClock.Seconds = 0f;

        void LateUpdate()
        {
            if (car == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player) car = player.GetComponent<EasyCarController>();
            }

            if (pause != null && pause.raceIsStarted)
                RaceClock.Seconds += Time.deltaTime;
            if (time) time.text = Tele.Clock(RaceClock.Seconds);

            if (car != null)
            {
                int kmh = Mathf.RoundToInt(Mathf.Abs(car.currentSpeed) * speedMultiplier);
                if (kmh != lastSpeed && speed) speed.text = kmh.ToString("000");
                lastSpeed = kmh;

                if (car.currentGear != lastGear && gear) gear.text = car.currentGear < 0 ? "R" : (car.currentGear + 1).ToString();
                lastGear = car.currentGear;

                if (revs != null && revs.Length > 0)
                {
                    int lit = Mathf.RoundToInt(Mathf.Clamp01((car.Revs - 0.1f) / 0.6f) * revs.Length);
                    if (lit != lastRevs)
                    {
                        for (int i = 0; i < revs.Length; i++)
                        {
                            float f = (float)i / revs.Length;
                            var on = f < 0.6f ? Tele.Ink : f < 0.8f ? Tele.Lime : Tele.Amber;
                            revs[i].color = i < lit ? on : Tele.Dim;
                        }
                        lastRevs = lit;
                    }
                }
            }

            MarkPlayer(race, standingNames, standingRows, standingPlaces);
        }

        /// <summary>Race_Manager writes the names; this shows the player's as YOU on a lime row.</summary>
        public static int MarkPlayer(Race_Manager race, Text[] names, Image[] rows, Text[] places)
        {
            int me = -1;
            if (race == null || names == null) return me;
            string player = race.PlayerName;
            for (int i = 0; i < names.Length; i++)
            {
                if (!names[i]) continue;
                bool isMe = names[i].text == player || names[i].text == "YOU";
                if (isMe)
                {
                    names[i].text = "YOU";
                    me = i;
                }
                var ink = isMe ? Tele.OnLime : Tele.Ink;
                names[i].color = ink;
                if (rows != null && i < rows.Length && rows[i]) rows[i].color = isMe ? Tele.Lime : Color.clear;
                if (places != null && i < places.Length && places[i]) places[i].color = isMe ? Tele.OnLime : Tele.Muted;
            }
            return me;
        }
    }

    /// <summary>
    /// The screen before and after the race (the template's Start_UI): the starting grid with START,
    /// then the result with the prize, RESTART, GARAGE and the rewarded ×2.
    /// </summary>
    public sealed class ResultsView : MonoBehaviour
    {
        public Start_Finish_UI results;
        public Race_Manager race;
        public Pause_Menu pause;
        public string trackName = "Village";

        public Text title, subtitle, heroLabel, hero, heroOf;
        public Image heroMedal;
        public Sprite gold, silver, bronze;
        public Text[] kvLabels, kvValues;
        public Text[] names, places, awards;
        public Image[] rows, awardMedals;

        void LateUpdate()
        {
            if (results == null || race == null) return;
            bool finished = results.finishRaceMenu && results.finishRaceMenu.activeSelf;
            int count = race.RacerCount > 0 ? race.RacerCount : names.Length;

            int me = HudView.MarkPlayer(race, names, rows, places);

            if (title) title.text = finished ? "RACE COMPLETE" : "STARTING GRID";
            if (subtitle) subtitle.text = trackName.ToUpperInvariant() + " · " + race.totalLaps + " LAPS";
            if (heroLabel) heroLabel.text = finished ? "FINISH" : "GRID SLOT";
            if (hero) hero.text = me >= 0 ? "P" + (me + 1) : "P–";
            if (heroOf) heroOf.text = "/" + count;

            int prize = finished ? Prize(me) : 0;
            if (heroMedal)
            {
                var sprite = !finished ? null : me == 0 ? gold : me == 1 ? silver : me == 2 ? bronze : null;
                heroMedal.sprite = sprite;
                heroMedal.enabled = sprite != null;
            }
            if (hero) hero.color = finished && me == 0 ? Tele.Lime : Tele.Ink;

            int credits = PlayerPrefs.GetInt("TotalScores");
            if (finished)
                SetKv(("TIME", Tele.Clock(RaceClock.Seconds), Tele.Ink),
                      ("PRIZE", prize > 0 ? "+" + Tele.Credits(prize) : "—", Tele.Amber),
                      ("CREDITS", Tele.Credits(credits), Tele.Ink));
            else
                SetKv(("LAPS", race.totalLaps.ToString(), Tele.Ink),
                      ("RACERS", count.ToString(), Tele.Ink),
                      ("CREDITS", Tele.Credits(credits), Tele.Ink));

            for (int i = 0; awards != null && i < awards.Length; i++)
            {
                bool medal = finished && i < 3;
                if (awardMedals != null && i < awardMedals.Length && awardMedals[i])
                {
                    awardMedals[i].enabled = medal;
                    awardMedals[i].sprite = i == 0 ? gold : i == 1 ? silver : bronze;
                }
                if (awards[i])
                {
                    awards[i].text = finished && i == me && prize > 0 ? "+" + Tele.Credits(prize) : medal ? "" : "—";
                    awards[i].color = i == me ? Tele.OnLime : Tele.Muted;
                }
            }

            if (finished && TeleScreen.ShowKeys)
            {
                if (Input.GetKeyDown(KeyCode.R) && pause) pause.Restart();
                if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && pause) pause.Exit();
            }
        }

        int Prize(int place) =>
            place == 0 ? results.goldAward : place == 1 ? results.secondPlaceAward : place == 2 ? results.thirdPlaceAward : 0;

        void SetKv(params (string label, string value, Color color)[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (kvLabels != null && i < kvLabels.Length && kvLabels[i]) kvLabels[i].text = items[i].label;
                if (kvValues != null && i < kvValues.Length && kvValues[i])
                {
                    kvValues[i].text = items[i].value;
                    kvValues[i].color = items[i].color;
                }
            }
        }
    }

    /// <summary>The pause screen: the race's numbers next to the menu, and R / G on a keyboard.</summary>
    public sealed class PauseView : MonoBehaviour
    {
        public Pause_Menu pause;
        public Text position, lap, time, nitro;
        public Text hudPosition, hudLap;
        [Tooltip("Closed with the pause menu, so it is not left open over the race.")]
        public GameObject settings;

        void OnDisable()
        {
            if (settings) settings.SetActive(false);
        }

        void Update()
        {
            if (position && hudPosition) position.text = hudPosition.text;
            if (lap && hudLap) lap.text = hudLap.text;
            if (time) time.text = Tele.Clock(RaceClock.Seconds);
            if (nitro)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                var feature = player ? player.GetComponent<Nitro_Feature>() : null;
                nitro.text = feature ? Mathf.RoundToInt(feature.Amount) + "%" : "—";
            }
            if (pause == null) return;
            if (Keys.Down(KeyCode.R)) pause.Restart();
            if (Keys.Down(KeyCode.G)) pause.Exit();
        }
    }

    /// <summary>
    /// A held touch control. Steering is fed every frame while held (the template's arrows nudged it
    /// once per press and never reached full lock).
    /// </summary>
    public sealed class TeleTouch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public enum Kind { Gas, Brake, Left, Right, Handbrake, Nitro }

        public Kind kind;
        InputSystem input;
        Nitro nitro;
        bool held;

        void Awake()
        {
            input = FindFirstObjectByType<InputSystem>(FindObjectsInactive.Include);
            nitro = FindFirstObjectByType<Nitro>(FindObjectsInactive.Include);
        }

        public void OnPointerDown(PointerEventData e)
        {
            held = true;
            transform.localScale = Vector3.one * 0.92f;
            if (input == null) return;
            switch (kind)
            {
                case Kind.Gas: input.Throttle(); break;
                case Kind.Brake: input.Brake(true); break;
                case Kind.Left: input.Steer(false); break;
                case Kind.Right: input.Steer(true); break;
                case Kind.Handbrake: input.Hand_Brake(true); break;
                case Kind.Nitro: if (nitro) nitro.Apply_Nitro(); break;
            }
        }

        public void OnPointerUp(PointerEventData e) => Release();

        void Release()
        {
            if (!held) return;
            held = false;
            transform.localScale = Vector3.one;
            if (input == null) return;
            switch (kind)
            {
                case Kind.Gas: input.ThrottleRelease(); break;
                case Kind.Brake: input.Brake(false); break;
                case Kind.Left:
                case Kind.Right: input.SteerRelease(); break;
                case Kind.Handbrake: input.Hand_Brake(false); break;
                case Kind.Nitro: if (nitro) nitro.Release_Nitro(); break;
            }
        }

        void Update()
        {
            if (held && input != null && (kind == Kind.Left || kind == Kind.Right))
                input.Steer(kind == Kind.Right);
        }

        void OnDisable() => Release();
    }
}
