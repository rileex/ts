using System.Collections;
using System.Collections.Generic;
using ALIyerEdon;
using UnityEngine;

namespace MicroCar.UI
{
    /// <summary>
    /// The "Effects" volume: everything except the music (engine, skids, nitro, crashes, the start
    /// countdown, clicks). The web build has no AudioMixer, so every AudioSource is scaled one by one.
    ///
    /// <para>
    /// The cars' scripts own their sources' volumes and some of them ease it from the current value
    /// (the engine: Lerp(volume, target)). So the scaled value must never be what they read: late in
    /// the frame, after every script and before the audio update, each volume is scaled; at the end of
    /// the frame the owner's value is put back. A volume that differs from what was applied has just
    /// been set by its owner and becomes the new owner's value. The music (Load_Settings.music) keeps
    /// its own slider.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class SfxVolume : MonoBehaviour
    {
        public const string Key = "SfxVolume";

        public static float Value { get; private set; } = 1f;

        public static void Set(float value)
        {
            Value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(Key, Value);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            Value = Mathf.Clamp01(PlayerPrefs.GetFloat(Key, 1f));
            var go = new GameObject("SfxVolume");
            DontDestroyOnLoad(go);
            go.AddComponent<SfxVolume>();
        }

        struct Level { public float owner, applied; }

        readonly Dictionary<AudioSource, Level> levels = new Dictionary<AudioSource, Level>();
        readonly List<AudioSource> sources = new List<AudioSource>();
        readonly HashSet<AudioSource> music = new HashSet<AudioSource>();
        readonly List<AudioSource> gone = new List<AudioSource>();
        readonly WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
        float nextScan;

        IEnumerator Start()
        {
            while (true)
            {
                yield return endOfFrame;
                Restore();
            }
        }

        void LateUpdate()
        {
            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + 0.5f;
                Rescan();
            }

            float v = Value;
            foreach (var src in sources)
            {
                if (src == null || music.Contains(src)) continue;
                float now = src.volume;
                if (!levels.TryGetValue(src, out var level) || !Mathf.Approximately(now, level.applied))
                    level.owner = now; // new, or set by its owner since the last frame
                level.applied = level.owner * v;
                if (!Mathf.Approximately(now, level.applied))
                    src.volume = level.applied;
                levels[src] = level;
            }
        }

        void Restore()
        {
            foreach (var src in sources)
            {
                if (src == null || !levels.TryGetValue(src, out var level)) continue;
                if (Mathf.Approximately(src.volume, level.applied))
                {
                    src.volume = level.owner;
                    level.applied = level.owner; // what the owner sees until the next scaling
                    levels[src] = level;
                }
            }
        }

        void Rescan()
        {
            // The cloud save may have brought a different value since the start.
            Value = Mathf.Clamp01(PlayerPrefs.GetFloat(Key, Value));

            music.Clear();
            foreach (var ls in FindObjectsByType<Load_Settings>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (ls.music) music.Add(ls.music);

            sources.Clear();
            sources.AddRange(FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None));

            gone.Clear();
            foreach (var src in levels.Keys)
                if (src == null) gone.Add(src);
            foreach (var src in gone)
                levels.Remove(src);
        }
    }
}
