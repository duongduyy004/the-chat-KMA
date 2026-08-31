using System;
using KMA.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class PausePanel : MonoBehaviour
    {
        [SerializeField] Button pauseButton;
        [SerializeField] GameObject menuRoot;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button exitButton;

        public event Action RestartRequested;
        public event Action ExitToMapRequested;
        public bool IsOpen { get; private set; }
        float previousTimeScale = 1f;

        void Awake()
        {
            pauseButton ??= GetComponent<Button>();
            EnsureMenu();
            WireButtons();
            SetMenuVisible(false);
        }

        public void Open()
        {
            if (IsOpen)
                return;
            previousTimeScale = Time.timeScale;
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (behaviour is IPauseAware pauseAware)
                    pauseAware.SetPaused(true);
            Time.timeScale = 0f;
            IsOpen = true;
            SetMenuVisible(true);
        }

        public void Resume()
        {
            if (!IsOpen)
                return;
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (behaviour is IPauseAware pauseAware)
                    pauseAware.SetPaused(false);
            Time.timeScale = previousTimeScale;
            IsOpen = false;
            SetMenuVisible(false);
        }

        public void Restart()
        {
            Resume();
            RestartRequested?.Invoke();
        }

        public void ExitToMap()
        {
            Resume();
            ExitToMapRequested?.Invoke();
        }

        void WireButtons()
        {
            if (pauseButton != null)
                pauseButton.onClick.AddListener(Open);
            if (resumeButton != null)
                resumeButton.onClick.AddListener(Resume);
            if (restartButton != null)
                restartButton.onClick.AddListener(Restart);
            if (exitButton != null)
                exitButton.onClick.AddListener(ExitToMap);
        }

        void EnsureMenu()
        {
            if (menuRoot != null)
                return;

            var parent = transform.parent;
            if (parent == null)
                return;

            menuRoot = new GameObject("PauseMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            menuRoot.transform.SetParent(parent, false);
            var menuRect = (RectTransform)menuRoot.transform;
            menuRect.anchorMin = Vector2.zero;
            menuRect.anchorMax = Vector2.one;
            menuRect.offsetMin = Vector2.zero;
            menuRect.offsetMax = Vector2.zero;
            menuRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, .7f);

            resumeButton = CreateMenuButton("ResumeButton", "RESUME", 72f);
            restartButton = CreateMenuButton("RestartButton", "RESTART", 0f);
            exitButton = CreateMenuButton("ExitButton", "EXIT TO MAP", -72f);
        }

        Button CreateMenuButton(string buttonName, string label, float y)
        {
            var buttonRoot = new GameObject(buttonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonRoot.transform.SetParent(menuRoot.transform, false);
            var rect = (RectTransform)buttonRoot.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(300f, 56f);
            rect.anchoredPosition = new Vector2(0f, y);

            var labelRoot = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelRoot.transform.SetParent(buttonRoot.transform, false);
            var labelRect = (RectTransform)labelRoot.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var text = labelRoot.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return buttonRoot.GetComponent<Button>();
        }

        void SetMenuVisible(bool visible)
        {
            if (menuRoot != null)
                menuRoot.SetActive(visible);
        }
    }
}
