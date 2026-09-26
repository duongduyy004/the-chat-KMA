using KMA.Gameplay.UI;
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public sealed class VolleyballController : MinigameBase
    {
        public const float MaxStep = .1f;

        [SerializeField] VolleyAthleteView playerView;
        [SerializeField] VolleyAthleteView opponentView;
        [SerializeField] VolleyBallView ballView;
        [SerializeField] VolleyballInputBridge input;
        [SerializeField] VolleyballHud hud;
        float stepDistance;

        public VolleyballMatch Match { get; private set; }
        public MinigameResult LastResult { get; private set; }
        public VolleyAthleteView PlayerView => playerView;
        public VolleyAthleteView OpponentView => opponentView;
        public VolleyBallView BallView => ballView;
        public VolleyballInputBridge Input => input;
        public VolleyballHud Hud => hud;
        public bool HasAllReferences => playerView && opponentView && ballView && input && hud;

        public void Configure(VolleyAthleteView player, VolleyAthleteView opponent, VolleyBallView ball,
            VolleyballInputBridge inputBridge, VolleyballHud volleyballHud)
        {
            playerView = player;
            opponentView = opponent;
            ballView = ball;
            input = inputBridge;
            hud = volleyballHud;
        }

        protected override void Awake()
        {
            base.Awake();
            Match = new VolleyballMatch();
            Match.Completed += OnMatchCompleted;
            Match.PlayerActed += OnPlayerActed;
            Match.PointScored += OnPointScored;
            PhaseChanged += OnPhaseChanged;
        }

        void Start()
        {
            if (HasAllReferences)
                return;

            Debug.LogError("[KMA] VolleyballController is missing a scene reference; disabling.", this);
            enabled = false;
        }

        void OnDestroy()
        {
            PhaseChanged -= OnPhaseChanged;
            if (Match == null)
                return;

            Match.Completed -= OnMatchCompleted;
            Match.PlayerActed -= OnPlayerActed;
            Match.PointScored -= OnPointScored;
        }

        protected override void TickPlay(float dt)
        {
            float step = Mathf.Min(dt, MaxStep);
            if (step <= 0f)
            {
                // Paused (timeScale 0): drop presses rather than replaying them on resume.
                input.ClearPresses();
                return;
            }

            BallFlight previousFlight = Match.Flight;
            Vector2 previousPosition = Match.Player.Position;
            Match.SetMove(input.Move);
            for (int presses = input.ConsumePresses(); presses > 0; presses--)
                Match.PressAction();
            Match.Tick(step);
            if (Match.Flight != previousFlight && Match.BallState == BallState.InPlay)
                GameAudio.Play(GameSound.VolleyHit);
            float moved = Vector2.Distance(previousPosition, Match.Player.Position);
            // Ignore the between-point reset that teleports the athlete to the serve spot.
            if (moved <= VolleyballMatch.PlayerSpeed * step * 1.5f)
                stepDistance += moved;
            if (stepDistance >= .85f)
            {
                stepDistance %= .85f;
                GameAudio.Play(GameSound.SandStep);
            }
        }

        void LateUpdate()
        {
            playerView.Render(Match.Player);
            opponentView.Render(Match.Opponent);
            ballView.Render(Match);
            hud.Render(Match, PresentationPhase, Time.deltaTime);
        }

        protected override MinigameHudState BuildHudState() => new MinigameHudState(
            PresentationPhase.ToString(),
            Match == null ? VolleyballMatch.TimeLimit : Match.TimeRemaining,
            0f,
            0f,
            Match == null ? 0f : Match.PlayerPoints,
            string.Empty);

        public void SkipToPlayForTest()
        {
            SetTutorialGate(false);
            Lifecycle.Tick(float.MaxValue);
        }

        void OnPhaseChanged(MinigamePhase phase)
        {
            if (phase == MinigamePhase.Play && input)
                input.ClearPresses();
        }

        void OnPlayerActed(ActionDecision decision)
        {
            if (hud)
                hud.ShowFeedback(decision);
        }

        void OnPointScored(CourtSide winner)
        {
            if (Match.PlayerPoints < VolleyballMatch.PointsToWin && Match.OpponentPoints < VolleyballMatch.PointsToWin)
                GameAudio.Play(winner == CourtSide.Player ? GameSound.Point : GameSound.Miss);
            if (hud)
                hud.ShowPoint(winner);
        }

        void OnMatchCompleted()
        {
            LastResult = Match.BuildResult();
            Finish(LastResult);
        }
    }
}
