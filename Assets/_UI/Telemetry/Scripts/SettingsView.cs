using ALIyerEdon;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MicroCar.UI
{
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
        public Slider music, effects, tilt;
        public Text musicValue, effectsValue, tiltValue;

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
            if (effects)
            {
                effects.minValue = 0f;
                effects.maxValue = 1f;
                effects.onValueChanged.AddListener(v =>
                {
                    SfxVolume.Set(v);
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
            if (effects) effects.SetValueWithoutNotify(SfxVolume.Value);
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
            if (effectsValue && effects)
                effectsValue.text = Mathf.RoundToInt(effects.value * 100f).ToString();
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
}
