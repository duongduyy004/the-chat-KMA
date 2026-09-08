using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KMA.EditorTools
{
    public readonly struct ResolvedQaTarget
    {
        public ResolvedQaTarget(GameObject gameObject, Vector2 screenPosition, RaycastResult raycastResult)
        {
            GameObject = gameObject;
            ScreenPosition = screenPosition;
            RaycastResult = raycastResult;
        }

        public GameObject GameObject { get; }
        public Vector2 ScreenPosition { get; }
        public RaycastResult RaycastResult { get; }
    }

    public static class UnityQaTargetResolver
    {
        public static bool Resolve(string locator, EventSystem eventSystem, out ResolvedQaTarget target)
        {
            target = default;
            if (string.IsNullOrWhiteSpace(locator) || eventSystem == null)
                return false;

            GameObject resolved = null;
            var matched = 0;
            var pathLocator = locator.Contains("/");
            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!candidate.scene.IsValid() || !candidate.scene.isLoaded || !candidate.activeInHierarchy)
                    continue;

                var matches = pathLocator
                    ? BuildPath(candidate.transform) == locator
                    : candidate.name == locator;
                if (!matches)
                    continue;

                resolved = candidate;
                matched++;
                if (matched > 1)
                    return false;
            }

            if (matched != 1 || !TryGetScreenPosition(resolved, out var screenPosition))
                return false;

            var selectable = resolved.GetComponentInParent<Selectable>();
            if (selectable != null && !selectable.IsInteractable())
                return false;

            var eventData = new PointerEventData(eventSystem) { position = screenPosition };
            var raycasts = new List<RaycastResult>();
            eventSystem.RaycastAll(eventData, raycasts);
            foreach (var raycast in raycasts)
            {
                var handler = FirstPointerHandler(raycast.gameObject);
                if (handler == null)
                    continue;

                if (handler != resolved && !handler.transform.IsChildOf(resolved.transform))
                    return false;

                var handlerSelectable = handler.GetComponentInParent<Selectable>();
                if (handlerSelectable != null && !handlerSelectable.IsInteractable())
                    return false;

                target = new ResolvedQaTarget(resolved, screenPosition, raycast);
                return true;
            }

            return false;
        }

        public static Vector2 NormalizedToScreen(float[] position)
        {
            return new Vector2(position[0] * Screen.width, position[1] * Screen.height);
        }

        private static bool TryGetScreenPosition(GameObject gameObject, out Vector2 screenPosition)
        {
            screenPosition = default;
            var rectTransform = gameObject.GetComponent<RectTransform>();
            if (rectTransform == null)
                return false;

            var canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas == null)
                return false;

            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            var center = (corners[0] + corners[1] + corners[2] + corners[3]) * .25f;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            screenPosition = RectTransformUtility.WorldToScreenPoint(camera, center);
            return true;
        }

        private static GameObject FirstPointerHandler(GameObject gameObject)
        {
            return ExecuteEvents.GetEventHandler<IPointerDownHandler>(gameObject)
                ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(gameObject)
                ?? ExecuteEvents.GetEventHandler<IDragHandler>(gameObject);
        }

        private static string BuildPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}
