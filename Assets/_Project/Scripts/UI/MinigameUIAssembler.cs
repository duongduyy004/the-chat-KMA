#if UNITY_EDITOR
using System;
using System.IO;
using KMA.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public static class MinigameUIAssembler
    {
        const string CameraPrefabPath = "Assets/_Project/Prefabs/Gameplay/GameCamera.prefab";
        const string HudPrefabPath = "Assets/_Project/Prefabs/UI/HUD_Minigame.prefab";
        const string PhasePrefabPath = "Assets/_Project/Prefabs/UI/PhaseOverlay.prefab";
        const string ResultPrefabPath = "Assets/_Project/Prefabs/UI/ResultPanel.prefab";
        const string ThemePath = "Assets/_Project/Settings/UI/UITheme.asset";

        static readonly string[] ScenePaths =
        {
            "Assets/_Project/Scenes/MG_Sprint.unity",
            "Assets/_Project/Scenes/MG_Endurance.unity",
            "Assets/_Project/Scenes/MG_Boss.unity",
            "Assets/_Project/Scenes/Punishment.unity",
            "Assets/_Project/Scenes/Map.unity",
            "Assets/_Project/Scenes/GameOver.unity",
            "Assets/_Project/Scenes/MG_Volleyball.unity",
            "Assets/_Project/Scenes/MG_Basketball.unity",
            "Assets/_Project/Scenes/MG_PingPong.unity",
            "Assets/_Project/Scenes/MG_Badminton.unity",
            "Assets/_Project/Scenes/MG_Football.unity"
        };

        [MenuItem("KMA/S2/Assemble Task 5 Presentation")]
        public static void AssembleTask5Presentation()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CameraPrefabPath));
            var cameraPrefab = EnsureCameraPrefab();

            foreach (var scenePath in ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                AssembleScene(scene, cameraPrefab);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static GameObject EnsureCameraPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);
            GameObject cameraRoot;
            if (prefab == null)
            {
                cameraRoot = new GameObject("GameCamera");
                ConfigureCamera(cameraRoot);
                PrefabUtility.SaveAsPrefabAsset(cameraRoot, CameraPrefabPath);
                UnityEngine.Object.DestroyImmediate(cameraRoot);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);
            }

            cameraRoot = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (cameraRoot == null)
                throw new InvalidOperationException($"Could not instantiate {CameraPrefabPath}.");

            ConfigureCamera(cameraRoot);
            PrefabUtility.SaveAsPrefabAsset(cameraRoot, CameraPrefabPath);
            UnityEngine.Object.DestroyImmediate(cameraRoot);
            return AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);
        }

        static void AssembleScene(Scene scene, GameObject cameraPrefab)
        {
            var cameraObject = EnsureSceneCamera(scene, cameraPrefab);
            var camera = cameraObject.GetComponent<Camera>();
            var minigame = FindInScene<MinigameBase>(scene);
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);

            var hudRoot = EnsurePrefabRoot(scene, HudPrefabPath, "S2_HUD_Minigame", null);
            ConfigureCanvas(hudRoot, camera);
            ConfigureHud(hudRoot, minigame, theme);

            var canvasTransform = hudRoot.transform;
            var phaseRoot = EnsurePrefabRoot(scene, PhasePrefabPath, "S2_PhaseOverlay", canvasTransform);
            StretchToParent(phaseRoot);
            ConfigurePhaseOverlay(phaseRoot, minigame);

            var resultRoot = EnsurePrefabRoot(scene, ResultPrefabPath, "S2_ResultPanel", canvasTransform);
            StretchToParent(resultRoot);
            resultRoot.SetActive(false);
            ConfigureResultPanel(resultRoot, theme);

            if (scene.name.StartsWith("MG_", StringComparison.Ordinal) || scene.name == "Punishment")
                EnsurePausePanel(scene, canvasTransform);

            EnsureEventSystem(scene);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static void EnsurePausePanel(Scene scene, Transform canvasTransform)
        {
            var pause = FindInScene<PausePanel>(scene);
            if (pause == null)
            {
                var pauseObject = new GameObject("PausePanel", typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(pauseObject, scene);
                pauseObject.transform.SetParent(canvasTransform, false);
                pause = pauseObject.AddComponent<PausePanel>();
            }

            var button = pause.GetComponent<Button>();
            if (button == null)
            {
                button = pause.gameObject.AddComponent<Image>().gameObject.AddComponent<Button>();
                button.onClick.AddListener(pause.Open);
                var rect = button.transform as RectTransform;
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-32f, -32f);
                rect.sizeDelta = new Vector2(150f, 68f);
                var labelObject = new GameObject("Label");
                labelObject.transform.SetParent(button.transform, false);
                var labelRect = labelObject.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                var label = labelObject.AddComponent<Text>();
                label.text = "PAUSE";
                label.alignment = TextAnchor.MiddleCenter;
                label.fontSize = 24;
                label.color = Color.white;
            }
        }

        static GameObject EnsureSceneCamera(Scene scene, GameObject cameraPrefab)
        {
            var cameraObject = FindRoot(scene, "GameCamera");
            if (cameraObject == null)
            {
                cameraObject = PrefabUtility.InstantiatePrefab(cameraPrefab, scene) as GameObject;
                if (cameraObject == null)
                    throw new InvalidOperationException($"Could not instantiate {CameraPrefabPath} in {scene.name}.");
                cameraObject.name = "GameCamera";
            }

            ConfigureCamera(cameraObject);
            foreach (var camera in FindComponentsInScene<Camera>(scene))
            {
                if (camera.gameObject != cameraObject)
                    UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }

            return cameraObject;
        }

        static void ConfigureCamera(GameObject cameraObject)
        {
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.transform.rotation = Quaternion.identity;
            cameraObject.transform.localScale = Vector3.one;

            var camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
                camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0x19, 0x82, 0xC4, 0xFF);
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;

            EnsureUrpCameraData(cameraObject);
        }

        static void EnsureUrpCameraData(GameObject cameraObject)
        {
            const string typeName =
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime";
            var cameraDataType = Type.GetType(typeName);
            if (cameraDataType == null)
                throw new InvalidOperationException("Install com.unity.render-pipelines.universal so GameCamera can serialize UniversalAdditionalCameraData.");
            if (cameraObject.GetComponent(cameraDataType) == null)
                cameraObject.AddComponent(cameraDataType);
        }

        static GameObject EnsurePrefabRoot(Scene scene, string prefabPath, string instanceName, Transform parent)
        {
            var root = FindInSceneByName(scene, instanceName);
            if (root == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                    throw new InvalidOperationException($"Missing prefab: {prefabPath}");
                root = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (root == null)
                    throw new InvalidOperationException($"Could not instantiate {prefabPath} in {scene.name}.");
                root.name = instanceName;
            }

            if (parent != null)
                root.transform.SetParent(parent, false);
            return root;
        }

        static void ConfigureCanvas(GameObject root, Camera camera)
        {
            var canvas = root.GetComponent<Canvas>();
            if (canvas == null)
                canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;

            var scaler = root.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            if (root.GetComponent<GraphicRaycaster>() == null)
                root.AddComponent<GraphicRaycaster>();
            if (root.GetComponentInChildren<SafeAreaFitter>(true) == null)
                root.AddComponent<SafeAreaFitter>();
        }

        static void ConfigureHud(GameObject root, MinigameBase minigame, UITheme theme)
        {
            var hud = root.GetComponentInChildren<MinigameHUD>(true);
            if (hud == null)
                return;
            SetObjectReference(hud, "minigameSource", minigame);
            SetObjectReference(hud, "theme", theme);
        }

        static void ConfigurePhaseOverlay(GameObject root, MinigameBase minigame)
        {
            var overlay = root.GetComponentInChildren<PhaseOverlay>(true);
            if (overlay != null)
                SetObjectReference(overlay, "minigameSource", minigame);
        }

        static void ConfigureResultPanel(GameObject root, UITheme theme)
        {
            var panel = root.GetComponentInChildren<ResultPanel>(true);
            if (panel != null)
                SetObjectReference(panel, "theme", theme);
        }

        static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                return;
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static void StretchToParent(GameObject root)
        {
            if (root.transform is not RectTransform rectTransform)
                return;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        static void EnsureEventSystem(Scene scene)
        {
            if (FindInScene<EventSystem>(scene) != null)
                return;

            var eventSystemObject = new GameObject("EventSystem");
            SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
            }
            return null;
        }

        static T FindInScene<T>(Scene scene) where T : UnityEngine.Object
        {
            foreach (var component in FindComponentsInScene<T>(scene))
                return component;
            return null;
        }

        static T[] FindComponentsInScene<T>(Scene scene) where T : UnityEngine.Object
        {
            return Array.FindAll(UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                component => SceneOf(component) == scene);
        }

        static GameObject FindInSceneByName(Scene scene, string name)
        {
            foreach (var transform in FindComponentsInScene<Transform>(scene))
            {
                if (transform.name == name)
                    return transform.gameObject;
            }
            return null;
        }

        static Scene SceneOf(UnityEngine.Object component)
        {
            return component switch
            {
                Component sceneComponent => sceneComponent.gameObject.scene,
                GameObject gameObject => gameObject.scene,
                _ => default
            };
        }
    }
}
#endif
