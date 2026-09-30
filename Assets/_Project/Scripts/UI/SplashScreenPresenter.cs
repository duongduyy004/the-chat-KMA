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
        [SerializeField] TMP_Text progressText;
        [SerializeField, Min(0f)] float minimumIntroSeconds = 1.5f;
        [SerializeField, Min(0)] int minimumIntroFrames = 30;

        SplashPresentationView presentation;
        SceneRouter router;
        float targetProgress;
        float displayedProgress;
        float introStartedAt;
        int introStartedFrame;
        bool loadCompleted;
        bool loadFailed;
        bool finishScheduled;

        public bool IsVisible { get; private set; }
        public bool IsBlockingInput => canvasGroup != null && canvasGroup.blocksRaycasts;
        // Keep the reported router progress available while only the presentation is smoothed.
        public float Progress => targetProgress;
        public float DisplayedProgress => displayedProgress;

        void Awake()
        {
            canvasGroup = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>();
            loadingBar = loadingBar != null ? loadingBar : GetComponentInChildren<Slider>(true);
            statusText = statusText != null ? statusText : GetComponentInChildren<TMP_Text>(true);
            presentation = SplashPresentationView.Build(transform);
            if (presentation != null)
            {
                loadingBar = presentation.LoadingBar;
                statusText = presentation.Status;
                progressText = presentation.Percent;
            }
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
            targetProgress = displayedProgress = 0f;
            if (loadingBar != null) loadingBar.value = 0f;
            presentation?.SetDisplayedProgress(0f);
            if (progressText != null) progressText.text = VietText.Fix("0%");
            SetStatus("Đang chuẩn bị...");
            // Bootstrap input is no longer needed once the automatic route starts.
            // Disable it before Menu activates so there is only one active EventSystem.
            DisableBootstrapEventSystem();
        }

        void OnProgressChanged(float value)
        {
            targetProgress = Mathf.Clamp01(value);
            if (targetProgress >= 0.99f)
                DisableBootstrapEventSystem();
        }

        void Update()
        {
            // Avoid consuming the whole animation in a single scene-activation stall.
            float delta = Mathf.Min(Time.unscaledDeltaTime, UITheme.Shared.Splash.maxAnimationDelta);
            float factor = 1f - Mathf.Exp(-delta / UITheme.Shared.Splash.progressSmoothSeconds);
            displayedProgress = Mathf.Lerp(displayedProgress, targetProgress, factor);
            if (Mathf.Abs(displayedProgress - targetProgress) < .001f)
                displayedProgress = targetProgress;
            if (loadingBar != null) loadingBar.value = displayedProgress;
            presentation?.SetDisplayedProgress(displayedProgress);
            if (progressText != null)
            {
                // Do not round to 100% until the displayed fill has actually completed.
                int percent = displayedProgress >= 1f ? 100 : Mathf.FloorToInt(displayedProgress * 100f);
                progressText.text = VietText.Fix($"{percent}%");
            }
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
            while (!Application.isEditor && !UnityEngine.Rendering.SplashScreen.isFinished)
                yield return null;
            introStartedAt = Time.realtimeSinceStartup;
            introStartedFrame = Time.frameCount;
            // Scene activation can stall the player for seconds inside a single frame, so a
            // purely wall-clock hold expires before Android ever presents the splash. Require
            // presented frames as well: the intro is a presentation, not a timer.
            while (loadCompleted &&
                   (Time.realtimeSinceStartup - introStartedAt < minimumIntroSeconds ||
                    Time.frameCount - introStartedFrame < minimumIntroFrames ||
                    displayedProgress < 1f))
                yield return null;

            if (loadCompleted && !loadFailed)
            {
                float elapsed = 0f;
                float duration = UITheme.Shared.Splash.exitDuration;
                while (elapsed < duration && loadCompleted && !loadFailed)
                {
                    elapsed += Mathf.Min(Time.unscaledDeltaTime, UITheme.Shared.Splash.maxAnimationDelta);
                    if (canvasGroup != null)
                        canvasGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    yield return null;
                }
                if (loadCompleted && !loadFailed) Hide();
            }
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
                statusText.text = VietText.Fix(value);
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
