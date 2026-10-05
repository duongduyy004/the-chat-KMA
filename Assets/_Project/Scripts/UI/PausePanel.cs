using System;
using KMA.Gameplay;
using KMA.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class PausePanel : MonoBehaviour
    {
        const int MenuSortingOrder = 900;

        [SerializeField] Button pauseButton;
        [SerializeField] GameObject menuRoot;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button exitButton;
        Transform menuCard;
        bool leaveOptionsVisible = true;

        public event Action RestartRequested;
        public event Action ExitToMapRequested;
        public bool IsOpen { get; private set; }
        float previousTimeScale = 1f;

        void Awake()
        {
            // StylePauseButton adds the Button itself, so style first and resolve the reference after.
            if (transform is RectTransform rect)
                UiKit.StylePauseButton(rect);
            if (pauseButton == null)
                pauseButton = GetComponent<Button>();
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
            if (!leaveOptionsVisible)
                return;
            Resume();
            RestartRequested?.Invoke();
        }

        public void ExitToMap()
        {
            if (!leaveOptionsVisible)
                return;
            Resume();
            ExitToMapRequested?.Invoke();
        }

        /// The frog jump is mandatory: its pause menu only resumes.
        public void SetLeaveOptionsVisible(bool visible)
        {
            leaveOptionsVisible = visible;
            ApplyMenuLayout();
            if (restartButton != null)
                restartButton.gameObject.SetActive(visible);
            if (exitButton != null)
                exitButton.gameObject.SetActive(visible);
        }

        // With only Tiếp tục left, shrink the card so it does not keep room for the hidden buttons.
        void ApplyMenuLayout()
        {
            if (menuCard == null) return;
            bool compact = !leaveOptionsVisible;
            ((RectTransform)menuCard).sizeDelta = new Vector2(560f, compact ? 270f : 440f);
            Transform heading = menuCard.Find("Heading");
            if (heading != null) ((RectTransform)heading).anchoredPosition = new Vector2(0f, compact ? 80f : 155f);
            if (resumeButton != null)
                ((RectTransform)resumeButton.transform).anchoredPosition = new Vector2(0f, compact ? -35f : 55f);
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
            UiKit.Stretch((RectTransform)menuRoot.transform);
            var menuCanvas = menuRoot.GetComponent<Canvas>();
            menuCanvas.overrideSorting = true;
            var parentCanvas = GetComponentInParent<Canvas>();
            menuCanvas.sortingOrder = parentCanvas
                ? Mathf.Max(MenuSortingOrder, parentCanvas.sortingOrder + 1)
                : MenuSortingOrder;
            menuRoot.GetComponent<Image>().color = MinigameUiTheme.Scrim;

            Image card = UiKit.Panel(menuRoot.transform, "PauseCard");
            card.raycastTarget = true;
            menuCard = card.transform;
            Vector2 centre = new Vector2(.5f, .5f);
            UiKit.Place(card.rectTransform, centre, centre, Vector2.zero, new Vector2(560f, 440f));

            TMP_Text heading = UiKit.Label(card.transform, "Heading", "TẠM DỪNG", MinigameUiTheme.Headline,
                MinigameUiTheme.TextPrimary);
            UiKit.Place(heading.rectTransform, centre, centre, new Vector2(0f, 155f), new Vector2(480f, 70f));

            resumeButton = CreateMenuButton("ResumeButton", "TIẾP TỤC", 55f, ButtonVariant.Primary);
            restartButton = CreateMenuButton("RestartButton", "CHƠI LẠI", -49f, ButtonVariant.Secondary);
            exitButton = CreateMenuButton("ExitButton", "VỀ CHỌN MÔN", -153f, ButtonVariant.Danger);
        }

        Button CreateMenuButton(string buttonName, string label, float y, ButtonVariant variant)
        {
            ButtonHandle handle = UiKit.Button(menuCard != null ? menuCard : menuRoot.transform, buttonName, label, variant);
            Vector2 centre = new Vector2(.5f, .5f);
            UiKit.Place((RectTransform)handle.Button.transform, centre, centre, new Vector2(0f, y),
                new Vector2(400f, MinigameUiTheme.ButtonHeight));
            return handle.Button;
        }

        void SetMenuVisible(bool visible)
        {
            if (menuRoot != null)
            {
                if (visible)
                    menuRoot.transform.SetAsLastSibling();
                menuRoot.SetActive(visible);
            }

            if (visible)
                SetLeaveOptionsVisible(leaveOptionsVisible);
        }
    }
}
