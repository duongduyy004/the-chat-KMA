using System;

namespace KMA.Gameplay.UI
{
    public sealed class MainMenuScreen : ScreenBase
    {
        public event Action NewGameRequested;
        public event Action ContinueRequested;
        public event Action PlayRequested;
        public event Action SettingsRequested;
        public event Action QuitRequested;

        public void Play() => PlayRequested?.Invoke();
        public void NewGame() => NewGameRequested?.Invoke();
        public void Continue() => ContinueRequested?.Invoke();
        public void OpenSettings() => SettingsRequested?.Invoke();
        public void Quit() => QuitRequested?.Invoke();
    }
}
