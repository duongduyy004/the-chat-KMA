using System;
using UnityEngine;

namespace KMA.Gameplay.UI
{
    public sealed class GameOverScreen : ScreenBase
    {
        [SerializeField] Sprite background;
        public Sprite Background => background;
        public void SetBackground(Sprite sprite) => background = sprite;

        public event Action RetryRequested;
        public event Action NewGameRequested;
        public event Action MenuRequested;
        public event Action RestartRequested;
        public event Action ExitToMapRequested;

        public void Retry() => RetryRequested?.Invoke();
        public void NewGame() => NewGameRequested?.Invoke();
        public void Restart() => RestartRequested?.Invoke();
        public void ExitToMap() => ExitToMapRequested?.Invoke();
        public void ReturnToMenu() => MenuRequested?.Invoke();
    }
}
