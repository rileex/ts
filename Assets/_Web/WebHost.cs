using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MicroCar.Web
{
    /// <summary>
    /// Runtime glue between the template game and the platform. Lives on the persistent 'Playables'
    /// object created before the first scene loads.
    ///
    /// <list type="bullet">
    /// <item>Platform pause / ads hold the game: Time.timeScale = 0 and AudioListener.pause. Holds nest.</item>
    /// <item>Platform mute always wins (AudioListener.pause), there is no in-game master mute.</item>
    /// <item>gameReady is sent when the garage first comes up.</item>
    /// <item>Interstitials at the seams: garage → race, race → garage, race restart.</item>
    /// <item>Progress is pushed to the cloud on every scene load and every few seconds if it changed.</item>
    /// </list>
    /// </summary>
    public sealed class WebHost : MonoBehaviour
    {
        public const string SceneSplash = "1_firstScene";
        public const string SceneGarage = "Garage";

        const float PushEvery = 5f;

        static WebHost instance;
        static int holds;
        static float timeScaleBefore = 1f;

        string previousScene;
        float nextPush;

        /// <summary>Freezes the game (time + sound) until a matching <see cref="Release"/>.</summary>
        public static void Hold()
        {
            if (holds++ > 0)
                return;

            timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;
            ApplyAudio();
        }

        public static void Release()
        {
            if (holds == 0)
                return;

            if (--holds > 0)
                return;

            Time.timeScale = timeScaleBefore;
            ApplyAudio();
        }

        /// <summary>Calls <paramref name="onTimeout"/> after real seconds, unless the game is gone.</summary>
        public static void Watchdog(float seconds, Action onTimeout)
        {
            if (instance != null)
                instance.StartCoroutine(WatchdogRoutine(seconds, onTimeout));
        }

        static IEnumerator WatchdogRoutine(float seconds, Action onTimeout)
        {
            yield return new WaitForSecondsRealtime(seconds);
            onTimeout?.Invoke();
        }

        static void ApplyAudio()
        {
            AudioListener.pause = holds > 0 || !Playables.HostAudioEnabled;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            holds = 0;
            timeScaleBefore = 1f;
        }

        void Awake()
        {
            instance = this;
            Playables.HostPauseChanged += HandleHostPause;
            Playables.HostAudioChanged += HandleHostAudio;
            SceneManager.sceneLoaded += OnSceneLoaded;
            CloudSave.Request();
        }

        void OnDestroy()
        {
            Playables.HostPauseChanged -= HandleHostPause;
            Playables.HostAudioChanged -= HandleHostAudio;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // Not "OnHostPause"/"OnHostAudio": this component sits on the same GameObject the page
        // SendMessages to, and SendMessage would pick these up and fail to pass them a string
        // (MissingMethodException in the browser console, never in the editor).
        void HandleHostPause(bool paused)
        {
            if (paused) Hold();
            else Release();
        }

        void HandleHostAudio(bool enabled) => ApplyAudio();

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string from = previousScene;
            previousScene = scene.name;

            // The template's pause menu and race exit set AudioListener.volume = 0 and the garage
            // puts it back; the platform's mute is AudioListener.pause, kept separate on purpose.
            ApplyAudio();

            if (scene.name == SceneGarage)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                HideQuitButtons(scene);
#endif
                StartCoroutine(AnnounceReady());
            }

            // Ads only at the seams, and never on the first trip from the splash to the garage.
            bool raceStart = from == SceneGarage && scene.name != SceneGarage;
            bool raceExit = from != null && from != SceneSplash && from != SceneGarage && scene.name == SceneGarage;
            bool restart = from != null && from == scene.name && scene.name != SceneGarage;
            if (raceStart || raceExit || restart)
                AdService.ShowInterstitial();

            CloudSave.Push();
        }

        /// <summary>A browser game has nowhere to quit to: hide every button wired to MainUtility.Exit.</summary>
        static void HideQuitButtons(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                {
                    var click = button.onClick;
                    for (int i = 0; i < click.GetPersistentEventCount(); i++)
                    {
                        if (click.GetPersistentMethodName(i) == "Exit" && click.GetPersistentTarget(i) is ALIyerEdon.MainUtility)
                        {
                            button.gameObject.SetActive(false);
                            break;
                        }
                    }
                }
            }
        }

        IEnumerator AnnounceReady()
        {
            // One frame so the garage has drawn itself; the player can interact from here on.
            yield return null;
            Playables.Ready();
        }

        void Update()
        {
            if (Time.unscaledTime < nextPush)
                return;

            nextPush = Time.unscaledTime + PushEvery;
            CloudSave.Push();
        }
    }
}
