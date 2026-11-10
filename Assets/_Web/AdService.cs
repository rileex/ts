using System;
using UnityEngine;

namespace MicroCar.Web
{
    /// <summary>
    /// The only place in the game that decides how an ad is shown.
    ///
    /// <para>
    /// WebGL: YouTube Playables ads through <see cref="Playables"/>. While an ad is up the game is
    /// held (time and sound) by <see cref="WebHost.Hold"/>. Every call answers exactly once — a
    /// swallowed callback would leave the player in front of a frozen screen.
    /// </para>
    ///
    /// <para>
    /// Rules agreed for McPlay: rewarded only from a button the player pressed; interstitials only at
    /// the seams (race start from the garage, leaving a race, restart), never during a race, and not
    /// more often than <see cref="MinInterstitialGap"/>.
    /// </para>
    /// </summary>
    public static class AdService
    {
        public const float MinInterstitialGap = 60f;

        /// <summary>The reward ids. Same string every time a reward is offered, never anything personal.</summary>
        public const string RewardDoublePrize = "double_prize";

        static float lastInterstitial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // The pre-roll has just played when the game starts: no interstitial in the first minute.
            lastInterstitial = 0f;
        }

        public static bool InterstitialDue => Time.realtimeSinceStartup - lastInterstitial >= MinInterstitialGap;

        public static void ShowInterstitial(Action finished = null)
        {
            if (!InterstitialDue || !Playables.Available)
            {
                finished?.Invoke();
                return;
            }

            lastInterstitial = Time.realtimeSinceStartup;
            WebHost.Hold();

            bool settled = false;
            void Done()
            {
                if (settled) return;
                settled = true;
                WebHost.Release();
                finished?.Invoke();
            }

            WebHost.Watchdog(90f, () => { if (!settled) { Playables.AbandonPendingAds(); Done(); } });
            Playables.ShowInterstitial(Done);
        }

        public static void ShowRewarded(string rewardId, Action<bool> finished)
        {
            if (!Playables.Available)
            {
#if UNITY_EDITOR
                Debug.Log($"[Ads] Rewarded '{rewardId}' granted without an ad (editor).");
                finished?.Invoke(true);
#else
                finished?.Invoke(false);
#endif
                return;
            }

            WebHost.Hold();

            bool settled = false;
            void Done(bool earned)
            {
                if (settled) return;
                settled = true;
                WebHost.Release();
                if (earned)
                    lastInterstitial = Time.realtimeSinceStartup; // don't follow a rewarded ad with an interstitial
                finished?.Invoke(earned);
            }

            WebHost.Watchdog(120f, () => { if (!settled) { Playables.AbandonPendingAds(); Done(false); } });
            Playables.ShowRewarded(rewardId, Done);
        }
    }
}
