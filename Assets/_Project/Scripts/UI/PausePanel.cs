using System;
using KMA.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class PausePanel : MonoBehaviour
    {
        const int MenuSortingOrder = 100;

        [SerializeField] Button pauseButton;
        [SerializeField] GameObject menuRoot;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button exitButton;
        Transform menuCard;

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

            menuRoot = new GameObject("PauseMenu", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Canvas), typeof(GraphicRaycaster));
            menuRoot.transform.SetParent(parent, false);
            var menuRect = (RectTransform)menuRoot.transform;
            menuRect.anchorMin = Vector2.zero;
            menuRect.anchorMax = Vector2.one;
            menuRect.offsetMin = Vector2.zero;
            menuRect.offsetMax = Vector2.zero;
            var menuCanvas = menuRoot.GetComponent<Canvas>();
            menuCanvas.overrideSorting = true;
            menuCanvas.sortingOrder = MenuSortingOrder;
            menuRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, .7f);

            var card = new GameObject("PauseCard", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Outline));
            card.transform.SetParent(menuRoot.transform, false);
            menuCard = card.transform;
            var cardRect = (RectTransform)card.transform;
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(.5f, .5f);
            cardRect.sizeDelta = new Vector2(560f, 440f);
            card.GetComponent<Image>().color = new Color32(255, 249, 231, 255);
            Outline cardOutline = card.GetComponent<Outline>();
            cardOutline.effectColor = new Color32(8, 35, 61, 255);
            cardOutline.effectDistance = new Vector2(5f, -5f);
            CreateHeading(card.transform);

            resumeButton = CreateMenuButton("ResumeButton", "TIẾP TỤC", 55f,
                new Color32(255, 202, 58, 255));
            restartButton = CreateMenuButton("RestartButton", "CHƠI LẠI", -30f,
                new Color32(25, 130, 196, 255));
            exitButton = CreateMenuButton("ExitButton", "VỀ CHỌN MÔN", -115f,
                new Color32(255, 89, 94, 255));
        }

        void CreateHeading(Transform parent)
        {
            var heading = new GameObject("Heading", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            heading.transform.SetParent(parent, false);
            var rect = (RectTransform)heading.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(480f, 70f);
            rect.anchoredPosition = new Vector2(0f, 155f);
            Text text = heading.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "TẠM DỪNG";
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color32(8, 35, 61, 255);
        }

        Button CreateMenuButton(string buttonName, string label, float y, Color color)
        {
            var buttonRoot = new GameObject(buttonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonRoot.transform.SetParent(menuCard ?? menuRoot.transform, false);
            var rect = (RectTransform)buttonRoot.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(400f, 66f);
            rect.anchoredPosition = new Vector2(0f, y);
            buttonRoot.GetComponent<Image>().color = color;
            Outline outline = buttonRoot.AddComponent<Outline>();
            outline.effectColor = new Color32(8, 35, 61, 255);
            outline.effectDistance = new Vector2(3f, -3f);

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
            text.fontSize = 21;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color32(8, 35, 61, 255);
            return buttonRoot.GetComponent<Button>();
        }

        void SetMenuVisible(bool visible)
        {
            if (menuRoot != null)
            {
                if (visible)
                    menuRoot.transform.SetAsLastSibling();
                menuRoot.SetActive(visible);
            }
        }
    }
}
