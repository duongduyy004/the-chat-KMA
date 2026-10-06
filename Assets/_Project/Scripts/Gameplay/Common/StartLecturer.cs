using UnityEngine;

namespace KMA.Gameplay
{
    /// The lecturer who starts every sports minigame: arms crossed during the tutorial, whistle
    /// raised during the countdown, and the blast on the same frame the shared Whistle sound plays.
    /// The art faces right, so a lecturer standing to the right of the athletes sets <c>faceLeft</c>.
    public sealed class StartLecturer : MonoBehaviour
    {
        public enum Pose { Waiting, WhistleRaised, WhistleBlown }

        /// How long the blast pose is held before she folds her arms again.
        public const float BlastSeconds = .7f;

        [SerializeField] SpriteRenderer body;
        [SerializeField] Sprite waiting;
        [SerializeField] Sprite whistleRaised;
        [SerializeField] Sprite whistleBlown;
        [SerializeField] bool faceLeft;
        [SerializeField] float leaveAfterSeconds;

        MinigameBase source;
        float blastRemaining;
        float leaveRemaining;

        public Pose Current { get; private set; }
        public bool FacesLeft => body != null && body.flipX;
        public float Alpha => body == null ? 0f : body.color.a;

        public void Configure(SpriteRenderer renderer, Sprite waitingPose, Sprite raisedPose, Sprite blownPose,
            bool facingLeft, float leaveAfter)
        {
            body = renderer;
            waiting = waitingPose;
            whistleRaised = raisedPose;
            whistleBlown = blownPose;
            faceLeft = facingLeft;
            leaveAfterSeconds = leaveAfter;
            Apply(Pose.Waiting);
        }

        void OnEnable()
        {
            if (body != null)
                body.flipX = faceLeft;
            source = FindFirstObjectByType<MinigameBase>();
            if (source == null)
            {
                Apply(Pose.Waiting);
                return;
            }

            source.PhaseChanged += OnPhaseChanged;
            OnPhaseChanged(source.PresentationPhase);
        }

        void OnDisable()
        {
            if (source != null)
                source.PhaseChanged -= OnPhaseChanged;
            source = null;
        }

        void Update() => Tick(Time.deltaTime);

        public void OnPhaseChanged(MinigamePhase phase)
        {
            switch (phase)
            {
                case MinigamePhase.Countdown:
                    if (body != null)
                        SetAlpha(1f);
                    Apply(Pose.WhistleRaised);
                    break;
                case MinigamePhase.Play:
                    blastRemaining = BlastSeconds;
                    leaveRemaining = leaveAfterSeconds;
                    Apply(Pose.WhistleBlown);
                    break;
                default:
                    blastRemaining = 0f;
                    leaveRemaining = 0f;
                    if (body != null)
                        SetAlpha(1f);
                    Apply(Pose.Waiting);
                    break;
            }
        }

        public void Tick(float dt)
        {
            if (blastRemaining > 0f)
            {
                blastRemaining -= dt;
                if (blastRemaining <= 0f)
                    Apply(Pose.Waiting);
            }

            if (leaveRemaining > 0f)
            {
                leaveRemaining = Mathf.Max(0f, leaveRemaining - dt);
                SetAlpha(Mathf.Clamp01(leaveRemaining / .4f));
            }
        }

        void Apply(Pose pose)
        {
            Current = pose;
            if (body == null)
                return;
            body.sprite = pose switch
            {
                Pose.WhistleRaised => whistleRaised,
                Pose.WhistleBlown => whistleBlown,
                _ => waiting
            };
        }

        void SetAlpha(float alpha)
        {
            Color color = body.color;
            color.a = alpha;
            body.color = color;
        }
    }
}
