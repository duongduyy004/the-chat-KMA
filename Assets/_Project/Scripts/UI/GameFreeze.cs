using System.Collections.Generic;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    /// Freezes gameplay while any holder (the pause menu, the first-run guide) is open. The first
    /// Acquire saves Time.timeScale and pauses every IPauseAware; the last Release restores both.
    public static class GameFreeze
    {
        static readonly HashSet<object> holders = new HashSet<object>();
        static float previousTimeScale = 1f;

        public static bool IsFrozen => holders.Count > 0;
        public static bool Holds(object holder) => holder != null && holders.Contains(holder);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            holders.Clear();
            previousTimeScale = 1f;
        }

        public static void Acquire(object holder)
        {
            if (holder == null || !holders.Add(holder) || holders.Count > 1)
                return;
            previousTimeScale = Time.timeScale;
            NotifyPauseAware(true);
            Time.timeScale = 0f;
        }

        public static void Release(object holder)
        {
            if (holder == null || !holders.Remove(holder) || holders.Count > 0)
                return;
            NotifyPauseAware(false);
            Time.timeScale = previousTimeScale;
        }

        static void NotifyPauseAware(bool paused)
        {
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (behaviour is IPauseAware pauseAware)
                    pauseAware.SetPaused(paused);
        }
    }
}
