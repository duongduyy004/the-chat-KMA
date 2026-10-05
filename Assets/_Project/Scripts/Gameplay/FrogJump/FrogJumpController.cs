using KMA.Gameplay.Core;
using KMA.Gameplay.UI;
using TMPro;
using UnityEngine;

namespace KMA.Gameplay.FrogJump
{
    public sealed class FrogJumpController : MinigameBase
    {
        [SerializeField] FrogJumpBalanceConfig balance;
        [SerializeField] FrogJumpView view;
        [SerializeField] FrogJumpPowerBar powerBar;
        [SerializeField] FrogJumpTapArea tapArea;
        [SerializeField] TMP_Text feedback;

        FrogJumpRules rules;
        float feedbackUntil;
        bool finished;

        public FrogJumpRules Rules => rules;
        public bool IsWired => view != null && powerBar != null && tapArea != null && feedback != null;

        public void Configure(FrogJumpBalanceConfig config, FrogJumpView frogView, FrogJumpPowerBar bar,
            FrogJumpTapArea tap, TMP_Text feedbackLabel)
        {
            balance = config;
            view = frogView;
            powerBar = bar;
            tapArea = tap;
            feedback = feedbackLabel;
        }

        protected override void Awake()
        {
            base.Awake();
            rules = new FrogJumpRules(balance != null ? balance.Tuning : new FrogJumpTuning());
            rules.Landed += OnLanded;
        }

        void Start()
        {
            if (tapArea != null) tapArea.Tapped += OnTapped;
            var pause = FindFirstObjectByType<PausePanel>(FindObjectsInactive.Include);
            if (pause != null) pause.SetLeaveOptionsVisible(false);
            if (feedback != null) feedback.text = string.Empty;
        }

        void OnDestroy()
        {
            if (tapArea != null) tapArea.Tapped -= OnTapped;
            if (rules != null) rules.Landed -= OnLanded;
        }

        protected override void Update()
        {
            base.Update();
            bool playing = PresentationPhase == MinigamePhase.Play && !rules.IsOver;
            if (powerBar != null) powerBar.SetNeedle(rules.Needle01, playing && rules.State == FrogJumpState.Aiming);
            if (view != null)
                view.Present(rules, Mathf.Clamp01(rules.StateElapsed / Mathf.Max(.01f, rules.Tuning.jumpSeconds)));
            if (feedback != null && Time.time > feedbackUntil) feedback.text = string.Empty;
        }

        protected override void TickPlay(float dt)
        {
            rules.Tick(dt);
            if (rules.IsOver && !finished)
            {
                finished = true;
                bool pass = rules.ReachedFinish;
                Finish(new MinigameResult(pass, 0f, pass ? Rank.C : Rank.F));
            }
        }

        protected override MinigameHudState BuildHudState() => rules == null
            ? MinigameHudState.Empty
            : new MinigameHudState("PLAY", rules.TimeRemaining, rules.Progress01, 0f, rules.Distance,
                VietText.Fix($"Còn {rules.Tuning.trackMetres - rules.Distance:0.0} m"));

        void OnTapped()
        {
            if (PresentationPhase != MinigamePhase.Play || rules.IsOver) return;
            if (rules.Stop()) GameAudio.Play(GameSound.Click);
        }

        void OnLanded(float metres)
        {
            GameAudio.Play(metres <= 0f ? GameSound.Miss : GameSound.SandStep);
            var haptics = FindFirstObjectByType<HapticsService>();
            if (haptics != null) { if (metres <= 0f) haptics.Fail(); else haptics.Light(); }
            if (feedback == null) return;
            feedback.text = VietText.Fix(metres <= 0f ? "NGÃ!"
                : metres >= rules.Tuning.maxJumpMetres - .3f ? $"ĐẸP! {metres:0.0} m" : $"{metres:0.0} m");
            feedbackUntil = Time.time + .8f;
        }
    }
}
