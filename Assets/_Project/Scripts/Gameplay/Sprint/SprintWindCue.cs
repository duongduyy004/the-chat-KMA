using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public sealed class SprintWindCue : MonoBehaviour
    {
        [SerializeField] SprintController controller;
        [SerializeField] GameObject cueRoot;
        [SerializeField] TMP_Text stateLabel;
        [SerializeField] Image cueImage;
        [SerializeField] Color cueColor = Color.white;
        [SerializeField] Color activeColor = Color.yellow;
        [SerializeField] Color successColor = Color.green;
        [SerializeField] Color failureColor = Color.red;
        public string StateText { get; private set; } = string.Empty;

        void Awake()
        {
            if (controller == null) controller = Object.FindFirstObjectByType<SprintController>();
            CacheVisuals();
        }
        void OnEnable() => Refresh();
        void Update() => Refresh();
        public void Refresh()
        {
            if (controller == null) return;
            bool visible = controller.WindCueVisible || controller.WindChallengeCountered || controller.WindChallengeFailed || controller.WindChallengeExpired;
            if (cueRoot != null && cueRoot != gameObject) cueRoot.SetActive(visible);
            if (controller.WindChallengeCountered) StateText = "WIND COUNTERED";
            else if (controller.WindChallengeFailed || controller.WindChallengeExpired) StateText = "WIND MISSED";
            else if (controller.WindWindowActive) StateText = "COUNTER THE WIND NOW";
            else if (controller.WindCueVisible) StateText = "WIND INCOMING";
            else StateText = string.Empty;
            if (stateLabel != null) stateLabel.text = StateText;
            if (cueImage != null) cueImage.color = controller.WindChallengeCountered ? successColor : (controller.WindChallengeFailed || controller.WindChallengeExpired ? failureColor : (controller.WindWindowActive ? activeColor : cueColor));
        }

        public bool HasBoundVisuals => cueRoot != null && cueRoot != gameObject && stateLabel != null && cueImage != null;

        void CacheVisuals()
        {
            var host = cueRoot == null ? transform.Find("WindCueHost") : cueRoot.transform;
            if (host != null)
            {
                cueRoot ??= host.gameObject;
                stateLabel ??= host.Find("State")?.GetComponent<TMP_Text>();
                cueImage ??= host.GetComponent<Image>();
            }
        }
    }
}
