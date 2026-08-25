using UnityEngine;
using UnityEngine.SceneManagement;

namespace KMA.Gameplay.Core
{
    public sealed class GameplayPresentation : MonoBehaviour
    {
        string statusText;
        string titleText;
        string controlsText;
        GUIStyle titleStyle;
        GUIStyle statusStyle;
        GUIStyle controlsStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InstallSceneBootstrap()
        {
            SceneManager.sceneLoaded -= EnsureScenePresentation;
            SceneManager.sceneLoaded += EnsureScenePresentation;
            EnsureScenePresentation(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void EnsureScenePresentation(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "" || FindFirstObjectByType<GameplayPresentation>() != null)
                return;

            new GameObject("GameplayPresentation").AddComponent<GameplayPresentation>();
        }

        void Awake()
        {
            gameObject.name = "GameplayPresentation";
            EnsureCamera();
            titleText = SceneTitle(SceneManager.GetActiveScene().name);
            controlsText = Controls(SceneManager.GetActiveScene().name);
        }

        void Update()
        {
            var router = FindFirstObjectByType<SceneRouter>();
            var minigame = FindFirstObjectByType<MinigameBase>();
            var phase = minigame == null ? "Route" : minigame.PresentationPhase.ToString();
            var session = router == null
                ? "Session: waiting for route"
                : $"Lives: {router.Session.Lives}   Boss: {(router.Session.BossUnlocked ? "Unlocked" : "Locked")}";
            statusText = $"Phase: {phase}\n{session}";
        }

        void OnGUI()
        {
            if (titleStyle == null)
            {
                titleStyle = CreateStyle(46, FontStyle.Bold);
                statusStyle = CreateStyle(28, FontStyle.Normal);
                controlsStyle = CreateStyle(24, FontStyle.Normal);
            }

            var previousColor = GUI.color;
            GUI.color = new Color(.025f, .04f, .08f, .96f);
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);
            GUI.color = Color.white;

            GUI.Label(new Rect(0f, Screen.height * .08f, Screen.width, Screen.height * .16f),
                titleText, titleStyle);
            GUI.Label(new Rect(0f, Screen.height * .38f, Screen.width, Screen.height * .24f),
                statusText ?? "Phase: Tutorial", statusStyle);
            GUI.Label(new Rect(0f, Screen.height * .76f, Screen.width, Screen.height * .16f),
                controlsText, controlsStyle);
            GUI.color = previousColor;
        }

        static Camera EnsureCamera()
        {
            var camera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                var cameraObject = new GameObject("GameplayCamera");
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .04f, .08f, 1f);
            return camera;
        }

        static GUIStyle CreateStyle(int fontSize, FontStyle fontStyle)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
        }

        static string SceneTitle(string sceneName) => sceneName switch
        {
            "MG_Sprint" => "KMA — Sprint",
            "MG_Endurance" => "KMA — Endurance",
            "MG_Boss" => "KMA — Final Boss",
            "Punishment" => "KMA — Recovery Challenge",
            "Map" => "KMA — Map",
            "GameOver" => "KMA — Game Over",
            _ => "KMA Gameplay"
        };

        static string Controls(string sceneName) => sceneName switch
        {
            "MG_Sprint" => "Sprint: Left / Right arrows",
            "MG_Endurance" => "Endurance: T tap · H hold · Up / Down swipe",
            "MG_Boss" => "Boss: Space tap · H hold · Left / Right alternate",
            "Punishment" => "Recovery: Space tap · H hold · Left / Right alternate",
            "Map" => "Progression route",
            "GameOver" => "Run complete",
            _ => "KMA Gameplay Prototype"
        };
    }
}
