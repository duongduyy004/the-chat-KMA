using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay.UI
{
    public sealed class HomeBadgeShine : MonoBehaviour
    {
        RectTransform streak;
        Image image;

        void Awake()
        {
            var clip = new GameObject("ShineClip", typeof(RectTransform), typeof(Image), typeof(Mask));
            clip.transform.SetParent(transform, false);
            var clipRect = (RectTransform)clip.transform;
            clipRect.anchorMin = clipRect.anchorMax = new Vector2(.5f, .5f);
            clipRect.sizeDelta = new Vector2(124f, 124f);
            var face = transform.Find("NavyFace")?.GetComponent<Image>();
            clip.GetComponent<Image>().sprite = face != null ? face.sprite : null;
            clip.GetComponent<Mask>().showMaskGraphic = false;
            var go = new GameObject("Shine", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(clip.transform, false);
            streak = (RectTransform)go.transform;
            streak.anchorMin = streak.anchorMax = new Vector2(.5f, .5f);
            streak.sizeDelta = new Vector2(13f, 108f);
            streak.localRotation = Quaternion.Euler(0f, 0f, 25f);
            image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, .3f);
            image.raycastTarget = false;
        }

        void Update()
        {
            float cycle = Time.unscaledTime % Mathf.Max(.001f, UITheme.Shared.Motion.badgeShineCycle);
            image.enabled = cycle < UITheme.Shared.Motion.badgeShineDuration;
            if (image.enabled)
                streak.anchoredPosition = new Vector2(Mathf.Lerp(-82f, 82f, cycle / Mathf.Max(.001f, UITheme.Shared.Motion.badgeShineDuration)), 0f);
        }
    }
}
