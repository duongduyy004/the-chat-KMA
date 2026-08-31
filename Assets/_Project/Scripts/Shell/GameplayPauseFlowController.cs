using UnityEngine;
using UnityEngine.SceneManagement;
using KMA.Gameplay.Core;
using KMA.Gameplay.UI;

namespace KMA.Gameplay.Shell
{
    public sealed class GameplayPauseFlowController : MonoBehaviour
    {
        PausePanel pause;
        SceneRouter router;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= Ensure;
            SceneManager.sceneLoaded += Ensure;
            Ensure(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void Ensure(Scene scene, LoadSceneMode mode)
        {
            if (FindFirstObjectByType<PausePanel>() == null ||
                FindFirstObjectByType<GameplayPauseFlowController>() != null)
                return;
            new GameObject(nameof(GameplayPauseFlowController)).AddComponent<GameplayPauseFlowController>();
        }

        void Awake()
        {
            pause = FindFirstObjectByType<PausePanel>();
            router = SceneRouter.Instance;
            if (pause != null)
            {
                pause.RestartRequested += Restart;
                pause.ExitToMapRequested += ExitToMap;
            }
        }

        void OnDestroy()
        {
            if (pause != null)
            {
                pause.RestartRequested -= Restart;
                pause.ExitToMapRequested -= ExitToMap;
            }
        }

        void Restart() => (router ?? SceneRouter.Instance)?.RestartActiveSubject();
        void ExitToMap() => (router ?? SceneRouter.Instance)?.ExitActiveSubjectToMap();
    }
}
