using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public sealed class EnduranceObstacleCue : MonoBehaviour
    {
        [SerializeField] EnduranceController controller;
        [SerializeField] GameObject cueRoot;
        [SerializeField] Image cueImage;
        [SerializeField] TMP_Text stateLabel;
        [SerializeField] Color warningColor = new(1f, .8f, 0f, 1f);
        [SerializeField] Color activeColor = new(1f, .3f, .2f, 1f);
        [SerializeField] Color clearedColor = Color.green;

        public string StateText { get; private set; } = string.Empty;
        public bool HasBoundVisuals => controller != null && cueRoot != null && cueImage != null && stateLabel != null;

        void Awake() => CacheReferences();

        void OnEnable()
        {
            CacheReferences();
            Refresh();
        }

        void Update() => Refresh();

        public void Refresh()
        {
            if (controller == null || controller.Rules == null)
                return;
            if (!controller.ObstacleCueVisible)
            {
                Hide();
                return;
            }

            if (controller.Rules.Mode == EnduranceInputMode.ObstacleSwipe)
                ShowActive();
            else
                ShowWarning(2);
        }

        public void ShowWarning(int warningLeadBeats)
        {
            StateText = $"OBSTACLE IN {Mathf.Max(0, warningLeadBeats)} BEATS";
            SetVisible(true, warningColor);
        }

        public void ShowActive()
        {
            var cleared = controller != null && controller.Rules != null && controller.Rules.ObstacleCleared;
            StateText = cleared ? "OBSTACLE CLEARED" : "SWIPE NOW";
            SetVisible(true, cleared ? clearedColor : activeColor);
        }

        public void Hide()
        {
            StateText = string.Empty;
            if (cueRoot != null && cueRoot != gameObject) cueRoot.SetActive(false);
            if (stateLabel != null) stateLabel.text = StateText;
        }

        void CacheReferences()
        {
            controller ??= Object.FindFirstObjectByType<EnduranceController>();
            cueRoot ??= gameObject;
        }

        void SetVisible(bool visible, Color color)
        {
            if (cueRoot != null && cueRoot != gameObject) cueRoot.SetActive(visible);
            if (cueImage != null) cueImage.color = color;
            if (stateLabel != null) stateLabel.text = StateText;
        }
    }
}
