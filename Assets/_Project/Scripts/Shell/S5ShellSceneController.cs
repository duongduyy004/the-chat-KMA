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
                // Settings is authored under SafeArea (also MainMenuScreen). Give it a
                // sibling safe-area root so hiding the menu cannot hide settings too.
                if (settings != null && settings.transform.IsChildOf(mainMenu.transform))
                {
                    settings.transform.SetParent(mainMenu.transform.parent, false);
                    settings.gameObject.AddComponent<RectTransform>();
                    settings.gameObject.AddComponent<SafeAreaFitter>();
                }
                if (settings != null)
                    SettingsPresentationBuilder.Build(settings);
                HomePresentationBuilder.Build(mainMenu);
                mainMenu.Show();
                settings?.Hide();
                calibrate?.Hide();
                EnsureNewGameConfirmation();
                mainMenu.Configure(GameManager.Instance != null && GameManager.Instance.HasSavedCampaign);
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
                BuildMapPresentation(router == null ? null : router.Session);
                map.SubjectRequested += StartSubject;
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
            mainMenu?.CancelNewGame();
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
            mainMenu?.Configure(GameManager.Instance != null && GameManager.Instance.HasSavedCampaign);
            mainMenu?.Show();
        }

        void BuildMapPresentation(GameSession session)
            => MapPresentationBuilder.Build(map, session);

        void EnsureNewGameConfirmation()
        {
            if (confirmationRoot != null)
                return;
            confirmationRoot = new GameObject("NewGameConfirmation", typeof(RectTransform), typeof(Image));
            confirmationRoot.transform.SetParent(mainMenu.transform, false);
            RectTransform overlayRect = confirmationRoot.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            confirmationRoot.GetComponent<Image>().color = new Color(0.015f, .06f, .1f, .82f);

            var card = new GameObject("ConfirmationCard", typeof(RectTransform), typeof(Image), typeof(Outline));
            card.transform.SetParent(confirmationRoot.transform, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(.5f, .5f);
            cardRect.sizeDelta = new Vector2(620f, 380f);
            card.GetComponent<Image>().color = new Color32(255, 249, 231, 255);
            Outline outline = card.GetComponent<Outline>();
            outline.effectColor = new Color32(3, 18, 33, 255);
            outline.effectDistance = new Vector2(5f, -5f);
            AddLabel(card.transform, "Title", "BẮT ĐẦU HÀNH TRÌNH MỚI?", new Vector2(0f, 98f), 34,
                new Color32(8, 35, 61, 255));
            AddLabel(card.transform, "Message", "Tiến độ hiện tại sẽ được thay thế.\nBạn có chắc muốn tiếp tục?",
                new Vector2(0f, 25f), 22, new Color32(62, 79, 96, 255));
            AddButton(card.transform, "XÁC NHẬN", new Vector2(-145f, -105f), mainMenu.ConfirmNewGame,
                new Color32(255, 89, 94, 255));
            AddButton(card.transform, "QUAY LẠI", new Vector2(145f, -105f), () =>
            {
                mainMenu.CancelNewGame();
                confirmationRoot.SetActive(false);
            }, new Color32(25, 130, 196, 255));
            confirmationRoot.SetActive(false);
        }

        static Button AddButton(Transform parent, string label, Vector2 position,
            UnityEngine.Events.UnityAction action, Color color)
        {
            var root = new GameObject(label + "Button");
            root.transform.SetParent(parent, false);
            var rect = root.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250f, 76f);
            rect.anchoredPosition = position;
            root.AddComponent<Image>().color = color;
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color32(3, 18, 33, 255);
            outline.effectDistance = new Vector2(3f, -3f);
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            return button;
        }

        static Text AddLabel(Transform parent, string name, string value, Vector2 position,
            int fontSize, Color color)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(540f, 90f);
            rect.anchoredPosition = position;
            Text text = root.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        static void StartSubject(SubjectId subject)
        {
            var router = SceneRouter.Instance;
            if (router != null)
                router.StartSubject(subject);
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
