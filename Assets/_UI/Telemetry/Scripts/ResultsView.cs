using ALIyerEdon;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MicroCar.UI
{
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
}
