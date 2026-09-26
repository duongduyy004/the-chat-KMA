#if UNITY_EDITOR
using System;
using System.IO;
using KMA.Gameplay;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KMA.EditorTools
{
    public static class FootballSceneConfigurator
    {
        public const string ScenePath = "Assets/_Project/Scenes/MG_Football.unity";
        const string ArtRoot = "Assets/_Project/Art/Football/";
        const string ConfigPath = "Assets/_Project/ScriptableObjects/Football/FootballDifficulty.asset";
        const string SubjectPath = "Assets/_Project/ScriptableObjects/Subjects/Football.asset";
        const string FontPath = "Assets/_Project/Fonts/Baloo2-ExtraBold.asset";
        static readonly Color Ink = new Color32(14, 43, 57, 255);
        static readonly Color Sky = new Color32(120, 207, 235, 255);
        static readonly Color Grass = new Color32(39, 153, 59, 255);
        static readonly Color Gold = new Color32(255, 202, 40, 255);

        [MenuItem("KMA/Football/Build Scene")]
        public static void BuildScene()
        {
            if (!File.Exists(ScenePath))
                throw new FileNotFoundException("MG_Football scene is missing; refusing to create a new scene identity.", ScenePath);
            ImportGoalViewArt();
            EnsureConfiguration();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(root);
            BuildWorld(scene);
            BuildUi(scene);
            ConfigureSubjectAsset();
            EnsureInBuildSettings();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[KMA] MG_Football penalty scene built and saved.");
        }

        static void EnsureConfiguration()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            if (AssetDatabase.LoadAssetAtPath<FootballDifficultyConfig>(ConfigPath) != null) return;
            var config = ScriptableObject.CreateInstance<FootballDifficultyConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }

        static void BuildWorld(Scene scene)
        {
            var cameraRoot = new GameObject("GameCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraRoot, scene);
            cameraRoot.tag = "MainCamera";
            cameraRoot.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraRoot.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Sky;
            camera.nearClipPlane = .3f;
            camera.farClipPlane = 100f;
            AddUrpCameraData(cameraRoot);

            var cameraFit = cameraRoot.AddComponent<FootballCameraFit>();
            var world = new GameObject("FootballWorld");
            SceneManager.MoveGameObjectToScene(world, scene);
            var presentation = world.AddComponent<FootballPresentation>();
            SpriteRenderer Layer(string name, string image, float x, float y, float width, float height, int order) =>
                Renderer(world.transform, name, LoadSprite("GoalView/" + image + ".png"),
                    FootballPresentation.ScreenToWorld(x, y), order, new Vector2(width, height) * FootballPresentation.PixelToWorld);
            var field = Layer("Field", "field", 600, 337.5f, 1200, 675, 0);
            cameraFit.Configure(field);
            var goal = Layer("Goal", "goal", 600, 230, 620, 170, 10);
            var net = Layer("GoalNet", "net", 600, 230, 620, 170, 11);
            var keeper = Layer("Goalkeeper", "keeper", 600, 281, 140, 130, 20);
            var player = Layer("Player", "player", 490, 486, 140, 230, 21);
            var ball = Layer("Ball", "ball", 600, 486, 36, 36, 25);
            var shadow = Layer("BallShadow", "shadow", 600, 502, 48, 16, 19);
            var crosshair = Layer("AimCrosshair", "crosshair", 600, 213, 44, 44, 30);
            crosshair.color = new Color32(224, 255, 150, 255);
            crosshair.enabled = false;
            var dots = new SpriteRenderer[140];
            for (int i = 0; i < dots.Length; i++)
            {
                dots[i] = Layer("TrajectoryDot" + i, "knob", 600, 486, 5, 5, 29);
                dots[i].color = new Color(.85f, 1f, .6f, .75f);
                dots[i].enabled = false;
            }
            var left = new GameObject("GoalLeftInsidePost").transform;
            left.SetParent(world.transform, false);
            left.position = FootballPresentation.ScreenToWorld(300, 302);
            var right = new GameObject("GoalRightInsidePost").transform;
            right.SetParent(world.transform, false);
            right.position = FootballPresentation.ScreenToWorld(900, 302);
            presentation.Configure(field, goal, ball, shadow, player, keeper, crosshair, left, right, dots, net);
        }

        static void ImportGoalViewArt()
        {
            foreach (string file in Directory.GetFiles(ArtRoot + "GoalView", "*.png"))
            {
                AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(file);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 4096;
                importer.spritePixelsPerUnit = Path.GetFileNameWithoutExtension(file) == "panel" ? 50f : 200f;
                importer.filterMode = FilterMode.Bilinear;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = Path.GetFileNameWithoutExtension(file) switch
                {
                    "keeper" => new Vector2(.5f, 27f / 130f),
                    "player" => new Vector2(65f / 140f, .5f),
                    _ => new Vector2(.5f, .5f)
                };
                settings.spriteBorder = Path.GetFileNameWithoutExtension(file) == "panel" ? new Vector4(12f, 12f, 12f, 12f) : Vector4.zero;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        static void BuildUi(Scene scene)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (!font) throw new InvalidOperationException("Missing Baloo2 TMP font asset: " + FontPath);
            var canvasRoot = new GameObject("FootballHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasRoot, scene);
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            var safe = Rect(canvasRoot.transform, "SafeAreaRoot", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            var hud = safe.gameObject.AddComponent<FootballHud>();

            var panelSprite = LoadSprite("GoalView/panel.png");
            var buttonSprite = LoadSprite("GoalView/panel.png");
            var fillSprite = LoadSprite("GoalView/fill.png");

            Text(safe, "ScoreLabel", "BÀN: 0", font, 42, Color.white, TextAlignmentOptions.Center,
                new Vector2(.38f, .91f), new Vector2(.62f, .99f));
            Text(safe, "RemainingLabel", "CÒN 5 LƯỢT", font, 30, Color.white, TextAlignmentOptions.Center,
                new Vector2(.70f, .92f), new Vector2(.96f, .99f));
            var markerLabels = new TMP_Text[5];
            for (int i = 0; i < markerLabels.Length; i++)
                markerLabels[i] = Text(safe, "KickMarker" + (i + 1), "•", font, 25, Gold, TextAlignmentOptions.Center,
                    new Vector2(.405f + i * .038f, .855f), new Vector2(.435f + i * .038f, .905f));

            var directionPanel = Panel(safe, "DirectionPanel", panelSprite, new Vector2(.025f, .025f), new Vector2(.395f, .205f),
                Vector2.zero, Vector2.zero, new Color32(18, 60, 52, 235));
            Text(directionPanel.transform, "DirectionTitle", "HƯỚNG BÓNG", font, 26, Color.white,
                TextAlignmentOptions.Left, new Vector2(.05f, .72f), new Vector2(.58f, .96f));
            var directionText = Text(directionPanel.transform, "DirectionValue", "PHẢI 55%", font, 24, Gold,
                TextAlignmentOptions.Right, new Vector2(.58f, .72f), new Vector2(.95f, .96f));
            var direction = CreateDirectionSlider(directionPanel.transform, buttonSprite);
            Text(directionPanel.transform, "DirectionHint", "Kéo chọn hướng · Giữ SÚT để xem đường bay", font, 21, Color.white,
                TextAlignmentOptions.Center, new Vector2(.04f, .02f), new Vector2(.96f, .28f));
            var powerBackground = CreateImage(safe, "PowerBarBackground", buttonSprite, Ink,
                new Vector2(.80f, .16f), new Vector2(.975f, .20f), Vector2.zero, Vector2.zero);
            var power = CreateImage(powerBackground.transform, "PowerFill", fillSprite, new Color32(214, 247, 137, 255),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            power.type = Image.Type.Filled;
            power.fillMethod = Image.FillMethod.Horizontal;
            power.fillOrigin = (int)Image.OriginHorizontal.Left;
            var percent = Text(powerBackground.transform, "PowerPercent", "0%", font, 24, Color.white,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            var warning = Text(safe, "OverPowerWarning", "DỄ VƯỢT XÀ", font, 24, new Color32(255, 152, 117, 255),
                TextAlignmentOptions.Center, new Vector2(.79f, .205f), new Vector2(.985f, .25f)).gameObject;
            warning.SetActive(false);
            var shoot = Button(safe, "SHOOT", "GIỮ ĐỂ SÚT", buttonSprite, new Color32(214, 247, 137, 255), font, 34,
                new Vector2(.80f, .035f), new Vector2(.975f, .145f), Vector2.zero, new Vector2(.5f, .5f));
            var hold = shoot.gameObject.AddComponent<FootballHoldButton>();
            var feedback = Text(safe, "ShotFeedback", "", font, 48, Color.white,
                TextAlignmentOptions.Center, new Vector2(.20f, .69f), new Vector2(.80f, .79f));

            var startPanel = Panel(safe, "StartPanel", panelSprite, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(850f, 585f), new Color32(248, 250, 252, 252));
            Text(startPanel.transform, "StartTitle", "LOẠT SÚT LUÂN LƯU", font, 54, Ink,
                TextAlignmentOptions.Center, new Vector2(.06f, .76f), new Vector2(.94f, .94f));
            Text(startPanel.transform, "StartInstructions", "Kéo thanh chọn hướng. Giữ SÚT để xem đường bay, thả để đá.\nGhi ít nhất 3 bàn sau 5 lượt.",
                font, 29, Ink, TextAlignmentOptions.Center, new Vector2(.08f, .57f), new Vector2(.92f, .77f));
            Text(startPanel.transform, "DifficultyTitle", "ĐỘ KHÓ", font, 25, Ink,
                TextAlignmentOptions.Center, new Vector2(.1f, .45f), new Vector2(.9f, .55f));
            var easy = Button(startPanel.transform, "Easy", "DỄ", buttonSprite, Color.white, font, 28,
                new Vector2(.08f, .30f), new Vector2(.34f, .44f), Vector2.zero, Vector2.zero);
            var normal = Button(startPanel.transform, "Normal", "THƯỜNG", buttonSprite, Gold, font, 28,
                new Vector2(.35f, .30f), new Vector2(.65f, .44f), Vector2.zero, Vector2.zero);
            var hard = Button(startPanel.transform, "Hard", "KHÓ", buttonSprite, Color.white, font, 28,
                new Vector2(.66f, .30f), new Vector2(.92f, .44f), Vector2.zero, Vector2.zero);
            var startButton = Button(startPanel.transform, "StartButton", "BẮT ĐẦU", buttonSprite, Gold, font, 38,
                new Vector2(.29f, .07f), new Vector2(.71f, .24f), Vector2.zero, Vector2.zero);

            var countdownPanel = Panel(safe, "CountdownPanel", panelSprite, new Vector2(.5f, .55f), new Vector2(.5f, .55f),
                Vector2.zero, new Vector2(560f, 190f), new Color32(248, 250, 252, 230));
            var countdown = Text(countdownPanel.transform, "CountdownText", "SẴN SÀNG!", font, 58, Ink,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            countdownPanel.SetActive(false);

            var resultRoot = Panel(safe, "FootballResultPanel", panelSprite, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(5f / 255f, 28f / 255f, 42f / 255f, .76f));
            var resultCard = Panel(resultRoot.transform, "ResultCard", panelSprite, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(720f, 520f), new Color32(248, 250, 252, 255));
            var resultStatus = Text(resultCard.transform, "ResultStatus", "THẤT BẠI", font, 52, Ink,
                TextAlignmentOptions.Center, new Vector2(.08f, .77f), new Vector2(.92f, .93f));
            var resultGoals = Text(resultCard.transform, "ResultGoals", "0/5", font, 38, Ink,
                TextAlignmentOptions.Center, new Vector2(.08f, .62f), new Vector2(.92f, .77f));
            var resultScore = Text(resultCard.transform, "ResultScore", "0", font, 32, Ink,
                TextAlignmentOptions.Center, new Vector2(.08f, .48f), new Vector2(.92f, .62f));
            var resultRank = Text(resultCard.transform, "ResultRank", "XẾP HẠNG F", font, 30, Ink,
                TextAlignmentOptions.Center, new Vector2(.08f, .36f), new Vector2(.92f, .48f));
            var resultLives = Text(resultCard.transform, "ResultLives", "5", font, 25, Ink,
                TextAlignmentOptions.Center, new Vector2(.08f, .27f), new Vector2(.92f, .36f));
            var resultError = Text(resultCard.transform, "ResultError", string.Empty, font, 21,
                new Color32(190, 45, 28, 255), TextAlignmentOptions.Center, new Vector2(.05f, .20f), new Vector2(.95f, .29f));
            var continueButton = Button(resultCard.transform, "ContinueButton", "TIẾP TỤC", buttonSprite, Color.white, font, 28,
                new Vector2(.53f, .04f), new Vector2(.94f, .19f), Vector2.zero, Vector2.zero);
            var retryButton = Button(resultCard.transform, "RetryButton", "CHƠI LẠI", buttonSprite, Gold, font, 28,
                new Vector2(.06f, .04f), new Vector2(.47f, .19f), Vector2.zero, Vector2.zero);
            resultRoot.SetActive(false);

            var back = Button(safe, "BackButton", "‹", buttonSprite, Color.white, font, 48,
                new Vector2(0f, .91f), new Vector2(.065f, 1f), new Vector2(34f, -24f), new Vector2(0f, 1f));

            hud.Configure(direction, hold, power, percent, warning, FindText(safe, "ScoreLabel"),
                FindText(safe, "RemainingLabel"), markerLabels, startPanel, startButton, easy, normal, hard,
                countdownPanel, countdown, directionText, feedback);
            hud.ShowStart(FootballDifficulty.Normal);

            var result = resultRoot.AddComponent<FootballResultPanel>();
            result.Configure(resultRoot, resultStatus, resultGoals, resultScore, resultRank, resultLives,
                continueButton, retryButton, resultError);

            var config = AssetDatabase.LoadAssetAtPath<FootballDifficultyConfig>(ConfigPath);
            var input = new GameObject("FootballInputBridge").AddComponent<FootballInputBridge>();
            SceneManager.MoveGameObjectToScene(input.gameObject, scene);
            var world = GameObject.Find("FootballWorld");
            var presentation = world.GetComponent<FootballPresentation>();
            var controller = input.gameObject.AddComponent<FootballController>();
            controller.Configure(config, input, presentation, hud, result);
            UnityEventTools.AddPersistentListener(back.onClick, controller.ExitToMap);
            input.gameObject.AddComponent<MinigamePresentationOwner>();
            EnsureEventSystem(scene);
        }

        static Slider CreateDirectionSlider(Transform parent, Sprite sprite)
        {
            var rect = Rect(parent, "DirectionSlider", new Vector2(.06f, .28f), new Vector2(.94f, .74f), Vector2.zero, Vector2.zero);
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            var track = CreateImage(rect, "Track", sprite, new Color32(125, 163, 119, 255),
                new Vector2(0f, .40f), new Vector2(1f, .60f), Vector2.zero, Vector2.zero);
            track.raycastTarget = false;
            var area = Rect(rect, "HandleSlideArea", new Vector2(0f, .5f), new Vector2(1f, .5f), Vector2.zero, new Vector2(-36f, 42f));
            var handle = CreateImage(area, "Handle", LoadSprite("GoalView/knob.png"), new Color32(214, 247, 137, 255),
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(42f, 0f));
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = -1f;
            slider.maxValue = 1f;
            slider.value = .55f;
            return slider;
        }

        static void ConfigureSubjectAsset()
        {
            var subject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SubjectPath);
            if (!subject) return;
            var serialized = new SerializedObject(subject);
            serialized.FindProperty("goalText").stringValue = "Ghi ít nhất 3 bàn sau 5 lượt sút.";
            serialized.FindProperty("timeLimit").floatValue = 0f;
            serialized.FindProperty("unlocked").boolValue = true;
            serialized.FindProperty("comingSoon").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(subject);
        }

        static void EnsureInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == ScenePath)
                {
                    if (!scenes[i].enabled) { scenes[i].enabled = true; EditorBuildSettings.scenes = scenes; }
                    return;
                }
            }
            Array.Resize(ref scenes, scenes.Length + 1);
            scenes[scenes.Length - 1] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = scenes;
        }

        static void EnsureEventSystem(Scene scene)
        {
            var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (!eventSystem || eventSystem.gameObject.scene != scene)
            {
                var root = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(root, scene);
                return;
            }
            foreach (var module in eventSystem.GetComponents<BaseInputModule>())
                if (!(module is InputSystemUIInputModule)) UnityEngine.Object.DestroyImmediate(module);
            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>() ?? eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        static void AddUrpCameraData(GameObject cameraRoot)
        {
            const string typeName = "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime";
            var type = Type.GetType(typeName);
            if (type != null && cameraRoot.GetComponent(type) == null)
                cameraRoot.AddComponent(type);
        }

        static SpriteRenderer Renderer(Transform parent, string name, Sprite sprite, Vector3 position,
            int sortingOrder, Vector2 worldSize)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.position = position;
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.drawMode = SpriteDrawMode.Simple;
            if (sprite)
            {
                var size = sprite.bounds.size;
                child.transform.localScale = new Vector3(worldSize.x / size.x, worldSize.y / size.y, 1f);
            }
            return renderer;
        }

        static Sprite LoadSprite(string path)
        {
            string full = ArtRoot + path;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(full);
            if (!sprite) throw new InvalidOperationException("Football sprite is missing or not imported as Sprite: " + full);
            return sprite;
        }

        static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = (anchorMin + anchorMax) * .5f;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static GameObject Panel(Transform parent, string name, Sprite sprite, Vector2 min, Vector2 max,
            Vector2 position, Vector2 size, Color color)
        {
            var rect = Rect(parent, name, min, max, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return rect.gameObject;
        }

        static Image CreateImage(Transform parent, string name, Sprite sprite, Color color,
            Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            var rect = Rect(parent, name, min, max, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        static Button Button(Transform parent, string name, string label, Sprite sprite, Color color,
            TMP_FontAsset font, float fontSize, Vector2 min, Vector2 max, Vector2 position, Vector2 pivot)
        {
            var rect = Rect(parent, name, min, max, position, Vector2.zero);
            rect.pivot = pivot;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = Image.Type.Sliced;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var text = Text(rect, "Label", label, font, fontSize, Ink, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one);
            text.raycastTarget = false;
            return button;
        }

        static TMP_Text Text(Transform parent, string name, string value, TMP_FontAsset font,
            float fontSize, Color color, TextAlignmentOptions alignment, Vector2 min, Vector2 max)
        {
            var rect = Rect(parent, name, min, max, Vector2.zero, Vector2.zero);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            Style(text, font, fontSize, color, alignment);
            text.text = value;
            text.enableWordWrapping = true;
            return text;
        }

        static void Style(TMP_Text text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
        {
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(18f, size - 8f);
            text.fontSizeMax = size;
            text.raycastTarget = false;
        }

        static TMP_Text FindText(Transform root, string name) => root.Find(name).GetComponent<TMP_Text>();
    }
}
#endif
