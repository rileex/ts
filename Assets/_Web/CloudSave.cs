using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace MicroCar.Web
{
    /// <summary>
    /// Mirrors the game's progress (PlayerPrefs) to the player's YouTube account.
    ///
    /// <para>
    /// The template scripts (namespace ALIyerEdon) read and write PlayerPrefs all over the place,
    /// and PlayerPrefs cannot list its keys on WebGL. So instead of rewriting every call site, this
    /// takes a snapshot of a known list of keys (<see cref="Keys"/>) and pushes it when it changed:
    /// on every scene load, every few seconds, and when YouTube pauses the game.
    /// </para>
    ///
    /// <para>
    /// Which copy wins: every push carries a revision that only goes up. The cloud copy is applied
    /// on boot only when its revision is higher than the one this browser last wrote — a new device
    /// inherits the account's progress, and a stale cloud copy never overwrites newer local play.
    /// </para>
    /// </summary>
    public static class CloudSave
    {
        const string KeyRevision = "cloud.rev";
        const int MaxCars = 30;
        const int MaxLevels = 20;

        static readonly string[] IntKeys =
        {
            "TotalScores", "CarID", "LevelID", "FirstRun", "ControlType", "QualityLevel", "Difficulty Level",
            "ShowPositionUI", "ShowLocalPosition", "Display FPS", "Sun Shaft", "Dynamic Camera", "Wheel Smoke",
            "Balloons", "Target FPS", "TruckID",
        };

        static readonly string[] FloatKeys = { "Music", "accelSensibility", "SteeringWheelSens" };

        static List<(string key, bool isFloat)> keys;
        static bool loaded;
        static bool requested;
        static string lastPushed;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void MicroCloudLoad();
        [DllImport("__Internal")] static extern void MicroCloudSave(string payload);
#endif

        /// <summary>Every key that is progress or a setting worth carrying between devices.</summary>
        static List<(string key, bool isFloat)> Keys
        {
            get
            {
                if (keys != null)
                    return keys;

                keys = new List<(string, bool)>();
                foreach (var k in IntKeys) keys.Add((k, false));
                foreach (var k in FloatKeys) keys.Add((k, true));

                for (int i = 0; i < MaxCars; i++)
                {
                    keys.Add(("Car" + i, false));                 // 3 = unlocked
                    keys.Add(("CarColor" + i, false));
                    keys.Add(("TruckColor" + i, false));
                    keys.Add(("Car" + i + "Neon", false));
                    keys.Add(("Car" + i + "Ring", false));
                    keys.Add(("Car" + i + "Roof", false));
                    keys.Add(("Car" + i + "Spoiler", false));
                    keys.Add(("Car" + i + "Suspension", true));
                }

                for (int i = 0; i < MaxLevels; i++)
                {
                    keys.Add(("Level" + i, false));                // 3 = unlocked
                    keys.Add(("Award_Level_" + i, false));
                }

                return keys;
            }
        }

        /// <summary>
        /// True once the cloud copy has arrived (or is known not to be coming). The splash scene waits
        /// on this before loading the garage, which reads money and unlocks in its first frame.
        /// </summary>
        public static bool Ready
        {
            get
            {
                Request();
                return loaded;
            }
        }

        /// <summary>Asks for the cloud copy once. Off YouTube there is nothing to ask.</summary>
        public static void Request()
        {
            if (requested)
                return;

            requested = true;

            if (!Playables.Available)
            {
                loaded = true;
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            MicroCloudLoad();
#else
            loaded = true;
#endif
        }

        /// <summary>Pushes the snapshot if it changed since the last push (or always, when forced).</summary>
        public static void Push(bool force = false)
        {
            if (!loaded)
                return; // Never push before the cloud copy has been read: it could be newer.

            string body = Snapshot();
            if (!force && body == lastPushed)
                return;

            if (body != lastPushed)
            {
                int rev = PlayerPrefs.GetInt(KeyRevision, 0) + 1;
                PlayerPrefs.SetInt(KeyRevision, rev);
                lastPushed = body;
            }

            PlayerPrefs.Save();

            if (!Playables.Available)
                return;

            string payload = "rev=" + PlayerPrefs.GetInt(KeyRevision, 0) + "\n" + body;
#if UNITY_WEBGL && !UNITY_EDITOR
            MicroCloudSave(payload);
#endif
        }

        static string Snapshot()
        {
            var sb = new StringBuilder(2048);
            foreach (var (key, isFloat) in Keys)
            {
                if (!PlayerPrefs.HasKey(key))
                    continue;

                if (isFloat)
                    sb.Append("f|").Append(key).Append('|')
                      .Append(PlayerPrefs.GetFloat(key).ToString("R", CultureInfo.InvariantCulture)).Append('\n');
                else
                    sb.Append("i|").Append(key).Append('|').Append(PlayerPrefs.GetInt(key)).Append('\n');
            }

            return sb.ToString();
        }

        static void Apply(string text)
        {
            loaded = true;

            if (string.IsNullOrEmpty(text) || !text.StartsWith("rev="))
            {
                lastPushed = Snapshot();
                return;
            }

            int nl = text.IndexOf('\n');
            string revText = nl < 0 ? text.Substring(4) : text.Substring(4, nl - 4);
            if (!int.TryParse(revText, out int remoteRev) || remoteRev <= PlayerPrefs.GetInt(KeyRevision, 0))
            {
                lastPushed = Snapshot();
                return;
            }

            foreach (var line in text.Substring(nl + 1).Split('\n'))
            {
                var parts = line.Split('|');
                if (parts.Length != 3)
                    continue;

                if (parts[0] == "i" && int.TryParse(parts[2], out int iv))
                    PlayerPrefs.SetInt(parts[1], iv);
                else if (parts[0] == "f" && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float fv))
                    PlayerPrefs.SetFloat(parts[1], fv);
            }

            PlayerPrefs.SetInt(KeyRevision, remoteRev);
            PlayerPrefs.Save();
            lastPushed = Snapshot();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            loaded = false;
            requested = false;
            lastPushed = null;
        }

        /// <summary>Receives the cloud copy from CloudSave.jslib (lives on the 'Playables' object).</summary>
        public sealed class Receiver : MonoBehaviour
        {
            public void OnCloudLoaded(string text) => Apply(text);
        }
    }
}
