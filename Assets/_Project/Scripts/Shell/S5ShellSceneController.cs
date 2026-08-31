using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using UnityEngine;

namespace KMA.Gameplay.Shell
{
    public sealed class S5ShellSceneController : MonoBehaviour
    {
        [SerializeField] MainMenuScreen mainMenu;
        [SerializeField] MapScreen map;
        [SerializeField] GameOverScreen gameOver;
        [SerializeField] SettingsScreen settings;
        [SerializeField] CalibrateScreen calibrate;

        void Awake()
        {
            mainMenu = mainMenu ?? GetComponentInChildren<MainMenuScreen>(true);
            map = map ?? GetComponentInChildren<MapScreen>(true);
            gameOver = gameOver ?? GetComponentInChildren<GameOverScreen>(true);
            settings = settings ?? GetComponentInChildren<SettingsScreen>(true);
            calibrate = calibrate ?? GetComponentInChildren<CalibrateScreen>(true);

            if (mainMenu != null)
            {
                mainMenu.PlayRequested += OpenMap;
                mainMenu.ContinueRequested += OpenMap;
                mainMenu.NewGameRequested += StartNewGame;
                mainMenu.QuitRequested += Quit;
            }
            if (map != null)
            {
                var router = SceneRouter.Instance;
                map.SetBossUnlocked(router != null && router.Session.BossUnlocked);
                map.SubjectRequested += StartSubject;
                map.BossRequested += StartBoss;
            }
            if (gameOver != null)
            {
                gameOver.RetryRequested += OpenMap;
                gameOver.NewGameRequested += StartNewGame;
                gameOver.MenuRequested += OpenMap;
            }
            if (settings != null)
                settings.SettingsChanged += ApplySettings;
            if (calibrate != null)
                calibrate.OffsetChanged += ApplyOffset;
        }

        void OnDestroy()
        {
            if (mainMenu != null)
            {
                mainMenu.PlayRequested -= OpenMap;
                mainMenu.ContinueRequested -= OpenMap;
                mainMenu.NewGameRequested -= StartNewGame;
                mainMenu.QuitRequested -= Quit;
            }
            if (map != null)
            {
                map.SubjectRequested -= StartSubject;
                map.BossRequested -= StartBoss;
            }
            if (gameOver != null)
            {
                gameOver.RetryRequested -= OpenMap;
                gameOver.NewGameRequested -= StartNewGame;
                gameOver.MenuRequested -= OpenMap;
            }
            if (settings != null)
                settings.SettingsChanged -= ApplySettings;
            if (calibrate != null)
                calibrate.OffsetChanged -= ApplyOffset;
        }

        static void OpenMap()
        {
            var router = SceneRouter.Instance;
            if (router != null)
                router.Route(SessionRoute.Map);
        }

        static void StartNewGame() => GameManager.Instance?.StartNewGame();

        static void StartSubject(SubjectId subject)
        {
            var router = SceneRouter.Instance;
            if (router != null)
                router.StartSubject(subject);
        }

        static void StartBoss()
        {
            var router = SceneRouter.Instance;
            if (router != null)
                router.StartBoss();
        }

        static void ApplySettings(Settings value) => GameManager.Instance?.UpdateSettings(value);

        static void ApplyOffset(float value)
        {
            var manager = GameManager.Instance;
            if (manager == null || manager.Settings == null)
                return;
            manager.Settings.rhythmOffsetMs = value;
            manager.UpdateSettings(manager.Settings);
        }

        static void Quit() => Application.Quit();
    }
}
