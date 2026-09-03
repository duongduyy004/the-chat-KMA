using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
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
        {
            if (map == null || map.transform.Find("S5MapPresentation") != null)
                return;
            foreach (var button in map.GetComponentsInChildren<Button>(true))
                button.gameObject.SetActive(false);

            var root = new GameObject("S5MapPresentation");
            root.transform.SetParent(map.transform, false);
            var heartObject = new GameObject("HeartBar");
            heartObject.transform.SetParent(root.transform, false);
            var heartBar = heartObject.AddComponent<HeartBar>();
            var hearts = new Image[5];
            for (var i = 0; i < hearts.Length; i++)
            {
                var heart = new GameObject("Heart" + i);
                heart.transform.SetParent(heartObject.transform, false);
                var rect = heart.AddComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(-120f + i * 60f, 300f);
                rect.sizeDelta = new Vector2(48f, 48f);
                hearts[i] = heart.AddComponent<Image>();
            }
            heartBar.SetSlots(hearts);

            var nodes = new System.Collections.Generic.List<MapNodeView>();
            var subjects = (SubjectId[])System.Enum.GetValues(typeof(SubjectId));
            for (var i = 0; i < subjects.Length; i++)
            {
                var id = subjects[i];
                var node = CreateMapNode(root.transform, id.ToString(), id, false,
                    new Vector2(-480f + (i % 4) * 320f, 160f - (i / 4) * 180f),
                    () => map.SelectSubject(id));
                nodes.Add(node);
            }
            foreach (var coming in new[] { "PushUps", "Rhythm", "Swimming" })
            {
                nodes.Add(CreateMapNode(root.transform, coming, SubjectId.Sprint, true,
                    new Vector2(-320f + nodes.Count % 3 * 320f, -260f), null));
            }
            CreateMapNode(root.transform, "BOSS", SubjectId.Sprint, false,
                new Vector2(560f, -260f), map.SelectBoss);
            map.BindPresentation(nodes.ToArray(), heartBar, session);
        }

        static MapNodeView CreateMapNode(Transform parent, string name, SubjectId id, bool comingSoon, Vector2 position,
            UnityEngine.Events.UnityAction action)
        {
            var root = new GameObject(name + "Node");
            root.transform.SetParent(parent, false);
            var rect = root.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280f, 120f);
            rect.anchoredPosition = position;
            var node = root.AddComponent<MapNodeView>();
            var titleObject = new GameObject("Title");
            titleObject.transform.SetParent(root.transform, false);
            var title = titleObject.AddComponent<Text>();
            title.alignment = TextAnchor.MiddleCenter;
            var detailObject = new GameObject("Detail");
            detailObject.transform.SetParent(root.transform, false);
            var detail = detailObject.AddComponent<Text>();
            detail.alignment = TextAnchor.MiddleCenter;
            detail.rectTransform.anchoredPosition = new Vector2(0f, -30f);
            var button = root.AddComponent<Button>();
            button.targetGraphic = root.AddComponent<Image>();
            if (action != null)
                button.onClick.AddListener(action);
            node.Bind(button, title, detail);
            node.Configure(id, name, comingSoon, null, 5);
            return node;
        }

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
