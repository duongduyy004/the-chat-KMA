#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace KMA.Gameplay
{
    [DefaultExecutionOrder(-10000)]
    internal sealed class SprintTutorialScreenshotCheckpoint : MonoBehaviour
    {
        const string HoldKey = "KMA_PMS_HoldSprintTutorial";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InstallForRequestedCapture()
        {
            if (Application.isBatchMode || !SessionState.GetBool(HoldKey, false))
                return;

            var checkpoint = new GameObject("SprintTutorialScreenshotCheckpoint");
            DontDestroyOnLoad(checkpoint);
            checkpoint.AddComponent<SprintTutorialScreenshotCheckpoint>();
        }

        void Update()
        {
            SprintStartPresentation presenter = Object.FindFirstObjectByType<SprintStartPresentation>();
            if (presenter == null)
                return;

            presenter.enabled = false;
            enabled = false;
        }
    }
}
#endif
