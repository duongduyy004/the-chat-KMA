using System;

namespace KMA.Gameplay.UI
{
    public sealed class GameOverScreen : ScreenBase
    {
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
