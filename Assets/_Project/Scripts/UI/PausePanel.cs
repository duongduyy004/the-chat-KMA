using System;
using KMA.Gameplay;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    public sealed class PausePanel : MonoBehaviour
    {
        public event Action RestartRequested;
        public event Action ExitToMapRequested;
        public bool IsOpen { get; private set; }
        float previousTimeScale = 1f;

        public void Open()
        {
            if (IsOpen)
                return;
            previousTimeScale = Time.timeScale;
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (behaviour is IPauseAware pauseAware)
                    pauseAware.SetPaused(true);
            Time.timeScale = 0f;
            IsOpen = true;
        }

        public void Resume()
        {
            if (!IsOpen)
                return;
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (behaviour is IPauseAware pauseAware)
                    pauseAware.SetPaused(false);
            Time.timeScale = previousTimeScale;
            IsOpen = false;
        }

        public void Restart()
        {
            Resume();
            RestartRequested?.Invoke();
        }

        public void ExitToMap()
        {
            Resume();
            ExitToMapRequested?.Invoke();
        }
    }
}
