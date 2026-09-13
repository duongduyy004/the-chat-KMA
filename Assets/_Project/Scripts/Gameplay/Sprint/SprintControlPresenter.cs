using System.Collections.Generic;
using KMA.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public sealed class SprintControlPresenter : MonoBehaviour
    {
        SprintController controller;
        RectTransform leftVisual;
        RectTransform rightVisual;
        Graphic leftGraphic;
        Graphic rightGraphic;
        RectTransform leftHitArea;
        RectTransform rightHitArea;
        Color leftBaseColor;
        Color rightBaseColor;
        Side pressedSide;
        float pressRemaining;

        public Side HighlightedSide { get; private set; } = Side.Left;
        public float LeftScale { get; private set; } = 1f;
        public float RightScale { get; private set; } = 1f;

        public void Configure(SprintController sprintController, RectTransform leftControl,
            RectTransform rightControl, Graphic leftControlGraphic, Graphic rightControlGraphic)
        {
            controller = sprintController;
            leftVisual = leftControl;
            rightVisual = rightControl;
            leftGraphic = leftControlGraphic;
            rightGraphic = rightControlGraphic;
            leftBaseColor = leftGraphic == null ? Color.white : leftGraphic.color;
            rightBaseColor = rightGraphic == null ? Color.white : rightGraphic.color;
            SetScale(Side.Left, 1f);
            SetScale(Side.Right, 1f);
            RefreshForTest();
        }

        public void BindPressFeedback(ScreenTapArea leftTap, ScreenTapArea rightTap)
        {
            BindPressFeedback(leftTap, Side.Left);
            BindPressFeedback(rightTap, Side.Right);
        }

        public void ConfigureLayout(RectTransform leftTapArea, RectTransform rightTapArea)
        {
            leftHitArea = leftTapArea;
            rightHitArea = rightTapArea;
            Canvas.ForceUpdateCanvases();
            SyncLayout();
        }

        public void RefreshForTest()
        {
            if (controller == null)
                return;

            HighlightedSide = controller.ExpectedSide;
            ApplyHighlight(leftGraphic, leftBaseColor, HighlightedSide == Side.Left);
            ApplyHighlight(rightGraphic, rightBaseColor, HighlightedSide == Side.Right);
        }

        public void PressForTest(Side side)
        {
            if (pressRemaining > 0f)
                SetScale(pressedSide, 1f);

            pressedSide = side;
            pressRemaining = .09f;
            SetScale(side, .94f);
        }

        public void TickForTest(float deltaTime)
        {
            if (pressRemaining <= 0f)
                return;

            pressRemaining = Mathf.Max(0f, pressRemaining - deltaTime);
            if (pressRemaining <= 0f)
                SetScale(pressedSide, 1f);
        }

        void Update()
        {
            RefreshForTest();
            TickForTest(Time.unscaledDeltaTime);
            SyncLayout();
        }

        void BindPressFeedback(ScreenTapArea tapArea, Side side)
        {
            if (tapArea == null)
                return;

            EventTrigger trigger = tapArea.GetComponent<EventTrigger>() ?? tapArea.gameObject.AddComponent<EventTrigger>();
            trigger.triggers ??= new List<EventTrigger.Entry>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(_ => PressForTest(side));
            trigger.triggers.Add(entry);
        }

        void ApplyHighlight(Graphic graphic, Color baseColor, bool highlighted)
        {
            if (graphic == null)
                return;

            Color color = baseColor;
            color.a = .5f;
            graphic.color = color;

            Outline outline = graphic.GetComponent<Outline>();
            if (outline != null)
                outline.effectColor = new Color(1f, .79f, .23f, highlighted ? .45f : .16f);
        }

        void SyncLayout()
        {
            SetVisualLayout(leftVisual, leftHitArea, true);
            SetVisualLayout(rightVisual, rightHitArea, false);
        }

        static void SetVisualLayout(RectTransform visual, RectTransform hitArea, bool left)
        {
            if (visual == null || hitArea == null || visual.parent is not RectTransform parent)
                return;

            Rect safe = new Rect(0f, 0f, 1f, 1f);
            Rect visible = SprintUiLayout.VisibleControlRect(safe, left);
            if (HasSafeAreaInsets(visual))
            {
                visual.anchorMin = new Vector2(visible.xMin, visible.yMin);
                visual.anchorMax = new Vector2(visible.xMax, visible.yMax);
                visual.pivot = new Vector2(.5f, .5f);
                visual.offsetMin = Vector2.zero;
                visual.offsetMax = Vector2.zero;
                return;
            }

            Rect hit = ScreenRect(hitArea);
            Rect layoutHit = SprintUiLayout.HitAreaRect(safe, left);
            float minX = (visible.xMin - layoutHit.xMin) / layoutHit.width;
            float minY = (visible.yMin - layoutHit.yMin) / layoutHit.height;
            float maxX = (visible.xMax - layoutHit.xMin) / layoutHit.width;
            float maxY = (visible.yMax - layoutHit.yMin) / layoutHit.height;
            Vector2 screenMin = new Vector2(hit.xMin + hit.width * minX, hit.yMin + hit.height * minY);
            Vector2 screenMax = new Vector2(hit.xMin + hit.width * maxX, hit.yMin + hit.height * maxY);
            Camera camera = visual.GetComponentInParent<Canvas>()?.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenMin, camera, out Vector2 localMin)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenMax, camera, out Vector2 localMax))
                return;

            visual.anchorMin = visual.anchorMax = Vector2.zero;
            visual.pivot = new Vector2(.5f, .5f);
            visual.anchoredPosition = (localMin + localMax) * .5f;
            visual.sizeDelta = localMax - localMin;
        }

        static bool HasSafeAreaInsets(RectTransform visual)
        {
            var fitters = visual.GetComponentsInParent<UI.SafeAreaFitter>(true);
            for (int i = 0; i < fitters.Length; i++)
            {
                if (fitters[i] == null || !fitters[i].enabled)
                    continue;

                RectTransform safeArea = fitters[i].GetComponent<RectTransform>();
                if (safeArea != null
                    && (safeArea.offsetMin.sqrMagnitude > .01f || safeArea.offsetMax.sqrMagnitude > .01f))
                    return true;
            }

            return false;
        }

        static Rect ScreenRect(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Camera camera = rect.GetComponentInParent<Canvas>()?.worldCamera;
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        void SetScale(Side side, float scale)
        {
            RectTransform visual = side == Side.Left ? leftVisual : rightVisual;
            if (visual != null)
                visual.localScale = new Vector3(scale, scale, 1f);

            if (side == Side.Left)
                LeftScale = scale;
            else
                RightScale = scale;
        }
    }
}
