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

            Color color = highlighted ? Color.Lerp(baseColor, Color.white, .2f) : baseColor;
            color.a = .5f;
            graphic.color = color;
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
