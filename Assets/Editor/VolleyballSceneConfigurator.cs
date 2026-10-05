#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using KMA.Gameplay.UI;
using KMA.Gameplay.Volleyball;
using KMA.UI.Kit;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
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
        public const string OpponentCharacter = "FemaleAdventurer";
        // Character poses are 1.28 units tall; this matches the court scale the BVA2 athletes were tuned for.
        public const float AthleteScale = 1.8f;
        // Marker centre, in world units above the feet; its lowest point clears the tallest (jump) pose.
        const float MarkerWorldHeight = 2.4f;
        const string EnvironmentDir = "Assets/_Project/Art/Environments/Volleyball";
        const string PixelPath = EnvironmentDir + "/Pixel.png";
        const string HudRootName = "S2_HUD_Minigame";

        // Tuned by eye in the visual QA task; see the plan's Task 13.
        const float HorizonWorldY = 4.97f;
        const int HudSortingOrder = 500;
        // The net is a flat-colour band, drawn the same way as the Sky/Sand quads.
        const float NetWidth = .5f;
        static readonly Color SkyColor = new Color32(91, 200, 224, 255);
        static readonly Color SandColor = new Color32(236, 194, 150, 255);
        static readonly Color CourtColor = new Color32(246, 214, 172, 255);
        static readonly Color NetColor = new Color(.95f, .95f, .95f, .9f);
        static readonly Color ShadowTint = new Color(1f, 1f, 1f, .8f);
        static readonly Color ContactTint = new Color(1f, .9f, .2f, .85f);
        static readonly Color AimTint = new Color(1f, .25f, .2f, .85f);
        static readonly Color BallInk = new Color32(10, 40, 61, 255);

        readonly struct TextureSpec
        {
            public readonly string Path;
            public readonly float PixelsPerUnit;

            public TextureSpec(string path, float pixelsPerUnit)
            {
                Path = path;
                PixelsPerUnit = pixelsPerUnit;
            }
        }

        // World size 0.6 x 0.6 (ball) and about 0.77 x 0.34 (shadow) - the sizes the court was tuned for.
        const string BallPath = EnvironmentDir + "/Ball.png";
        const string ShadowPath = EnvironmentDir + "/Shadow.png";
        const int BallPixels = 128;
        const float BallPixelsPerUnit = BallPixels / .6f;
        const int ShadowWidth = 128, ShadowHeight = 56;
        const float ShadowPixelsPerUnit = ShadowWidth / .77f;
        const float AttackLineMetres = 3f;
        static readonly Vector2 Centre = new Vector2(.5f, .5f);

        static readonly TextureSpec[] Textures =
        {
            new TextureSpec(BallPath, BallPixelsPerUnit),
            new TextureSpec(ShadowPath, ShadowPixelsPerUnit),
            new TextureSpec(PixelPath, 4f)
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
            Directory.CreateDirectory(EnvironmentDir);
            EnsureGeneratedTexture(PixelPath, 4, 4, (u, v) => Color.white);
            EnsureGeneratedTexture(BallPath, BallPixels, BallPixels, BallColor);
            EnsureGeneratedTexture(ShadowPath, ShadowWidth, ShadowHeight, ShadowColor);
            foreach (TextureSpec spec in Textures)
                ConfigureTexture(spec);
            CharacterArt.ImportAll();
        }

        static readonly Color BallCream = new Color32(252, 246, 230, 255);
        static readonly Color BallSeam = new Color32(226, 150, 40, 255);

        // A flat volleyball: cream panels, three curved amber seams and an ink outline. u, v in [-1, 1].
        static Color BallColor(float u, float v)
        {
            float r = Mathf.Sqrt(u * u + v * v);
            if (r > 1f)
                return Color.clear;
            if (r > .88f)
                return BallInk;
            for (int k = 0; k < 3; k++)
            {
                float angle = (90f + k * 120f) * Mathf.Deg2Rad;
                float centreU = Mathf.Cos(angle) * 1.25f, centreV = Mathf.Sin(angle) * 1.25f;
                float distance = Mathf.Sqrt((u - centreU) * (u - centreU) + (v - centreV) * (v - centreV));
                if (Mathf.Abs(distance - 1f) < .06f)
                    return BallSeam;
            }

            return BallCream;
        }

        // A flat mid-grey ellipse; the scene tints it for the shadow and the two markers.
        static Color ShadowColor(float u, float v) =>
            u * u + v * v <= 1f ? new Color32(150, 150, 150, 255) : Color.clear;

        /// <summary>Writes a supersampled flat-colour PNG once; existing files are left alone.</summary>
        static void EnsureGeneratedTexture(string path, int width, int height, Func<float, float, Color> shade)
        {
            if (File.Exists(path))
                return;

            const int samples = 3;
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Color sum = Color.clear;
                for (int sy = 0; sy < samples; sy++)
                for (int sx = 0; sx < samples; sx++)
                {
                    float u = ((x + (sx + .5f) / samples) / width) * 2f - 1f;
                    float v = ((y + (sy + .5f) / samples) / height) * 2f - 1f;
                    Color c = shade(u, v);
                    sum += new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
                }

                sum /= samples * samples;
                pixels[y * width + x] = sum.a > 0f ? new Color(sum.r / sum.a, sum.g / sum.a, sum.b / sum.a, sum.a) : Color.clear;
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
        }

        static void ConfigureTexture(TextureSpec spec)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(spec.Path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = spec.PixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = Centre;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }


        static Sprite Single(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path) ??
            throw new InvalidOperationException($"[KMA] {path} did not import as a sprite.");

        static void BuildWorld()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Sprite pixel = Single(PixelPath);
            Sprite shadowSprite = Single(ShadowPath);

            Quad("Sky", pixel, SkyColor, new Vector3(0f, HorizonWorldY + 10f, 0f), new Vector2(60f, 20f));
            Quad("Sand", pixel, SandColor, new Vector3(0f, HorizonWorldY - 20f, 0f), new Vector2(60f, 40f));
            // CourtSpace.ToWorld scales the y (court-width) axis by PixelsPerMetreY /
            // BackgroundPixelsPerUnit (not 1:1), so the court's on-screen height isn't simply
            // HalfWidth * 2 world units - measure it via ToWorld so the band reaches both sidelines.
            float netWorldHeight = CourtSpace.ToWorld(new Vector2(0f, CourtSpace.HalfWidth), 0f).y -
                                    CourtSpace.ToWorld(new Vector2(0f, -CourtSpace.HalfWidth), 0f).y;
            Quad("Net", pixel, NetColor, CourtSpace.ToWorld(Vector2.zero, 0f),
                new Vector2(NetWidth, netWorldHeight), VolleyAthleteView.NetSortingOrder);
            float courtX = CourtSpace.ToWorld(new Vector2(CourtSpace.HalfLength, 0f), 0f).x;
            float courtY = CourtSpace.ToWorld(new Vector2(0f, CourtSpace.HalfWidth), 0f).y;
            Quad("Court", pixel, CourtColor, Vector3.zero, new Vector2(courtX * 2f, courtY * 2f), -20);
            float attackX = CourtSpace.ToWorld(new Vector2(AttackLineMetres, 0f), 0f).x;
            for (int side = -1; side <= 1; side += 2)
            {
                Quad(side < 0 ? "NearAttackLine" : "FarAttackLine", pixel, Color.white,
                    new Vector3(side * attackX, 0f, 0f), new Vector2(.09f, courtY * 2f), -9);
                Quad(side < 0 ? "NearBaseline" : "FarBaseline", pixel, Color.white,
                    new Vector3(side * courtX, 0f, 0f), new Vector2(.09f, courtY * 2f), -9);
                Quad(side < 0 ? "NearSideline" : "FarSideline", pixel, Color.white,
                    new Vector3(0f, side * courtY, 0f), new Vector2(courtX * 2f, .09f), -9);
            }

            VolleyAthleteView player = Athlete("Player", false, CharacterArt.Hero);
            VolleyAthleteView opponent = Athlete("Opponent", true, OpponentCharacter);
            AddAthleteMarker(player.transform, pixel, "Player", MinigameUiTheme.Player);
            AddAthleteMarker(opponent.transform, pixel, "Enemy", MinigameUiTheme.Energy);

            SpriteRenderer ball = Renderer("Ball", Single(BallPath), Vector3.zero, VolleyBallView.BallSortingOrder);
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

        static VolleyAthleteView Athlete(string name, bool mirror, string character)
        {
            Sprite[] Poses(params string[] poses) => CharacterArt.Frames(character, poses);
            Sprite[] idle = Poses("idle");
            SpriteRenderer body = Renderer(name, idle[0], Vector3.zero, 0);
            body.transform.localScale = Vector3.one * AthleteScale;
            var book = body.gameObject.AddComponent<SpriteFlipbook>();
            book.Configure(body, idle, true, 12f);
            var view = body.gameObject.AddComponent<VolleyAthleteView>();
            view.Configure(body, book, mirror, idle,
                Poses("run0", "run1", "run2", "run1"),
                Poses("duck", "hold"),
                Poses("jump", "attack1"),
                Poses("jump", "cheer1"),
                Poses("fall", "slide"));
            return view;
        }

        static void AddAthleteMarker(Transform athlete, Sprite pixel, string name, Color accent)
        {
            SpriteRenderer edge = Renderer(name + "MarkerEdge", pixel, Vector3.zero, 290);
            edge.transform.SetParent(athlete, false);
            edge.transform.localPosition = new Vector3(0f, MarkerWorldHeight / AthleteScale, 0f);
            edge.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            edge.transform.localScale = new Vector3(.43f, .43f, 1f) / AthleteScale;
            edge.color = MinigameUiTheme.Surface;
            SpriteRenderer centre = Renderer(name + "Marker", pixel, Vector3.zero, 291);
            centre.transform.SetParent(athlete, false);
            centre.transform.localPosition = edge.transform.localPosition;
            centre.transform.localRotation = edge.transform.localRotation;
            centre.transform.localScale = new Vector3(.31f, .31f, 1f) / AthleteScale;
            centre.color = accent;
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

        static void Quad(string name, Sprite pixel, Color color, Vector3 position, Vector2 size, int order = -30)
        {
            SpriteRenderer renderer = Renderer(name, pixel, position, order);
            renderer.color = color;
            renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        static void AddControlsAndHud()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject hudRoot = scene.GetRootGameObjects().Single(go => go.name == HudRootName);
            hudRoot.GetComponent<Canvas>().sortingOrder = HudSortingOrder;
            var parent = (RectTransform)(hudRoot.transform.Find("SafeAreaRoot") ?? hudRoot.transform);
            // The generic HUD belongs to other sports. Its timer, progress and status fields do
            // not represent volleyball, so hide only its prefab children in this scene.
            foreach (Transform child in parent)
                child.gameObject.SetActive(false);
            Camera.main.orthographicSize = 5.45f;

            RectTransform controls = UiRect("VolleyballControls", parent, Vector2.zero, Vector2.one);
            controls.SetAsFirstSibling();

            RectTransform area = UiRect("JoystickArea", controls, Vector2.zero, new Vector2(.38f, .52f));
            area.gameObject.AddComponent<Image>().color = Color.clear;
            JoystickHandle stick = UiKit.Joystick(area);
            var joystick = area.gameObject.AddComponent<VirtualJoystick>();
            joystick.Configure(area, stick.Base.rectTransform, stick.Knob.rectTransform, 88f, new Vector2(-135f, -140f));

            RectTransform buttonRect = UiRect("ActionButton", controls, Vector2.one, Vector2.one);
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(1f, 0f);
            buttonRect.sizeDelta = new Vector2(310f, 310f);
            buttonRect.anchoredPosition = new Vector2(-18f, 18f);
            buttonRect.gameObject.AddComponent<Image>().color = Color.clear;
            var button = buttonRect.gameObject.AddComponent<ActionButton>();
            UiKit.RoundButton(buttonRect, "ĐÁNH");

            Vector2 centre = new Vector2(.5f, .5f);
            Image scoreboard = UiKit.Panel(controls, "VolleyballScoreboard");
            UiKit.Place(scoreboard.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -12f),
                new Vector2(650f, 84f));
            TMP_Text playerTitle = UiKit.Label(scoreboard.transform, "PlayerTitle", "PLAYER", MinigameUiTheme.Body,
                MinigameUiTheme.Player);
            UiKit.Place(playerTitle.rectTransform, new Vector2(0f, .5f), centre, new Vector2(136f, 0f), new Vector2(225f, 65f));
            TMP_Text score = UiKit.Label(scoreboard.transform, "Score", VolleyballHud.ScoreText(0, 0),
                MinigameUiTheme.Title, MinigameUiTheme.TextPrimary);
            UiKit.Place(score.rectTransform, centre, centre, Vector2.zero, new Vector2(215f, 74f));
            TMP_Text enemyTitle = UiKit.Label(scoreboard.transform, "EnemyTitle", "ENEMY", MinigameUiTheme.Body,
                MinigameUiTheme.Energy);
            UiKit.Place(enemyTitle.rectTransform, new Vector2(1f, .5f), centre, new Vector2(-136f, 0f), new Vector2(225f, 65f));

            TMP_Text feedback = UiKit.Label(controls, "Feedback", string.Empty, MinigameUiTheme.Headline,
                MinigameUiTheme.Accent, TextAlignmentOptions.Center, outline: true);
            UiKit.Place(feedback.rectTransform, new Vector2(.5f, .75f), centre, Vector2.zero, new Vector2(680f, 100f));
            ChipHandle hint = UiKit.Chip(controls, "HintBanner", VolleyballHud.HintText);
            hint.Label.name = "Hint";
            UiKit.Place(hint.Background.rectTransform, new Vector2(.5f, .045f), centre, Vector2.zero, new Vector2(860f, 66f));
            var hud = controls.gameObject.AddComponent<VolleyballHud>();
            hud.Configure(score, feedback, hint.Label);
            hud.ConfigureHintBackdrop(hint.Background.gameObject);

            var pause = Object.FindFirstObjectByType<PausePanel>();
            if (pause)
            {
                pause.transform.SetParent(parent, false);
                UiKit.Place((RectTransform)pause.transform, Vector2.one, Vector2.one, new Vector2(-22f, -18f),
                    Vector2.one * MinigameUiTheme.ButtonHeight);
            }

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
