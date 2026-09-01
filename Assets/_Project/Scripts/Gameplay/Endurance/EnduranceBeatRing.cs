using KMA.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KMA.Gameplay
{
    public sealed class EnduranceBeatRing : MonoBehaviour
    {
        [SerializeField] EnduranceController controller;
        [SerializeField] EnduranceInputBridge inputBridge;
        [SerializeField] Image ringFill;
        [SerializeField] TMP_Text promptLabel;
        [SerializeField] TMP_Text feedbackLabel;
        [SerializeField] Color restingColor = new(.25f, .75f, 1f, 1f);
        [SerializeField] Color beatColor = Color.white;

        public string PromptText { get; private set; } = "TAP TO THE BEAT";
        public string FeedbackText { get; private set; } = string.Empty;
        public bool HasBoundVisuals => controller != null && ringFill != null && promptLabel != null && feedbackLabel != null;

        void Awake() => CacheReferences();

        void OnEnable()
        {
            CacheReferences();
            Subscribe();
            Refresh();
        }

        void OnDisable() => Unsubscribe();
        void Update() => Refresh();

        public void SetMode(EnduranceInputMode mode)
        {
            PromptText = mode == EnduranceInputMode.BreathHold ? "HOLD TO BREATHE" :
                mode == EnduranceInputMode.ObstacleSwipe ? "SWIPE TO CLEAR" : "TAP TO THE BEAT";
            if (promptLabel != null) promptLabel.text = PromptText;
        }

        public void Refresh()
        {
            if (controller == null || controller.Rules == null)
                return;

            SetMode(controller.Rules.Mode);
            var interval = controller.BeatIntervalSeconds;
            var elapsed = AudioSettings.dspTime - controller.CurrentBeatDspTime;
            var progress = interval <= 0d ? 0f : Mathf.Clamp01((float)(elapsed / interval));
            if (ringFill != null)
            {
                ringFill.fillAmount = progress;
                ringFill.color = Color.Lerp(beatColor, restingColor, progress);
            }
            if (feedbackLabel != null) feedbackLabel.text = FeedbackText;
        }

        void CacheReferences()
        {
            controller ??= Object.FindFirstObjectByType<EnduranceController>();
            inputBridge ??= Object.FindFirstObjectByType<EnduranceInputBridge>();
        }

        void Subscribe()
        {
            if (inputBridge != null)
                inputBridge.RhythmJudged += OnRhythmJudged;
        }

        void Unsubscribe()
        {
            if (inputBridge != null)
                inputBridge.RhythmJudged -= OnRhythmJudged;
        }

        void OnRhythmJudged(TimingJudge judge)
        {
            FeedbackText = judge == TimingJudge.Perfect ? "PERFECT" : judge == TimingJudge.Good ? "GOOD" : "MISS";
        }
    }
}
