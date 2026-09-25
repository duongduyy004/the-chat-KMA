using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KMA.Gameplay.Volleyball
{
    // Floating stick: the base jumps to wherever the thumb lands inside the area. The base and
    // knob are children anchored at the area's centre, so their anchoredPosition equals the local
    // point in the area.
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        const int NoPointer = int.MinValue;

        [SerializeField] RectTransform area;
        [SerializeField] RectTransform stickBase;
        [SerializeField] RectTransform knob;
        [SerializeField, Min(1f)] float radius = 100f;
        [SerializeField] Vector2 restPosition;
        [SerializeField] Image baseImage;
        [SerializeField] Image knobImage;

        int pointerId = NoPointer;
        Vector2 origin;

        public Vector2 Value { get; private set; }
        public bool IsHeld { get; private set; }

        public void Configure(RectTransform touchArea, RectTransform baseRect, RectTransform knobRect,
            float stickRadius, Vector2 rest)
        {
            area = touchArea;
            stickBase = baseRect;
            knob = knobRect;
            radius = Mathf.Max(1f, stickRadius);
            restPosition = rest;
            baseImage = stickBase ? stickBase.GetComponent<Image>() : null;
            knobImage = knob ? knob.GetComponent<Image>() : null;
            Release();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerId != NoPointer)
                return;

            pointerId = eventData.pointerId;
            Press(ToLocal(eventData));
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == pointerId)
                Drag(ToLocal(eventData));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == pointerId)
                Release();
        }

        public void Press(Vector2 local)
        {
            IsHeld = true;
            origin = local;
            if (stickBase)
                stickBase.anchoredPosition = origin;
            if (baseImage)
                baseImage.color = new Color(.05f, .65f, .78f, .6f);
            if (knobImage)
                knobImage.color = new Color(.72f, 1f, 1f, .95f);
            Drag(local);
        }

        public void Drag(Vector2 local)
        {
            if (!IsHeld)
                return;

            Vector2 offset = Vector2.ClampMagnitude(local - origin, radius);
            if (knob)
                knob.anchoredPosition = origin + offset;
            Value = offset / radius;
        }

        public void Release()
        {
            pointerId = NoPointer;
            IsHeld = false;
            Value = Vector2.zero;
            origin = restPosition;
            if (stickBase)
                stickBase.anchoredPosition = restPosition;
            if (knob)
                knob.anchoredPosition = restPosition;
            if (baseImage)
                baseImage.color = new Color(.05f, .57f, .7f, .35f);
            if (knobImage)
                knobImage.color = new Color(.72f, 1f, 1f, .75f);
        }

        void OnDisable() => Release();

        Vector2 ToLocal(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position,
                eventData.pressEventCamera, out Vector2 local);
            return local;
        }
    }
}
