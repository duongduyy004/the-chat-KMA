using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class FootballPresentation : MonoBehaviour
    {
        [SerializeField] SpriteRenderer field;
        [SerializeField] SpriteRenderer goal;
        [SerializeField] SpriteRenderer ball;
        [SerializeField] SpriteRenderer ballShadow;
        [SerializeField] SpriteRenderer player;
        [SerializeField] SpriteRenderer goalkeeper;
        [SerializeField] SpriteRenderer crosshair;
        [SerializeField] Transform leftGoalPoint;
        [SerializeField] Transform rightGoalPoint;
        [SerializeField] Color aimColor = Color.white;
        [SerializeField] Color lockedColor = new Color(1f, .82f, .08f, 1f);

        Vector3 ballStart;
        Vector3 playerStart;
        Vector3 keeperStart;
        Quaternion playerRotation;
        bool configured;

        public void Configure(SpriteRenderer fieldView, SpriteRenderer goalView, SpriteRenderer ballView,
            SpriteRenderer shadowView, SpriteRenderer playerView, SpriteRenderer keeperView,
            SpriteRenderer crosshairView, Transform leftPoint, Transform rightPoint)
        {
            field = fieldView;
            goal = goalView;
            ball = ballView;
            ballShadow = shadowView;
            player = playerView;
            goalkeeper = keeperView;
            crosshair = crosshairView;
            leftGoalPoint = leftPoint;
            rightGoalPoint = rightPoint;
            CaptureRestPositions();
        }

        public bool ValidateReferences() => field && goal && ball && ballShadow && player && goalkeeper && crosshair &&
            leftGoalPoint && rightGoalPoint && Vector3.Distance(leftGoalPoint.position, rightGoalPoint.position) > .01f;

        public Vector3 GoalXToWorld(float x)
        {
            if (!leftGoalPoint || !rightGoalPoint)
                return transform.position;
            return Vector3.Lerp(leftGoalPoint.position, rightGoalPoint.position, Mathf.Clamp01((x + 1f) * .5f));
        }

        public void Render(FootballRules rules)
        {
            if (rules == null || !ValidateReferences())
                return;
            if (!configured)
                CaptureRestPositions();

            bool aiming = rules.State == FootballState.Aiming || rules.State == FootballState.AimLocked ||
                rules.State == FootballState.Charging;
            crosshair.enabled = aiming;
            if (aiming)
            {
                Vector3 point = GoalXToWorld(rules.AimX);
                crosshair.transform.position = point;
                crosshair.color = rules.State == FootballState.Aiming ? aimColor : lockedColor;
            }

            if (rules.State == FootballState.Kicking && rules.LastShot.HasValue)
            {
                float kick = Mathf.Clamp01(rules.StateElapsed / .18f);
                player.transform.localRotation = playerRotation * Quaternion.Euler(0f, 0f, -24f * Mathf.Sin(kick * Mathf.PI));
                ball.transform.position = ballStart + Vector3.right * (kick * .12f) + Vector3.up * (kick * .08f);
            }
            else if ((rules.State == FootballState.Flying || rules.State == FootballState.ShotResult) && rules.LastShot.HasValue)
            {
                var shot = rules.LastShot.Value;
                float t = shot.FlightSeconds <= 0f ? 1f : Mathf.Clamp01(rules.FlightElapsed / shot.FlightSeconds);
                Vector3 target = GoalXToWorld(shot.TargetX);
                Vector3 position = Vector3.Lerp(ballStart, target, t);
                bool shortShot = shot.IsShort;
                position.y += shortShot ? 0f : Mathf.Sin(t * Mathf.PI) * .75f;
                ball.transform.position = position;
                float scale = Mathf.Lerp(1f, shortShot ? .7f : .42f, t);
                ball.transform.localScale = Vector3.one * scale;
                ballShadow.transform.position = Vector3.Lerp(ballStart, new Vector3(target.x, ballStart.y, ballStart.z), t);
                ballShadow.transform.localScale = Vector3.one * Mathf.Lerp(1f, .45f, t);
                goalkeeper.transform.position = GoalXToWorld(rules.KeeperX);
                float dive = Mathf.Clamp(rules.KeeperX * 30f, -28f, 28f);
                goalkeeper.transform.rotation = Quaternion.Euler(0f, 0f, -dive);
                crosshair.enabled = false;
            }
            else
            {
                ball.transform.position = ballStart;
                ball.transform.localScale = Vector3.one;
                ballShadow.transform.position = ballStart;
                ballShadow.transform.localScale = Vector3.one;
                player.transform.localRotation = playerRotation;
                goalkeeper.transform.position = keeperStart;
                goalkeeper.transform.rotation = Quaternion.identity;
            }
        }

        void Awake() => CaptureRestPositions();

        void CaptureRestPositions()
        {
            if (!ball || !ballShadow || !player || !goalkeeper)
                return;
            ballStart = ball.transform.position;
            playerStart = player.transform.position;
            keeperStart = goalkeeper.transform.position;
            playerRotation = player.transform.localRotation;
            configured = true;
        }
    }
}
