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
        SceneRouter boundRouter;

        public static SceneTransitionOverlay Instance => instance;
        public bool IsVisible { get; private set; }

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
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

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
            boundRouter = router;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void Bind(SceneRouter router)
        {
            router.SceneLoadStarted += Show;
            router.SceneLoadCompleted += Hide;
        }

        public void Unbind(SceneRouter router)
        {
            router.SceneLoadStarted -= Show;
            router.SceneLoadCompleted -= Hide;
        }

        public void Show()
        {
            IsVisible = true;
            panel.SetActive(true);
        }

        public void Hide()
        {
            IsVisible = false;
            panel.SetActive(false);
        }

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
            canvas.sortingOrder = 1000;
            panel.AddComponent<CanvasScaler>();

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

            panel.SetActive(false);
        }
    }
}
