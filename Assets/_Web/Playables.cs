using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MicroCar.Web
{
    /// <summary>
    /// The whole of the game's conversation with YouTube Playables (ytgame), talked to directly.
    ///
    /// <para>
    /// Ported from Car Simulator Open City Stunts, which passed McPlay pre-flight and YouTube's
    /// manual certification this way. Playgama Bridge is deliberately not used: it registers its
    /// own <c>visibilitychange</c> listeners, and certification fails any game whose bundle uses
    /// the Page Visibility API for pause and resume. Pause comes from <c>ytgame.system</c> only.
    /// </para>
    ///
    /// <para>
    /// Off YouTube (the editor, a local test of the build) <see cref="Available"/> is false and
    /// every call answers at once, so the game stays playable.
    /// </para>
    /// </summary>
    public static class Playables
    {
        // The GameObject the page sends to. Must match Playables.jslib and CloudSave.jslib.
        public const string ReceiverName = "Playables";

        static bool said;
        static int available = -1;

        static Action interstitialFinished;
        static Action<bool> rewardedFinished;

        // Each request carries a ticket the page hands back, so the answer to an ad the game has
        // already stopped waiting for can never be mistaken for the answer to the next one.
        static int interstitialTicket;
        static int rewardedTicket;

        static Playables()
        {
            HostAudioEnabled = true;
        }

        /// <summary>The platform asking the game to hold still (true), or to carry on (false).</summary>
        public static event Action<bool> HostPauseChanged;

        /// <summary>The platform's mute switch. True means the game may make a sound.</summary>
        public static event Action<bool> HostAudioChanged;

        public static bool HostPaused { get; private set; }
        public static bool HostAudioEnabled { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int MicroPlayableAvailable();
        [DllImport("__Internal")] static extern void MicroPlayableReady();
        [DllImport("__Internal")] static extern void MicroPlayableWatch();
        [DllImport("__Internal")] static extern void MicroPlayableInterstitial(int ticket);
        [DllImport("__Internal")] static extern void MicroPlayableRewarded(string rewardId, int ticket);
#endif

        /// <summary>Whether the game is running inside a Playable. The SDK is either on the page
        /// from the first line of game code or it is never coming, so this is asked once.</summary>
        public static bool Available
        {
            get
            {
                if (available < 0)
                {
#if UNITY_WEBGL && !UNITY_EDITOR
                    available = MicroPlayableAvailable();
#else
                    available = 0;
#endif
                }

                return available != 0;
            }
        }

        /// <summary>
        /// gameReady: the player can play right now. Sent once, when the garage first comes up —
        /// never while a splash or loading screen is still showing (certification asks exactly that).
        /// </summary>
        public static void Ready()
        {
            if (said)
                return;

            said = true;

#if UNITY_WEBGL && !UNITY_EDITOR
            MicroPlayableReady();
#endif
        }

        /// <summary>Shows an interstitial and always calls back exactly once, ad or no ad.</summary>
        public static void ShowInterstitial(Action finished)
        {
            int ticket = ++interstitialTicket;
            interstitialFinished = finished;

#if UNITY_WEBGL && !UNITY_EDITOR
            MicroPlayableInterstitial(ticket);
#else
            AnswerInterstitial(ticket);
#endif
        }

        /// <summary>
        /// Shows an ad the player asked for and reports whether the reward was earned.
        /// <paramref name="rewardId"/> names the reward (same string every time), never the player.
        /// </summary>
        public static void ShowRewarded(string rewardId, Action<bool> finished)
        {
            int ticket = ++rewardedTicket;
            rewardedFinished = finished;

#if UNITY_WEBGL && !UNITY_EDITOR
            MicroPlayableRewarded(rewardId, ticket);
#else
            AnswerRewarded(ticket, false);
#endif
        }

        /// <summary>Drops whatever ad callbacks are still waiting (used by the watchdog).</summary>
        public static void AbandonPendingAds()
        {
            interstitialTicket++;
            rewardedTicket++;
            interstitialFinished = null;
            rewardedFinished = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            said = false;
            available = -1;
            interstitialFinished = null;
            rewardedFinished = null;
            HostPaused = false;
            HostAudioEnabled = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (GameObject.Find(ReceiverName) != null)
                return;

            var host = new GameObject(ReceiverName);
            host.AddComponent<PlayablesReceiver>();
            host.AddComponent<CloudSave.Receiver>();
            host.AddComponent<WebHost>();
            UnityEngine.Object.DontDestroyOnLoad(host);

#if UNITY_WEBGL && !UNITY_EDITOR
            if (Available)
                MicroPlayableWatch();
#endif
        }

        static void AnswerInterstitial(int ticket)
        {
            if (ticket != interstitialTicket)
                return;

            var waiting = interstitialFinished;
            interstitialFinished = null;
            waiting?.Invoke();
        }

        static void AnswerRewarded(int ticket, bool earned)
        {
            if (ticket != rewardedTicket)
                return;

            var waiting = rewardedFinished;
            rewardedFinished = null;
            waiting?.Invoke(earned);
        }

        /// <summary>The end of every wire from the page. SendMessage needs a GameObject to aim at.</summary>
        sealed class PlayablesReceiver : MonoBehaviour
        {
            public void OnHostPause(string value)
            {
                bool paused = value == "1";
                HostPaused = paused;

                // A pause may be the last moment the game gets: push the save file now.
                if (paused)
                    CloudSave.Push(force: true);

                HostPauseChanged?.Invoke(paused);
            }

            public void OnHostAudio(string value)
            {
                HostAudioEnabled = value == "1";
                HostAudioChanged?.Invoke(HostAudioEnabled);
            }

            public void OnInterstitialFinished(string value)
            {
                if (int.TryParse(value, out int ticket))
                    AnswerInterstitial(ticket);
            }

            /// <summary>"&lt;ticket&gt;:&lt;1 when earned&gt;" — SendMessage carries one string.</summary>
            public void OnRewardedFinished(string value)
            {
                int mark = value.IndexOf(':');
                if (mark < 0 || !int.TryParse(value.Substring(0, mark), out int ticket))
                    return;

                AnswerRewarded(ticket, value.Substring(mark + 1) == "1");
            }
        }
    }
}
