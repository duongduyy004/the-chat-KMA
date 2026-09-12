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
        [SerializeField] TMP_Text loadingHintText;
        [SerializeField, Min(0f)] float minimumIntroSeconds = 1.5f;
        [SerializeField, Min(0)] int minimumIntroFrames = 30;

        SceneRouter router;
        float introStartedAt;
        int introStartedFrame;
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
            EnsureFestivalLoadingDetails();
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
            value = Mathf.Clamp01(value);
            if (loadingBar != null)
                loadingBar.value = value;
            if (progressText != null)
                progressText.text = $"{Mathf.RoundToInt(value * 100f)}%";
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
            while (!Application.isEditor && !UnityEngine.Rendering.SplashScreen.isFinished)
                yield return null;
            introStartedAt = Time.realtimeSinceStartup;
            introStartedFrame = Time.frameCount;
            // Scene activation can stall the player for seconds inside a single frame, so a
            // purely wall-clock hold expires before Android ever presents the splash. Require
            // presented frames as well: the intro is a presentation, not a timer.
            while (loadCompleted &&
                   (Time.realtimeSinceStartup - introStartedAt < minimumIntroSeconds ||
                    Time.frameCount - introStartedFrame < minimumIntroFrames))
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

        void EnsureFestivalLoadingDetails()
        {
            TMP_FontAsset font = statusText != null ? statusText.font : null;
            if (font == null)
                return;
            progressText = ResolveOrCreateText("ProgressPercent", "0%", font,
                new Vector2(.79f, .16f), new Vector2(.91f, .23f), 25f,
                new Color32(255, 202, 58, 255), TextAlignmentOptions.Center);
            ResolveOrCreatePanel("LoadingHintPlate",
                new Vector2(.20f, .235f), new Vector2(.80f, .31f),
                new Color32(8, 35, 61, 224));
            loadingHintText = ResolveOrCreateText("LoadingHint",
                "ĐANG KHỞI ĐỘNG NGÀY HỘI THỂ THAO", font,
                new Vector2(.22f, .245f), new Vector2(.78f, .30f), 24f,
                new Color32(255, 249, 231, 255), TextAlignmentOptions.Center);
            Shadow hintShadow = loadingHintText.GetComponent<Shadow>() ??
                loadingHintText.gameObject.AddComponent<Shadow>();
            hintShadow.effectColor = new Color(0f, 0f, 0f, .8f);
            hintShadow.effectDistance = new Vector2(2f, -2f);
        }

        TMP_Text ResolveOrCreateText(string objectName, string value, TMP_FontAsset font,
            Vector2 anchorMin, Vector2 anchorMax, float size, Color color,
            TextAlignmentOptions alignment)
        {
            Transform existing = transform.Find(objectName);
            TextMeshProUGUI text = existing == null ? null : existing.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                var root = new GameObject(objectName, typeof(RectTransform));
                root.SetActive(false);
                root.transform.SetParent(transform, false);
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                text = root.AddComponent<TextMeshProUGUI>();
            }
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.gameObject.SetActive(true);
            return text;
        }

        RectTransform ResolveOrCreatePanel(string objectName, Vector2 anchorMin,
            Vector2 anchorMax, Color color)
        {
            Transform existing = transform.Find(objectName);
            RectTransform rect = existing as RectTransform;
            if (rect == null)
            {
                var root = new GameObject(objectName, typeof(RectTransform), typeof(Image));
                root.transform.SetParent(transform, false);
                rect = root.GetComponent<RectTransform>();
            }
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }
    }
}
