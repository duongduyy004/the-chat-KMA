#if UNITY_EDITOR
using System.Collections.Generic;
using KMA.Gameplay;
using KMA.Gameplay.Shell;
using KMA.Gameplay.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KMA.EditorTools
{
    public static class S5SceneConfigurator
    {
        static readonly string[] PlaceholderScenes =
        {
            "MG_Volleyball", "MG_Basketball", "MG_PingPong", "MG_Badminton", "MG_Football"
        };

        [MenuItem("KMA/S5/Generate Placeholder Scenes and Build Routes")]
        public static void Generate()
        {
            foreach (string sceneName in PlaceholderScenes)
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("GameplayCamera");
                camera.tag = "MainCamera";
                camera.AddComponent<Camera>().orthographic = true;
                var placeholder = new GameObject("Placeholder_" + sceneName);
                placeholder.AddComponent<PlaceholderMinigameController>();
                new GameObject("GameplayPresentation").AddComponent<KMA.Gameplay.Core.GameplayPresentation>();
                EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/" + sceneName + ".unity");
            }

            var settings = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (string sceneName in PlaceholderScenes)
            {
                string path = "Assets/_Project/Scenes/" + sceneName + ".unity";
                if (!settings.Exists(entry => entry.path == path))
                    settings.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = settings.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            KMA.Gameplay.UI.MinigameUIAssembler.AssembleTask5Presentation();
            GenerateShellScenes();
        }

        static void GenerateShellScenes()
        {
            GenerateShell("Menu", true, false, false);
            GenerateShell("Map", false, true, false);
            GenerateShell("GameOver", false, false, true);
        }

        static void GenerateShell(string sceneName, bool menu, bool map, bool gameOver)
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + sceneName + ".unity", OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<S5ShellSceneController>() == null)
            {
                var canvasObject = new GameObject("S5ShellCanvas");
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
                var canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 1f;
                canvasObject.AddComponent<GraphicRaycaster>();

                var safeArea = new GameObject("SafeArea");
                safeArea.transform.SetParent(canvasObject.transform, false);
                var safeRect = safeArea.AddComponent<RectTransform>();
                safeRect.anchorMin = Vector2.zero;
                safeRect.anchorMax = Vector2.one;
                safeRect.offsetMin = Vector2.zero;
                safeRect.offsetMax = Vector2.zero;
                safeArea.AddComponent<SafeAreaFitter>();
                safeArea.AddComponent<S5ShellSceneController>();

                if (menu)
                {
                    var screen = safeArea.AddComponent<MainMenuScreen>();
                    AddButton(safeArea.transform, "PLAY", new Vector2(0f, 180f), screen.Play);
                    AddButton(safeArea.transform, "CONTINUE", new Vector2(0f, 60f), screen.Continue);
                    AddButton(safeArea.transform, "NEW GAME", new Vector2(0f, -60f), screen.NewGame);
                    AddButton(safeArea.transform, "SETTINGS", new Vector2(0f, -180f), screen.OpenSettings);
                    AddButton(safeArea.transform, "QUIT", new Vector2(0f, -300f), screen.Quit);
                    var settings = new GameObject("SettingsScreen");
                    settings.transform.SetParent(safeArea.transform, false);
                    settings.AddComponent<SettingsScreen>();
                    var calibrate = new GameObject("CalibrateScreen");
                    calibrate.transform.SetParent(safeArea.transform, false);
                    calibrate.AddComponent<CalibrateScreen>();
                }
                else if (map)
                {
                    var screen = safeArea.AddComponent<MapScreen>();
                    var subjects = (SubjectId[])System.Enum.GetValues(typeof(SubjectId));
                    for (var i = 0; i < subjects.Length; i++)
                    {
                        var subject = subjects[i];
                        AddButton(safeArea.transform, subject.ToString(),
                            new Vector2(-420f + (i % 4) * 280f, 120f - (i / 4) * 180f),
                            () => screen.SelectSubject(subject));
                    }
                    AddButton(safeArea.transform, "BOSS", new Vector2(560f, -240f), screen.SelectBoss);
                }
                else if (gameOver)
                {
                    var screen = safeArea.AddComponent<GameOverScreen>();
                    AddButton(safeArea.transform, "RETRY", new Vector2(0f, 120f), screen.Retry);
                    AddButton(safeArea.transform, "NEW GAME", new Vector2(0f, 0f), screen.NewGame);
                    AddButton(safeArea.transform, "MAIN MENU", new Vector2(0f, -120f), screen.ReturnToMenu);
                }
                EnsureEventSystem(scene);
                EditorSceneManager.MarkSceneDirty(scene);
            }
            EditorSceneManager.SaveScene(scene);
        }

        static Button AddButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var root = new GameObject(label + "Button");
            root.transform.SetParent(parent, false);
            var rect = root.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(360f, 84f);
            rect.anchoredPosition = position;
            var image = root.AddComponent<Image>();
            image.color = new Color32(255, 89, 94, 255);
            var button = root.AddComponent<Button>();
            button.onClick.AddListener(action);
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(root.transform, false);
            var labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var text = labelObject.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 28;
            text.color = Color.white;
            return button;
        }

        static void EnsureEventSystem(Scene scene)
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;
            var eventSystem = new GameObject("EventSystem");
            SceneManager.MoveGameObjectToScene(eventSystem, scene);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }
}
#endif
