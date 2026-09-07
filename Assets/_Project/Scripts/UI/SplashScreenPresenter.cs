using System.Collections;
using KMA.Gameplay.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    [DefaultExecutionOrder(-500)]
    public sealed class SplashScreenPresenter : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] Slider loadingBar;
        [SerializeField] TMP_Text statusText;
        [SerializeField, Min(0f)] float minimumIntroSeconds = 1.5f;

        SceneRouter router;
        float introStartedAt;
        bool loadCompleted;
        bool loadFailed;
        bool finishScheduled;

        public bool IsVisible { get; private set; }
        public bool IsBlockingInput => canvasGroup != null && canvasGroup.blocksRaycasts;
        public float Progress => loadingBar != null ? loadingBar.value : 0f;

        void Awake()
        {
            canvasGroup = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>();
            loadingBar = loadingBar != null ? loadingBar : GetComponentInChildren<Slider>(true);
            statusText = statusText != null ? statusText : GetComponentInChildren<TMP_Text>(true);
            introStartedAt = Time.realtimeSinceStartup;
            Show();

            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);

            if (SceneRouter.Instance != null)
                Bind(SceneRouter.Instance);
            else
                StartCoroutine(BindWhenRouterIsReady());
        }

        void OnDestroy()
        {
            if (router != null)
                Unbind(router);
        }

        public void Bind(SceneRouter sceneRouter)
        {
            if (sceneRouter == null || router == sceneRouter)
                return;
            if (router != null)
                Unbind(router);

            router = sceneRouter;
            router.SceneLoadStarted += OnLoadStarted;
            router.SceneLoadProgressChanged += OnProgressChanged;
            router.SceneLoadCompleted += OnLoadCompleted;
            router.SceneLoadFailed += OnLoadFailed;
        }

        void Unbind(SceneRouter sceneRouter)
        {
            sceneRouter.SceneLoadStarted -= OnLoadStarted;
            sceneRouter.SceneLoadProgressChanged -= OnProgressChanged;
            sceneRouter.SceneLoadCompleted -= OnLoadCompleted;
            sceneRouter.SceneLoadFailed -= OnLoadFailed;
            if (router == sceneRouter)
                router = null;
        }

        IEnumerator BindWhenRouterIsReady()
        {
            while (SceneRouter.Instance == null)
                yield return null;
            Bind(SceneRouter.Instance);
        }

        void OnLoadStarted()
        {
            loadCompleted = false;
            loadFailed = false;
            finishScheduled = false;
            Show();
            OnProgressChanged(0f);
            SetStatus("ĐANG CHUẨN BỊ...");
            // Bootstrap input is no longer needed once the automatic route starts.
            // Disable it before Menu activates so there is only one active EventSystem.
            DisableBootstrapEventSystem();
        }

        void OnProgressChanged(float value)
        {
            if (loadingBar != null)
                loadingBar.value = Mathf.Clamp01(value);
            if (value >= 0.99f)
                DisableBootstrapEventSystem();
        }

        void OnLoadCompleted()
        {
            if (loadFailed)
                return;

            loadCompleted = true;
            OnProgressChanged(1f);
            SetStatus("SẴN SÀNG!");
            DisableBootstrapEventSystem();
            if (router != null)
                Unbind(router);
            if (!finishScheduled)
                StartCoroutine(FinishAfterMinimumIntro());
        }

        void OnLoadFailed(string _)
        {
            loadFailed = true;
            loadCompleted = false;
            SetStatus("KHÔNG THỂ TẢI TRÒ CHƠI");
            ReleaseInput();
        }

        IEnumerator FinishAfterMinimumIntro()
        {
            finishScheduled = true;
            // Android can keep Unity's native splash above the game after the Menu
            // operation completes. Start our visible hold only after that layer ends.
            while (!UnityEngine.Rendering.SplashScreen.isFinished)
                yield return null;
            introStartedAt = Time.realtimeSinceStartup;
            while (loadCompleted && Time.realtimeSinceStartup - introStartedAt < minimumIntroSeconds)
                yield return null;

            if (loadCompleted && !loadFailed)
                Hide();
            finishScheduled = false;
        }

        void Show()
        {
            IsVisible = true;
            if (canvasGroup == null)
                return;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        void Hide()
        {
            IsVisible = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                ReleaseInput();
            }
            if (router != null)
                Unbind(router);
            if (Application.isPlaying)
                Destroy(gameObject);
        }

        void ReleaseInput()
        {
            if (canvasGroup == null)
                return;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        void SetStatus(string value)
        {
            if (statusText != null)
                statusText.text = value;
        }

        void DisableBootstrapEventSystem()
        {
            BaseInputModule inputModule = GetComponent<BaseInputModule>();
            if (inputModule != null)
                inputModule.enabled = false;
            EventSystem eventSystem = GetComponent<EventSystem>();
            if (eventSystem != null)
                eventSystem.enabled = false;
        }
    }
}
