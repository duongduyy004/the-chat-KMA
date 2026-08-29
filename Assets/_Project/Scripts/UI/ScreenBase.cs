using UnityEngine;

namespace KMA.Gameplay.UI
{
    public abstract class ScreenBase : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] UITheme theme;

        public UITheme Theme => theme;
        public bool IsVisible { get; private set; }

        protected virtual void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        public virtual void Show()
        {
            IsVisible = true;
            SetVisible(true);
        }

        public virtual void Hide()
        {
            IsVisible = false;
            SetVisible(false);
        }

        void SetVisible(bool visible)
        {
            if (canvasGroup == null)
                return;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
