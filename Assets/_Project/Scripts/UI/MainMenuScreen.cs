using System;

namespace KMA.Gameplay.UI
{
    public sealed class MainMenuScreen : ScreenBase
    {
        public event Action NewGameRequested;
        public event Action NewGameConfirmationRequested;
        public event Action ContinueRequested;
        public event Action PlayRequested;
        public event Action SettingsRequested;
        public event Action QuitRequested;
        public bool IsConfirmingNewGame { get; private set; }

        public void Play() => PlayRequested?.Invoke();
        public void NewGame()
        {
            IsConfirmingNewGame = true;
            NewGameConfirmationRequested?.Invoke();
        }
        public void ConfirmNewGame()
        {
            if (!IsConfirmingNewGame)
                return;
            IsConfirmingNewGame = false;
            NewGameRequested?.Invoke();
        }
        public void CancelNewGame() => IsConfirmingNewGame = false;
        public void Continue() => ContinueRequested?.Invoke();
        public void OpenSettings() => SettingsRequested?.Invoke();
        public void Quit() => QuitRequested?.Invoke();
    }
}
