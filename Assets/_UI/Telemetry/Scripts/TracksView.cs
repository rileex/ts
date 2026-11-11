using ALIyerEdon;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MicroCar.UI
{
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
}
