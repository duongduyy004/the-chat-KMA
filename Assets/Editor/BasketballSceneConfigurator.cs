#if UNITY_EDITOR
using System;
using System.IO;
using KMA.Gameplay;
using KMA.Gameplay.UI;
using KMA.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KMA.EditorTools
{
    // Authors the MG_Basketball production scene from scratch every time it runs, so the scene
    // stays reproducible from source rather than from hand-edited YAML (see S9's Volleyball
    // lesson: an unauthored scene is unverifiable and un-diffable).
    public static class BasketballSceneConfigurator
    {
        const string ScenePath = "Assets/_Project/Scenes/MG_Basketball.unity";
        const string BallPrefabPath = "Assets/_Project/Prefabs/Gameplay/BallPresentation.prefab";
        const string FlightProfilePath = "Assets/_Project/ScriptableObjects/Ball/FlightProfile_Basketball.asset";
        const string InputActionsPath = "Assets/_Project/Settings/Input/KMA.inputactions";
        const string ThemePath = "Assets/_Project/Settings/UI/UITheme.asset";
        const string PlaceholderSpritePath = "Assets/_Project/Art/Placeholder/PlaceholderSquare.png";
        const string BuiltinSpriteResource = "UI/Skin/UISprite.psd";

        static readonly Color CourtColor = new Color(.55f, .35f, .18f, 1f);
        static readonly Color PlayerColor = new Color(.15f, .55f, .95f, 1f);
        static readonly Color HoopColor = new Color(.95f, .35f, .1f, 1f);
        static readonly Color BackboardColor = new Color(.85f, .85f, .9f, 1f);
        static readonly Color FinisherColor = new Color(.15f, .8f, .4f, 1f);
        static readonly Color DefenderColor = new Color(.8f, .2f, .2f, 1f);
        static readonly Color BallColor = new Color(1f, .45f, .15f, 1f);

        [MenuItem("KMA/S10/Author Basketball Scene")]
        public static void Author()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            RemovePlaceholder();

            Sprite sprite = LoadPlaceholderSprite();

            // --- World layout (world units, ground y = 0 per FlightProfile_Basketball.groundY) ---
            AddVisual(null, "BasketballCourt", sprite, CourtColor, new Vector3(0f, -.15f, 1f), 18f, .3f, -10);
            AddVisual(null, "BasketballPlayer", sprite, PlayerColor, new Vector3(-3.5f, .6f, 0f), .9f, 1.2f, 1);

            var hand = new GameObject("BasketballPlayerHand");
            hand.transform.position = new Vector3(-3.5f, 1.2f, 0f);

            AddVisual(null, "BasketballHoop", sprite, HoopColor, new Vector3(0f, 3.05f, 0f), .9f, .1f, -1);
            AddVisual(null, "BasketballBackboard", sprite, BackboardColor, new Vector3(.6f, 3.6f, 1f), .15f, 1.2f, -2);
            SpriteRenderer finisher = AddVisual(null, "BasketballFinisher", sprite, FinisherColor, new Vector3(0f, .7f, 0f), .9f, 1.4f, 2);
            AddVisual(null, "BasketballDefender", sprite, DefenderColor, new Vector3(1.6f, .65f, 0f), .9f, 1.3f, 0);

            // --- Ball: the shared S8 presentation kit, given the renderer it lacks on its own ---
            GameObject ballGO = InstantiateBall(sprite);

            // --- Gameplay input surface (mirrors MG_Volleyball's FullScreenGameplayInput) ---
            (GameplayInputRouter router, ScreenTapArea surface) = BuildFullScreenGameplayInput();

            // --- Controller ---
            var controllerGO = new GameObject("BasketballController");
            var finisherBounds = controllerGO.AddComponent<BoxCollider2D>();
            finisherBounds.isTrigger = true;
            finisherBounds.offset = new Vector2(-1.5f, 0f);
            finisherBounds.size = new Vector2(7f, 4f);
            var controller = controllerGO.AddComponent<BasketballController>();

            var ballRig = ballGO.GetComponent<BallRig>();
            var preview = ballGO.GetComponent<TrajectoryPreview>();
            var shadow = ballGO.GetComponent<BallShadow>();

            SetObjectReference(controller, "ball", ballRig);
            SetObjectReference(controller, "inputRouter", router);
            SetObjectReference(controller, "playerHand", hand.transform);
            SetObjectReference(controller, "finisher", finisher.transform);
            SetObjectReference(controller, "finisherBounds", finisherBounds);
            SetObjectReference(controller, "trajectoryPreview", preview);
            SetObjectReference(controller, "ballShadow", shadow);

            // --- HUD ---
            BuildHud(controller);

            // --- Rebind the shared S2 presentation kit from the placeholder to the new controller ---
            var sharedHud = FindComponentInScene<MinigameHUD>(scene);
            var phaseOverlay = FindComponentInScene<PhaseOverlay>(scene);
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (sharedHud == null)
                throw new InvalidOperationException("MG_Basketball has no MinigameHUD to rebind.");
            if (phaseOverlay == null)
                throw new InvalidOperationException("MG_Basketball has no PhaseOverlay to rebind.");

            SetObjectReference(sharedHud, "minigameSource", controller);
            SetObjectReference(sharedHud, "theme", theme);
            SetObjectReference(phaseOverlay, "minigameSource", controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void RemovePlaceholder()
        {
            var placeholder = GameObject.Find("Placeholder_MG_Basketball");
            if (placeholder != null)
                UnityEngine.Object.DestroyImmediate(placeholder);
        }

        static GameObject InstantiateBall(Sprite sprite)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BallPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Missing prefab: {BallPrefabPath}");

            var ballGO = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (ballGO == null)
                throw new InvalidOperationException($"Could not instantiate {BallPrefabPath}.");

            ballGO.name = "BallPresentation";
            ballGO.SetActive(true);
            ballGO.transform.position = new Vector3(-3.5f, 1.2f, 0f);

            // BallPresentation.prefab's root already carries Rigidbody2D + BallRig (verified by
            // reading the prefab asset); fetch rather than blindly AddComponent, which would try
            // to add a second Rigidbody2D.
            var body = ballGO.GetComponent<Rigidbody2D>();
            if (body == null)
                body = ballGO.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            var ballRig = ballGO.GetComponent<BallRig>();
            if (ballRig == null)
                ballRig = ballGO.AddComponent<BallRig>();

            var flightProfile = AssetDatabase.LoadAssetAtPath<FlightProfile>(FlightProfilePath);
            if (flightProfile == null)
                throw new InvalidOperationException($"Missing {FlightProfilePath}");
            ballRig.SetProfile(flightProfile);

            // The prefab has no renderer for the ball itself (only the shadow and preview line) -
            // add one on the scene instance, exactly as the Volleyball scene had to.
            AddVisual(ballGO, "BasketballBallVisual", sprite, BallColor, ballGO.transform.position, .5f, .5f, 3);

            Transform lineTransform = ballGO.transform.Find("TrajectoryPreviewLine");
            LineRenderer line = lineTransform != null ? lineTransform.GetComponent<LineRenderer>() : null;
            var preview = ballGO.GetComponent<TrajectoryPreview>();
            if (preview == null || line == null)
                throw new InvalidOperationException("BallPresentation.prefab is missing TrajectoryPreview or its LineRenderer child.");
            preview.Configure(ballRig, line, 16, .04f);

            Transform shadowTransform = ballGO.transform.Find("BallShadowVisual");
            SpriteRenderer shadowRenderer = shadowTransform != null ? shadowTransform.GetComponent<SpriteRenderer>() : null;
            var shadow = ballGO.GetComponent<BallShadow>();
            if (shadow == null || shadowTransform == null || shadowRenderer == null)
                throw new InvalidOperationException("BallPresentation.prefab is missing BallShadow or its shadow visual.");
            shadow.Configure(ballGO.transform, shadowTransform, shadowRenderer, 0f, 4f, .35f, 1f, .2f, .75f);

            return ballGO;
        }

        // Copied structurally from MG_Volleyball.unity's FullScreenGameplayInput: one Canvas
        // (ScreenSpaceOverlay, below the HUD canvas), a transparent full-screen raycast Image,
        // the shared router and the shared tap surface. Basketball differs in one respect: it
        // binds inputActions so a keyboard fallback exists, unlike the S9 Volleyball scene.
        static (GameplayInputRouter router, ScreenTapArea surface) BuildFullScreenGameplayInput()
        {
            var go = new GameObject("FullScreenGameplayInput", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -1;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            go.AddComponent<GraphicRaycaster>();

            var image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;

            var router = go.AddComponent<GameplayInputRouter>();
            var surface = go.AddComponent<ScreenTapArea>();
            surface.Configure(router, rect);

            var inputActions = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(InputActionsPath);
            if (inputActions == null)
                throw new InvalidOperationException($"Missing input actions asset: {InputActionsPath}");
            SetObjectReference(router, "inputActions", inputActions);
            SetStringValue(router, "gameplayActionMapName", "Basketball");

            return (router, surface);
        }

        static void BuildHud(BasketballController controller)
        {
            var canvasGO = new GameObject("BasketballHudCanvas", typeof(RectTransform));
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            canvasGO.AddComponent<GraphicRaycaster>();

            var safeArea = CreateRect("SafeArea", canvasGO.transform);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            TMPro.TMP_Text scoreLabel = CreateLabel(safeArea, "BasketballScoreLabel",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -40f), new Vector2(460f, 60f), TMPro.TextAlignmentOptions.TopLeft);
            TMPro.TMP_Text attemptsLabel = CreateLabel(safeArea, "BasketballAttemptsLabel",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -104f), new Vector2(460f, 60f), TMPro.TextAlignmentOptions.TopLeft);
            TMPro.TMP_Text comboLabel = CreateLabel(safeArea, "BasketballComboLabel",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -168f), new Vector2(460f, 60f), TMPro.TextAlignmentOptions.TopLeft);
            TMPro.TMP_Text judgeLabel = CreateLabel(safeArea, "BasketballJudgeLabel",
                new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(.5f, 1f),
                new Vector2(0f, -40f), new Vector2(600f, 70f), TMPro.TextAlignmentOptions.Top);
            TMPro.TMP_Text chargeLabel = CreateLabel(safeArea, "BasketballChargeLabel",
                new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(.5f, 0f),
                new Vector2(0f, 80f), new Vector2(400f, 60f), TMPro.TextAlignmentOptions.Bottom);

            var apexZone = CreateRect("BasketballApexZone", safeArea);
            Anchor(apexZone, new Vector2(.5f, .62f), new Vector2(.5f, .62f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(240f, 240f));
            var apexZoneImage = apexZone.gameObject.AddComponent<Image>();
            apexZoneImage.color = new Color(1f, .85f, .2f, .35f);
            apexZoneImage.raycastTarget = false;
            apexZoneImage.enabled = false;

            var apexRing = CreateRect("BasketballApexRing", safeArea);
            Anchor(apexRing, new Vector2(.5f, .62f), new Vector2(.5f, .62f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(180f, 180f));
            var apexRingImage = apexRing.gameObject.AddComponent<Image>();
            apexRingImage.color = new Color(1f, 1f, 1f, .9f);
            apexRingImage.type = Image.Type.Filled;
            apexRingImage.fillMethod = Image.FillMethod.Radial360;
            apexRingImage.fillAmount = 1f;
            apexRingImage.raycastTarget = false;

            var chargeTrack = CreateRect("BasketballChargeTrack", safeArea);
            Anchor(chargeTrack, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-70f, 240f), new Vector2(60f, 420f));
            var chargeTrackImage = chargeTrack.gameObject.AddComponent<Image>();
            chargeTrackImage.color = new Color(0f, 0f, 0f, .35f);
            chargeTrackImage.raycastTarget = false;

            var chargeFillRect = CreateRect("BasketballChargeFill", chargeTrack);
            Stretch(chargeFillRect);
            var chargeFillImage = chargeFillRect.gameObject.AddComponent<Image>();
            chargeFillImage.color = new Color(.2f, .9f, .5f, .9f);
            chargeFillImage.type = Image.Type.Filled;
            chargeFillImage.fillMethod = Image.FillMethod.Vertical;
            chargeFillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
            chargeFillImage.fillAmount = 0f;
            chargeFillImage.raycastTarget = false;

            var chargeTargetBand = CreateRect("BasketballChargeTargetBand", chargeTrack);
            Anchor(chargeTargetBand, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            var chargeTargetBandImage = chargeTargetBand.gameObject.AddComponent<Image>();
            chargeTargetBandImage.color = new Color(1f, 1f, 1f, .55f);
            chargeTargetBandImage.raycastTarget = false;

            var hudGO = new GameObject("BasketballHud");
            hudGO.transform.SetParent(canvasGO.transform, false);
            var hud = hudGO.AddComponent<BasketballHud>();

            SetObjectReference(hud, "controller", controller);
            SetObjectReference(hud, "scoreLabel", scoreLabel);
            SetObjectReference(hud, "attemptsLabel", attemptsLabel);
            SetObjectReference(hud, "judgeLabel", judgeLabel);
            SetObjectReference(hud, "comboLabel", comboLabel);
            SetObjectReference(hud, "chargeLabel", chargeLabel);
            SetObjectReference(hud, "apexRingFill", apexRingImage);
            SetObjectReference(hud, "apexZoneGlow", apexZoneImage);
            SetObjectReference(hud, "chargeFill", chargeFillImage);
            SetObjectReference(hud, "chargeTargetBand", chargeTargetBand);
            SetObjectReference(hud, "chargeTrack", chargeTrack);
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static TMPro.TMP_Text CreateLabel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, TMPro.TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            Anchor(rect, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            var label = rect.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            label.text = string.Empty;
            label.fontSize = 40f;
            label.color = Color.white;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        // The built-in placeholder sprite matches the Volleyball scene (guid
        // 0000000000000000f000000000000000, fileIDs 10905/10913). If batch mode ever returns null
        // for it, fall back to a small committed placeholder sprite sized to the same 0.16 x 0.16
        // world-unit bounds, so AddVisual's localScale = worldSize / .16f stays valid either way.
        static Sprite LoadPlaceholderSprite()
        {
            var builtin = AssetDatabase.GetBuiltinExtraResource<Sprite>(BuiltinSpriteResource);
            if (builtin != null)
                return builtin;

            EnsurePlaceholderSpriteAsset();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            if (sprite == null)
                throw new InvalidOperationException($"Could not load or generate a placeholder sprite at {PlaceholderSpritePath}.");
            return sprite;
        }

        static void EnsurePlaceholderSpriteAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath) != null)
                return;

            string directory = Path.GetDirectoryName(PlaceholderSpritePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(PlaceholderSpritePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(PlaceholderSpritePath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(PlaceholderSpritePath);
            if (importer == null)
                throw new InvalidOperationException($"Could not import generated placeholder sprite at {PlaceholderSpritePath}.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f; // size(16) / 100 = 0.16 world units, matching UISprite.
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        // The built-in UISprite is 0.16 x 0.16 world units, so a world size becomes a local scale.
        static SpriteRenderer AddVisual(GameObject parent, string name, Sprite sprite, Color color,
            Vector3 position, float worldWidth, float worldHeight, int sortingOrder)
        {
            var visual = new GameObject(name);
            visual.transform.SetParent(parent == null ? null : parent.transform, false);
            visual.transform.position = position;
            visual.transform.localScale = new Vector3(worldWidth / .16f, worldHeight / .16f, 1f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"{target.GetType().Name} has no serialized field '{propertyName}'.");
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetStringValue(UnityEngine.Object target, string propertyName, string value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"{target.GetType().Name} has no serialized field '{propertyName}'.");
            property.stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static T FindComponentInScene<T>(Scene scene) where T : UnityEngine.Object
        {
            foreach (var component in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var sceneOf = component switch
                {
                    Component c => c.gameObject.scene,
                    GameObject g => g.scene,
                    _ => default
                };
                if (sceneOf == scene)
                    return component;
            }
            return null;
        }
    }
}
#endif
