using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.Shell
{
    public sealed class S5ShellSceneController : MonoBehaviour
    {
        [SerializeField] MainMenuScreen mainMenu;
        [SerializeField] MapScreen map;
        [SerializeField] GameOverScreen gameOver;
        [SerializeField] SettingsScreen settings;
        [SerializeField] CalibrateScreen calibrate;
        GameObject confirmationRoot;

        void Awake()
        {
            mainMenu = mainMenu ?? GetComponentInChildren<MainMenuScreen>(true);
            map = map ?? GetComponentInChildren<MapScreen>(true);
            gameOver = gameOver ?? GetComponentInChildren<GameOverScreen>(true);
            settings = settings ?? GetComponentInChildren<SettingsScreen>(true);
            calibrate = calibrate ?? GetComponentInChildren<CalibrateScreen>(true);

            if (mainMenu != null)
            {
                mainMenu.Show();
                settings?.Hide();
                calibrate?.Hide();
                EnsureNewGameConfirmation();
                mainMenu.Configure(GameManager.Instance != null && GameManager.Instance.HasSavedCampaign);
                mainMenu.PlayRequested += OpenMap;
                mainMenu.ContinueRequested += ContinueCampaign;
                mainMenu.NewGameRequested += StartNewGame;
                mainMenu.NewGameConfirmationRequested += ShowNewGameConfirmation;
                mainMenu.SettingsRequested += OpenSettings;
                mainMenu.QuitRequested += Quit;
                BindMainMenuButtons();
            }
            if (map != null)
            {
                var router = SceneRouter.Instance;
                map.SetBossUnlocked(router != null && router.Session.BossUnlocked);
                BuildMapPresentation(router == null ? null : router.Session);
                map.SubjectRequested += StartSubject;
                map.BossRequested += StartBoss;
            }
            if (gameOver != null)
            {
                gameOver.RetryRequested += StartNewGame;
                gameOver.NewGameRequested += StartNewGame;
                gameOver.MenuRequested += OpenMenu;
            }
            if (settings != null)
            {
                settings.SettingsChanged += ApplySettings;
                settings.CalibrateRequested += OpenCalibrate;
                settings.BackRequested += OpenMainMenu;
            }
            if (calibrate != null)
            {
                calibrate.OffsetChanged += ApplyOffset;
                calibrate.BackRequested += OpenSettings;
            }
        }

        void OnDestroy()
        {
            if (mainMenu != null)
            {
                mainMenu.PlayRequested -= OpenMap;
                mainMenu.ContinueRequested -= ContinueCampaign;
                mainMenu.NewGameRequested -= StartNewGame;
                mainMenu.NewGameConfirmationRequested -= ShowNewGameConfirmation;
                mainMenu.SettingsRequested -= OpenSettings;
                mainMenu.QuitRequested -= Quit;
                UnbindMainMenuButtons();
            }
            if (map != null)
            {
                map.SubjectRequested -= StartSubject;
                map.BossRequested -= StartBoss;
            }
            if (gameOver != null)
            {
                gameOver.RetryRequested -= StartNewGame;
                gameOver.NewGameRequested -= StartNewGame;
                gameOver.MenuRequested -= OpenMenu;
            }
            if (settings != null)
            {
                settings.SettingsChanged -= ApplySettings;
                settings.CalibrateRequested -= OpenCalibrate;
                settings.BackRequested -= OpenMainMenu;
            }
            if (calibrate != null)
            {
                calibrate.OffsetChanged -= ApplyOffset;
                calibrate.BackRequested -= OpenSettings;
            }
        }

        static void OpenMap()
        {
            var router = SceneRouter.Instance;
            if (router != null)
                router.Route(SessionRoute.Map);
        }

        static void ContinueCampaign()
        {
            var router = SceneRouter.Instance;
            if (router != null)
                router.ResumeCampaign();
        }

        static void StartNewGame() => GameManager.Instance?.StartNewGame();

        static void OpenMenu() => SceneRouter.Instance?.RouteToMenu();

        void BindMainMenuButtons()
        {
            BindButton("CONTINUEButton", mainMenu.Continue);
            BindButton("NEW GAMEButton", mainMenu.NewGame);
            BindButton("SETTINGSButton", mainMenu.OpenSettings);
            BindButton("QUITButton", mainMenu.Quit);
        }

        void UnbindMainMenuButtons()
        {
            UnbindButton("CONTINUEButton", mainMenu.Continue);
            UnbindButton("NEW GAMEButton", mainMenu.NewGame);
            UnbindButton("SETTINGSButton", mainMenu.OpenSettings);
            UnbindButton("QUITButton", mainMenu.Quit);
        }

        void BindButton(string name, UnityEngine.Events.UnityAction action)
        {
            var target = mainMenu.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button => button.name == name);
            target?.onClick.AddListener(action);
        }

        void UnbindButton(string name, UnityEngine.Events.UnityAction action)
        {
            var target = mainMenu.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button => button.name == name);
            target?.onClick.RemoveListener(action);
        }
        void ShowNewGameConfirmation() => confirmationRoot?.SetActive(true);

        void OpenSettings()
        {
            confirmationRoot?.SetActive(false);
            mainMenu?.Hide();
            calibrate?.Hide();
            settings?.Configure(GameManager.Instance?.Settings);
            settings?.Show();
        }

        void OpenCalibrate()
        {
            settings?.Hide();
            calibrate?.Configure(GameManager.Instance?.Settings?.rhythmOffsetMs ?? 0f);
            calibrate?.Show();
        }

        void OpenMainMenu()
        {
            settings?.Hide();
            calibrate?.Hide();
            mainMenu?.Show();
        }

        void BuildMapPresentation(GameSession session)
            => MapPresentationBuilder.Build(map, session);

        void EnsureNewGameConfirmation()
        {
            if (confirmationRoot != null)
                return;
            confirmationRoot = new GameObject("NewGameConfirmation");
            confirmationRoot.transform.SetParent(transform, false);
            AddButton(confirmationRoot.transform, "CONFIRM NEW GAME", new Vector2(0f, 20f), mainMenu.ConfirmNewGame);
            AddButton(confirmationRoot.transform, "CANCEL", new Vector2(0f, -90f), mainMenu.CancelNewGame);
            confirmationRoot.SetActive(false);
        }

        static Button AddButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var root = new GameObject(label + "Button");
            root.transform.SetParent(parent, false);
            var rect = root.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(360f, 84f);
            rect.anchoredPosition = position;
            root.AddComponent<Image>().color = new Color32(35, 86, 120, 255);
            var button = root.AddComponent<Button>();
            button.onClick.AddListener(action);
            var textObject = new GameObject("Label");
            textObject.transform.SetParent(root.transform, false);
            var labelRect = textObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var text = textObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 24;
            text.color = Color.white;
            return button;
        }

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
