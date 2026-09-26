#if UNITY_EDITOR
using System;
using System.IO;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KMA.EditorTools
{
    public static class DemoMenuConfigurator
    {
        const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        const string MenuScenePath = "Assets/_Project/Scenes/Menu.unity";
        const string LogoPath = "Assets/_Project/Art/Brand/GameLogo.png";
        const string IllustrationPath = "Assets/_Project/Art/UI/HomeIllustration.png";
        const string HeadingFontPath = "Assets/_Project/Fonts/Baloo2-ExtraBold.asset";
        const string BodyFontPath = "Assets/_Project/Fonts/Nunito-Bold.asset";

        static readonly Color Navy = new Color32(9, 35, 64, 255);
        static readonly Color Blue = new Color32(25, 130, 196, 255);
        static readonly Color Coral = new Color32(255, 89, 94, 255);
        static readonly Color Gold = new Color32(255, 202, 58, 255);

        [MenuItem("KMA/Demo/Configure Splash and Home")]
        public static void Configure()
        {
            AssetDatabase.Refresh();
            Sprite logo = LoadSprite(LogoPath);
            Sprite illustration = LoadSprite(IllustrationPath);
            TMP_FontAsset headingFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(HeadingFontPath);
            TMP_FontAsset bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BodyFontPath);

            ConfigureBootstrap(logo, illustration, headingFont, bodyFont);
            ConfigureHome(logo, illustration, headingFont);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Configured Bootstrap splash and Vietnamese demo home.");
        }

        static void ConfigureBootstrap(Sprite logo, Sprite illustration, TMP_FontAsset headingFont,
            TMP_FontAsset bodyFont)
        {
            Scene scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            GameManager manager = UnityEngine.Object.FindFirstObjectByType<GameManager>(
                FindObjectsInactive.Include);
            if (manager == null)
                throw new InvalidOperationException("Bootstrap scene must contain GameManager.");
            if (UnityEngine.Object.FindFirstObjectByType<SceneRouter>(FindObjectsInactive.Include) == null)
                manager.gameObject.AddComponent<SceneRouter>();

            DestroyNamed(scene, "SplashCanvas");
            var root = new GameObject("SplashCanvas");
            SceneManager.MoveGameObjectToScene(root, scene);
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            CanvasGroup group = root.AddComponent<CanvasGroup>();
            root.AddComponent<EventSystem>();
            root.AddComponent<StandaloneInputModule>();
            SplashScreenPresenter presenter = root.AddComponent<SplashScreenPresenter>();

            Image backdrop = CreateImage(root.transform, "Backdrop", null, Stretch(), Navy);
            backdrop.raycastTarget = true;

            Image art = CreateImage(root.transform, "Illustration", illustration,
                Anchored(new Vector2(0.42f, 0f), Vector2.one, Vector2.zero, Vector2.zero), Color.white);
            art.preserveAspect = true;

            var veil = CreateImage(root.transform, "ColorVeil", null, Stretch(),
                new Color(0.02f, 0.12f, 0.22f, 0.48f));
            veil.raycastTarget = false;

            Image logoImage = CreateImage(root.transform, "Logo", logo,
                Anchored(new Vector2(0.10f, 0.58f), new Vector2(0.56f, 0.94f), Vector2.zero, Vector2.zero),
                Color.white);
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;

            CreateText(root.transform, "Title", "THỂ CHẤT KMA",
                Anchored(new Vector2(0.08f, 0.43f), new Vector2(0.58f, 0.59f), Vector2.zero, Vector2.zero),
                58f, headingFont, Gold);

            TMP_Text subtitle = CreateText(root.transform, "Subtitle", "HÀNH TRÌNH RÈN LUYỆN THỂ CHẤT",
                Anchored(new Vector2(0.08f, 0.30f), new Vector2(0.58f, 0.43f), Vector2.zero, Vector2.zero),
                32f, bodyFont, Color.white);
            subtitle.characterSpacing = 3f;

            TMP_Text status = CreateText(root.transform, "Status", "ĐANG CHUẨN BỊ...",
                Anchored(new Vector2(0.22f, 0.16f), new Vector2(0.78f, 0.23f), Vector2.zero, Vector2.zero),
                26f, headingFont, Color.white);
            Slider loadingBar = CreateLoadingBar(root.transform);

            SerializedObject serializedPresenter = new SerializedObject(presenter);
            serializedPresenter.FindProperty("canvasGroup").objectReferenceValue = group;
            serializedPresenter.FindProperty("loadingBar").objectReferenceValue = loadingBar;
            serializedPresenter.FindProperty("statusText").objectReferenceValue = status;
            serializedPresenter.FindProperty("minimumIntroSeconds").floatValue = 1.5f;
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void ConfigureHome(Sprite logo, Sprite illustration, TMP_FontAsset headingFont)
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            MainMenuScreen menu = UnityEngine.Object.FindFirstObjectByType<MainMenuScreen>(
                FindObjectsInactive.Include);
            if (canvas == null || menu == null)
                throw new InvalidOperationException("Menu scene must contain its Canvas and MainMenuScreen.");

            DestroyNamed(scene, "HomeIllustration");
            DestroyNamed(scene, "HomeTint");
            DestroyNamed(scene, "HomeLogo");
            DestroyNamed(scene, "HomeTitle");

            Image background = CreateImage(canvas.transform, "HomeIllustration", illustration, Stretch(),
                Color.white);
            background.preserveAspect = false;
            background.transform.SetAsFirstSibling();
            Image tint = CreateImage(canvas.transform, "HomeTint", null, Stretch(),
                new Color(0.015f, 0.09f, 0.16f, 0.38f));
            tint.raycastTarget = false;
            tint.transform.SetSiblingIndex(1);

            Transform safeArea = menu.transform;
            Image logoImage = CreateImage(safeArea, "HomeLogo", logo,
                Fixed(new Vector2(-500f, 390f), new Vector2(230f, 230f)), Color.white);
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;
            CreateText(safeArea, "HomeTitle", "THỂ CHẤT KMA",
                Fixed(new Vector2(-500f, 245f), new Vector2(620f, 110f)), 56f, headingFont, Gold);

            ConfigureButton(scene, "CONTINUEButton", "TIẾP TỤC", new Vector2(-500f, -10f), Blue);
            ConfigureButton(scene, "NEW GAMEButton", "CHƠI MỚI", new Vector2(-500f, -125f), Blue);
            ConfigureButton(scene, "SETTINGSButton", "CÀI ĐẶT", new Vector2(-500f, -240f), Navy);
            ConfigureButton(scene, "QUITButton", "THOÁT", new Vector2(-500f, -355f), Navy);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void ConfigureButton(Scene scene, string objectName, string label, Vector2 position,
            Color color)
        {
            GameObject buttonObject = FindNamed(scene, objectName);
            if (buttonObject == null)
                throw new InvalidOperationException($"Menu is missing required object {objectName}.");

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(420f, 88f);
            rect.anchoredPosition = position;
            Image image = buttonObject.GetComponent<Image>();
            image.color = color;
            Outline outline = buttonObject.GetComponent<Outline>();
            if (outline == null)
                outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(4f, -4f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.disabledColor = new Color(0.35f, 0.39f, 0.44f, 0.72f);
            button.colors = colors;

            Text text = buttonObject.GetComponentInChildren<Text>(true);
            if (text == null)
                throw new InvalidOperationException($"{objectName} is missing its label.");
            text.text = label;
            text.fontSize = 30;
            text.fontStyle = FontStyle.Bold;
            text.color = objectName == "NEW GAMEButton" ? Navy : Color.white;
        }

        static Slider CreateLoadingBar(Transform parent)
        {
            var root = new GameObject("LoadingBar", typeof(RectTransform), typeof(Image), typeof(Slider));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.22f, 0.10f);
            rect.anchorMax = new Vector2(0.78f, 0.145f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image track = root.GetComponent<Image>();
            track.color = new Color(1f, 1f, 1f, 0.25f);
            track.raycastTarget = false;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(5f, 5f);
            fillAreaRect.offsetMax = new Vector2(-5f, -5f);
            Image fill = CreateImage(fillArea.transform, "Fill", null, Stretch(), Gold);
            fill.raycastTarget = false;

            Slider slider = root.GetComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = track;
            return slider;
        }

        static TMP_Text CreateText(Transform parent, string name, string value, RectLayout layout,
            float size, TMP_FontAsset font, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            Apply(gameObject.GetComponent<RectTransform>(), layout);
            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.font = font;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        static Image CreateImage(Transform parent, string name, Sprite sprite, RectLayout layout,
            Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Apply(gameObject.GetComponent<RectTransform>(), layout);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        static Sprite LoadSprite(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Required demo art is missing.", path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Could not import demo art at {path}.");
            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single || !importer.alphaIsTransparency)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException($"Demo art did not import as a Sprite: {path}.");
            return sprite;
        }

        static void DestroyNamed(Scene scene, string name)
        {
            GameObject existing = FindNamed(scene, name);
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing);
        }

        static GameObject FindNamed(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform descendant in descendants)
                    if (string.Equals(descendant.name, name, StringComparison.Ordinal))
                        return descendant.gameObject;
            }
            return null;
        }

        readonly struct RectLayout
        {
            public RectLayout(Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin,
                Vector2 offsetMax, Vector2 sizeDelta, Vector2 anchoredPosition)
            {
                AnchorMin = anchorMin;
                AnchorMax = anchorMax;
                OffsetMin = offsetMin;
                OffsetMax = offsetMax;
                SizeDelta = sizeDelta;
                AnchoredPosition = anchoredPosition;
            }

            public Vector2 AnchorMin { get; }
            public Vector2 AnchorMax { get; }
            public Vector2 OffsetMin { get; }
            public Vector2 OffsetMax { get; }
            public Vector2 SizeDelta { get; }
            public Vector2 AnchoredPosition { get; }
        }

        static RectLayout Stretch() =>
            Anchored(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        static RectLayout Anchored(Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax) =>
            new RectLayout(min, max, offsetMin, offsetMax, Vector2.zero, Vector2.zero);

        static RectLayout Fixed(Vector2 position, Vector2 size) =>
            new RectLayout(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                Vector2.zero, size, position);

        static void Apply(RectTransform rect, RectLayout layout)
        {
            rect.anchorMin = layout.AnchorMin;
            rect.anchorMax = layout.AnchorMax;
            rect.offsetMin = layout.OffsetMin;
            rect.offsetMax = layout.OffsetMax;
            rect.sizeDelta = layout.SizeDelta;
            rect.anchoredPosition = layout.AnchoredPosition;
        }
    }
}
#endif
