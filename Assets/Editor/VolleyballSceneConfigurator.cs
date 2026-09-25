#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using KMA.Gameplay.UI;
using KMA.Gameplay.Volleyball;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace KMA.EditorTools
{
    public static class VolleyballSceneConfigurator
    {
        public const string ScenePath = "Assets/_Project/Scenes/MG_Volleyball.unity";
        const string CharacterDir = "Assets/_Project/Art/Characters/BeachVolley";
        const string EnvironmentDir = "Assets/_Project/Art/Environments/Volleyball";
        const string PixelPath = EnvironmentDir + "/Pixel.png";
        const string FontPath = "Assets/_Project/Fonts/Baloo2-ExtraBold.asset";
        const string HudRootName = "S2_HUD_Minigame";
        const string KnobSpritePath = "UI/Skin/Knob.psd";

        // Tuned by eye in the visual QA task; see the plan's Task 13.
        const float AthletePixelsPerUnit = 26f;
        const float NetPixelsPerUnit = 43f;
        const float HorizonWorldY = 4.97f;
        const int HudSortingOrder = 500;
        static readonly Color SkyColor = new Color32(91, 200, 224, 255);
        static readonly Color SandColor = new Color32(236, 194, 150, 255);
        static readonly Color OpponentTint = new Color(1f, .55f, .55f, 1f);
        static readonly Color ShadowTint = new Color(1f, 1f, 1f, .8f);
        static readonly Color ContactTint = new Color(1f, .9f, .2f, .85f);
        static readonly Color AimTint = new Color(1f, .25f, .2f, .85f);
        static readonly Color ButtonColor = new Color32(255, 152, 0, 242);

        readonly struct TextureSpec
        {
            public readonly string Path;
            public readonly int FrameWidth;
            public readonly float PixelsPerUnit;
            public readonly Vector2 Pivot;

            public TextureSpec(string path, int frameWidth, float pixelsPerUnit, Vector2 pivot)
            {
                Path = path;
                FrameWidth = frameWidth;
                PixelsPerUnit = pixelsPerUnit;
                Pivot = pivot;
            }

            public bool Sliced => FrameWidth > 0;
        }

        static readonly Vector2 Feet = new Vector2(.5f, 0f);
        static readonly Vector2 Centre = new Vector2(.5f, .5f);

        static readonly TextureSpec[] Textures =
        {
            new TextureSpec(CharacterDir + "/playerIdle.png", 32, AthletePixelsPerUnit, Feet),
            new TextureSpec(CharacterDir + "/playerRun.png", 32, AthletePixelsPerUnit, Feet),
            new TextureSpec(CharacterDir + "/playerReception.png", 32, AthletePixelsPerUnit, Feet),
            new TextureSpec(CharacterDir + "/playerBlock.png", 32, AthletePixelsPerUnit, Feet),
            new TextureSpec(CharacterDir + "/playerSmash.png", 32, AthletePixelsPerUnit, Feet),
            new TextureSpec(CharacterDir + "/playerSlide.png", 43, AthletePixelsPerUnit, Feet),
            new TextureSpec(EnvironmentDir + "/net0.png", 45, NetPixelsPerUnit, Centre),
            new TextureSpec(EnvironmentDir + "/ballRoll.png", 15, CourtSpace.BackgroundPixelsPerUnit, Centre),
            new TextureSpec(EnvironmentDir + "/beachbkgO.png", 0, CourtSpace.BackgroundPixelsPerUnit, Centre),
            new TextureSpec(EnvironmentDir + "/shadow1.png", 0, CourtSpace.BackgroundPixelsPerUnit, Centre),
            new TextureSpec(PixelPath, 0, 4f, Centre)
        };

        [MenuItem("KMA/Volleyball/Build Scene")]
        public static void BuildScene()
        {
            ImportArt();
            BuildWorld();
            MinigameUIAssembler.AssembleScenePath(ScenePath);
            AddControlsAndHud();
            EnsureInBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[KMA] MG_Volleyball built.");
        }

        public static void ImportArt()
        {
            EnsurePixelTexture();
            foreach (TextureSpec spec in Textures)
            {
                if (!File.Exists(spec.Path))
                    throw new FileNotFoundException(
                        $"[KMA] Volleyball art is missing: {spec.Path}. Extract it from BVA2.zip.", spec.Path);
            }

            foreach (TextureSpec spec in Textures)
                ConfigureTexture(spec);
        }

        static void EnsurePixelTexture()
        {
            if (File.Exists(PixelPath))
                return;

            Directory.CreateDirectory(EnvironmentDir);
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            texture.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
            texture.Apply();
            File.WriteAllBytes(PixelPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(PixelPath);
        }

        static void ConfigureTexture(TextureSpec spec)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(spec.Path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = spec.Sliced ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.spritePixelsPerUnit = spec.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = spec.Pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            if (!spec.Sliced)
                return;

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            int count = width / spec.FrameWidth;
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            SpriteRect[] existing = provider.GetSpriteRects();
            if (existing.Length == count && existing.All(r => Mathf.Approximately(r.rect.width, spec.FrameWidth)))
                return;

            string baseName = Path.GetFileNameWithoutExtension(spec.Path);
            var rects = new SpriteRect[count];
            for (int i = 0; i < count; i++)
            {
                rects[i] = new SpriteRect
                {
                    name = $"{baseName}_{i:00}",
                    rect = new Rect(i * spec.FrameWidth, 0, spec.FrameWidth, height),
                    alignment = SpriteAlignment.Custom,
                    pivot = spec.Pivot,
                    spriteID = GUID.Generate()
                };
            }

            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(
                rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
            provider.Apply();
            importer.SaveAndReimport();
        }

        static Sprite[] Frames(string path)
        {
            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
            if (frames.Length == 0)
                throw new InvalidOperationException($"[KMA] No sprites were sliced from {path}.");
            return frames;
        }

        static Sprite Single(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path) ??
            throw new InvalidOperationException($"[KMA] {path} did not import as a sprite.");

        static void BuildWorld()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Sprite pixel = Single(PixelPath);
            Sprite shadowSprite = Single(EnvironmentDir + "/shadow1.png");

            Quad("Sky", pixel, SkyColor, new Vector3(0f, HorizonWorldY + 10f, 0f), new Vector2(60f, 20f));
            Quad("Sand", pixel, SandColor, new Vector3(0f, HorizonWorldY - 20f, 0f), new Vector2(60f, 40f));
            Renderer("Court", Single(EnvironmentDir + "/beachbkgO.png"), CourtSpace.BackgroundWorldPosition, -20);
            Renderer("Net", Frames(EnvironmentDir + "/net0.png")[0], CourtSpace.ToWorld(Vector2.zero, 0f),
                VolleyAthleteView.NetSortingOrder);

            VolleyAthleteView player = Athlete("Player", false, Color.white);
            VolleyAthleteView opponent = Athlete("Opponent", true, OpponentTint);

            Sprite[] roll = Frames(EnvironmentDir + "/ballRoll.png");
            SpriteRenderer ball = Renderer("Ball", roll[0], Vector3.zero, VolleyBallView.BallSortingOrder);
            ball.gameObject.AddComponent<SpriteFlipbook>().Configure(ball, roll, true, 14f);
            SpriteRenderer shadow = Renderer("BallShadow", shadowSprite, Vector3.zero, VolleyBallView.ShadowSortingOrder);
            shadow.color = ShadowTint;
            SpriteRenderer contact = Renderer("ContactMarker", shadowSprite, Vector3.zero, VolleyBallView.MarkerSortingOrder);
            contact.color = ContactTint;
            SpriteRenderer aim = Renderer("AimMarker", shadowSprite, Vector3.zero, VolleyBallView.MarkerSortingOrder);
            aim.color = AimTint;
            var ballView = new GameObject("BallView").AddComponent<VolleyBallView>();
            ballView.Configure(ball, shadow, contact, aim);

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            var controllerObject = new GameObject("VolleyballController");
            var input = controllerObject.AddComponent<VolleyballInputBridge>();
            var controller = controllerObject.AddComponent<VolleyballController>();
            controller.Configure(player, opponent, ballView, input, null);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static VolleyAthleteView Athlete(string name, bool mirror, Color tint)
        {
            Sprite[] idle = Frames(CharacterDir + "/playerIdle.png");
            SpriteRenderer body = Renderer(name, idle[0], Vector3.zero, 0);
            body.color = tint;
            var book = body.gameObject.AddComponent<SpriteFlipbook>();
            book.Configure(body, idle, true, 12f);
            var view = body.gameObject.AddComponent<VolleyAthleteView>();
            view.Configure(body, book, mirror, idle,
                Frames(CharacterDir + "/playerRun.png"),
                Frames(CharacterDir + "/playerReception.png"),
                Frames(CharacterDir + "/playerSmash.png"),
                Frames(CharacterDir + "/playerBlock.png"),
                Frames(CharacterDir + "/playerSlide.png"));
            return view;
        }

        static SpriteRenderer Renderer(string name, Sprite sprite, Vector3 position, int order)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        static void Quad(string name, Sprite pixel, Color color, Vector3 position, Vector2 size)
        {
            SpriteRenderer renderer = Renderer(name, pixel, position, -30);
            renderer.color = color;
            renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        static void AddControlsAndHud()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject hudRoot = scene.GetRootGameObjects().Single(go => go.name == HudRootName);
            hudRoot.GetComponent<Canvas>().sortingOrder = HudSortingOrder;
            var parent = (RectTransform)(hudRoot.transform.Find("SafeAreaRoot") ?? hudRoot.transform);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(KnobSpritePath);

            RectTransform controls = UiRect("VolleyballControls", parent, Vector2.zero, Vector2.one);
            controls.SetAsFirstSibling();

            RectTransform area = UiRect("JoystickArea", controls, Vector2.zero, new Vector2(.4f, 1f));
            area.gameObject.AddComponent<Image>().color = Color.clear;
            RectTransform stickBase = Circle("JoystickBase", area, knobSprite, 240f, new Color(1f, 1f, 1f, .3f));
            RectTransform knob = Circle("JoystickKnob", area, knobSprite, 120f, new Color(1f, 1f, 1f, .7f));
            var joystick = area.gameObject.AddComponent<VirtualJoystick>();
            joystick.Configure(area, stickBase, knob, 100f, new Vector2(0f, -220f));

            RectTransform buttonRect = Circle("ActionButton", controls, knobSprite, 280f, ButtonColor);
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(1f, 0f);
            buttonRect.anchoredPosition = new Vector2(-140f, 140f);
            buttonRect.GetComponent<Image>().raycastTarget = true;
            var button = buttonRect.gameObject.AddComponent<ActionButton>();
            Label("Label", buttonRect, font, "ĐÁNH", 64f, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TMP_Text score = Label("Score", controls, font, VolleyballHud.ScoreText(0, 0), 64f,
                new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -40f), new Vector2(800f, 100f));
            TMP_Text feedback = Label("Feedback", controls, font, string.Empty, 80f,
                new Vector2(.5f, .65f), new Vector2(.5f, .65f), Vector2.zero, new Vector2(600f, 120f));
            TMP_Text hint = Label("Hint", controls, font, VolleyballHud.HintText, 40f,
                new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 40f), new Vector2(1400f, 80f));
            var hud = controls.gameObject.AddComponent<VolleyballHud>();
            hud.Configure(score, feedback, hint);

            var controller = Object.FindFirstObjectByType<VolleyballController>();
            controller.Input.Configure(joystick, button);
            controller.Configure(controller.PlayerView, controller.OpponentView, controller.BallView, controller.Input, hud);

            foreach (Object dirty in new Object[] { joystick, button, hud, controller, controller.Input, hudRoot })
                EditorUtility.SetDirty(dirty);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static RectTransform UiRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        static RectTransform Circle(string name, Transform parent, Sprite sprite, float size, Color color)
        {
            RectTransform rect = UiRect(name, parent, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            rect.sizeDelta = new Vector2(size, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, string text, float size,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 dimensions)
        {
            RectTransform rect = UiRect(name, parent, anchorMin, anchorMax);
            rect.pivot = new Vector2(.5f, anchorMin.y >= 1f ? 1f : anchorMin.y <= 0f && anchorMax.y <= 0f ? 0f : .5f);
            rect.anchoredPosition = position;
            if (dimensions != Vector2.zero)
                rect.sizeDelta = dimensions;
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font)
                label.font = font;
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        static void EnsureInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath))
                return;

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
