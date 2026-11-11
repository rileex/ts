using ALIyerEdon;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MicroCar.UI
{
    /// <summary>The pause screen: the race's numbers next to the menu, and R / G on a keyboard.</summary>
    public sealed class PauseView : MonoBehaviour
    {
        public Pause_Menu pause;
        public Text position, lap, time, nitro;
        public Text hudPosition, hudLap;
        [Tooltip("Closed with the pause menu, so it is not left open over the race.")]
        public GameObject settings;
        [Tooltip("Hidden while paused: the race HUD (the pause screen has the same numbers) and the touch controls.")]
        public CanvasGroup[] hide;

        void OnEnable() => Show(false);

        void Show(bool on)
        {
            if (hide == null) return;
            foreach (var g in hide)
                if (g) g.alpha = on ? 1f : 0f;
        }

        void OnDisable()
        {
            if (settings) settings.SetActive(false);
            Show(true);
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
}
