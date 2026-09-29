using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KMA.UI.Kit
{
    /// Shared press response: while held, the face lightens and the target shrinks; after release
    /// both ease back over PressRestoreSeconds. A non-interactable Selectable on the same object
    /// fades to DisabledAlpha and ignores presses. Owns no input.
    public sealed class KitPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] Graphic face;
        [SerializeField] RectTransform target;
        [SerializeField] Color restColor = Color.white;
        [SerializeField] Selectable selectable;

        CanvasGroup group;
        bool pressed;
        float restoreRemaining;

        public bool IsPressed => pressed;
        public float Scale => target == null ? 1f : target.localScale.x;
        public Color RestColor => restColor;

        public void Configure(Graphic faceGraphic, RectTransform scaleTarget)
        {
            face = faceGraphic;
            target = scaleTarget;
            selectable = GetComponent<Selectable>();
            if (face != null)
                restColor = face.color;
        }

        public void SetRestColor(Color color)
        {
            restColor = color;
            if (!pressed && restoreRemaining <= 0f && face != null)
                face.color = color;
        }

        public void OnPointerDown(PointerEventData eventData) => Press();
        public void OnPointerUp(PointerEventData eventData) => Release();

        public void Press()
        {
            if (selectable != null && !selectable.interactable)
                return;
            pressed = true;
            restoreRemaining = 0f;
            if (face != null)
                face.color = MinigameUiTheme.Lighten(restColor, MinigameUiTheme.PressLighten);
            SetScale(MinigameUiTheme.PressScale);
        }

        public void Release()
        {
            if (!pressed)
                return;
            pressed = false;
            restoreRemaining = MinigameUiTheme.PressRestoreSeconds;
        }

        public void Tick(float deltaTime)
        {
            ApplyInteractable();
            if (pressed || restoreRemaining <= 0f)
                return;

            restoreRemaining = Mathf.Max(0f, restoreRemaining - deltaTime);
            float t = 1f - restoreRemaining / MinigameUiTheme.PressRestoreSeconds;
            if (face != null)
                face.color = Color.Lerp(MinigameUiTheme.Lighten(restColor, MinigameUiTheme.PressLighten), restColor, t);
            SetScale(Mathf.Lerp(MinigameUiTheme.PressScale, 1f, t));
        }

        void Update() => Tick(Time.unscaledDeltaTime);

        void OnDisable()
        {
            pressed = false;
            restoreRemaining = 0f;
            if (face != null)
                face.color = restColor;
            SetScale(1f);
        }

        void ApplyInteractable()
        {
            if (selectable == null)
                return;
            if (group == null)
                group = UiKit.GetOrAdd<CanvasGroup>(gameObject);
            group.alpha = selectable.interactable ? 1f : MinigameUiTheme.DisabledAlpha;
            if (!selectable.interactable && pressed)
            {
                pressed = false;
                restoreRemaining = 0f;
                if (face != null)
                    face.color = restColor;
                SetScale(1f);
            }
        }

        void SetScale(float scale)
        {
            if (target != null)
                target.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
