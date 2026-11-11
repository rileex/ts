using ALIyerEdon;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MicroCar.UI
{
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
}
