using ALIyerEdon;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MicroCar.UI
{
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
}
