using KMA.Gameplay.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class SceneTransitionOverlay : MonoBehaviour
    {
        const float SpinnerDegreesPerSecond = 240f;

        static SceneTransitionOverlay instance;

        GameObject panel;
        RectTransform spinner;
        CanvasGroup canvasGroup;
        Slider loadingBar;
        SceneRouter boundRouter;
        bool initialized;

        public static SceneTransitionOverlay Instance => instance;
        public bool IsVisible { get; private set; }
        public bool IsBlockingInput => canvasGroup != null && canvasGroup.blocksRaycasts;
        public float Progress => loadingBar != null ? loadingBar.value : 0f;

        public static SceneTransitionOverlay EnsurePersistentInstance()
        {
            if (instance != null)
                return instance;

            var existing = FindFirstObjectByType<SceneTransitionOverlay>();
            if (existing != null)
                return existing;

            return new GameObject(nameof(SceneTransitionOverlay)).AddComponent<SceneTransitionOverlay>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Application.isPlaying)
                EnsurePersistentInstance();
        }

        void Awake() => Initialize();

        // Unity's batch EditMode test runner does not reliably dispatch Awake() for
        // components added at runtime within a single synchronous [Test]. Tests call
        // this directly instead of depending on Awake firing.
        public void InitializeForTest() => Initialize();

        void Initialize()
        {
            if (initialized)
                return;
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            initialized = true;
            instance = this;
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);
            Build();

            TryBindToRouter();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (boundRouter != null)
                Unbind(boundRouter);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryBindToRouter();

        void TryBindToRouter()
        {
            if (boundRouter != null)
                return;

            var router = SceneRouter.Instance;
            if (router == null)
                return;

            Bind(router);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void Bind(SceneRouter router)
        {
            if (router == null || boundRouter == router)
                return;
            if (boundRouter != null)
                Unbind(boundRouter);

            router.SceneLoadStarted += Show;
            router.SceneLoadProgressChanged += SetProgress;
            router.SceneLoadCompleted += Hide;
            router.SceneLoadFailed += OnLoadFailed;
            boundRouter = router;
        }

        public void Unbind(SceneRouter router)
        {
            if (router == null)
                return;
            router.SceneLoadStarted -= Show;
            router.SceneLoadProgressChanged -= SetProgress;
            router.SceneLoadCompleted -= Hide;
            router.SceneLoadFailed -= OnLoadFailed;
            if (boundRouter == router)
                boundRouter = null;
        }

        public void Show()
        {
            IsVisible = true;
            SetProgress(0f);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            panel.SetActive(true);
        }

        public void Hide()
        {
            IsVisible = false;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            panel.SetActive(false);
        }

        void SetProgress(float value)
        {
            if (loadingBar != null)
                loadingBar.value = Mathf.Clamp01(value);
        }

        void OnLoadFailed(string _) => Hide();

        void Update()
        {
            if (IsVisible && spinner != null)
                spinner.Rotate(0f, 0f, -SpinnerDegreesPerSecond * Time.unscaledDeltaTime);
        }

        void Build()
        {
            panel = new GameObject("Panel");
            panel.transform.SetParent(transform, false);

            var canvas = panel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800;
            panel.AddComponent<CanvasScaler>();
            panel.AddComponent<GraphicRaycaster>();
            canvasGroup = panel.AddComponent<CanvasGroup>();

            var backdropObject = new GameObject("Backdrop");
            backdropObject.transform.SetParent(panel.transform, false);
            var backdropRect = backdropObject.AddComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            var backdropImage = backdropObject.AddComponent<Image>();
            backdropImage.color = Color.black;
            backdropImage.raycastTarget = true;

            var spinnerObject = new GameObject("Spinner");
            spinnerObject.transform.SetParent(panel.transform, false);
            spinner = spinnerObject.AddComponent<RectTransform>();
            spinner.sizeDelta = new Vector2(64f, 64f);
            spinner.anchoredPosition = Vector2.zero;
            var spinnerImage = spinnerObject.AddComponent<Image>();
            spinnerImage.color = Color.white;

            var progressObject = new GameObject("LoadingBar", typeof(RectTransform), typeof(Image),
                typeof(Slider));
            progressObject.transform.SetParent(panel.transform, false);
            var progressRect = progressObject.GetComponent<RectTransform>();
            progressRect.sizeDelta = new Vector2(420f, 24f);
            progressRect.anchoredPosition = new Vector2(0f, -90f);
            var track = progressObject.GetComponent<Image>();
            track.color = new Color(1f, 1f, 1f, 0.3f);
            track.raycastTarget = false;

            var fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaObject.transform.SetParent(progressObject.transform, false);
            var fillArea = fillAreaObject.GetComponent<RectTransform>();
            fillArea.anchorMin = Vector2.zero;
            fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = new Vector2(4f, 4f);
            fillArea.offsetMax = new Vector2(-4f, -4f);

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(fillArea, false);
            var fill = fillObject.GetComponent<RectTransform>();
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            var fillImage = fillObject.GetComponent<Image>();
            fillImage.color = new Color32(255, 202, 58, 255);
            fillImage.raycastTarget = false;

            loadingBar = progressObject.GetComponent<Slider>();
            loadingBar.minValue = 0f;
            loadingBar.maxValue = 1f;
            loadingBar.interactable = false;
            loadingBar.transition = Selectable.Transition.None;
            loadingBar.fillRect = fill;
            loadingBar.targetGraphic = track;

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            panel.SetActive(false);
        }
    }
}
