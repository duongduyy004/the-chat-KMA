using UnityEngine;

namespace KMA.Gameplay.FrogJump
{
    public sealed class FrogJumpView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer hero;
        [SerializeField] Sprite squatPose;
        [SerializeField] Sprite jumpPose;
        [SerializeField] Sprite fallPose;
        [SerializeField] float startX = -7f;
        [SerializeField] float finishX = 7f;
        [SerializeField] float groundY = -2f;
        [SerializeField] float hopHeight = 1.4f;

        Vector3 baseScale = Vector3.one;

        public void Configure(SpriteRenderer heroRenderer, Sprite squat, Sprite jump, Sprite fall,
            float start, float finish, float ground)
        {
            hero = heroRenderer;
            squatPose = squat;
            jumpPose = jump;
            fallPose = fall;
            startX = start;
            finishX = finish;
            groundY = ground;
        }

        void Awake()
        {
            if (hero != null) baseScale = hero.transform.localScale;
        }

        public void Present(FrogJumpRules rules, float stateProgress01)
        {
            if (hero == null || rules == null) return;
            float track = Mathf.Max(.01f, rules.Tuning.trackMetres);
            float metres = rules.Distance;
            float y = groundY;
            Sprite pose = squatPose;
            Vector3 scale = baseScale;
            switch (rules.State)
            {
                case FrogJumpState.Jumping:
                    metres = Mathf.Lerp(rules.Distance, Mathf.Min(track, rules.Distance + (rules.LastJumpMetres ?? 0f)),
                        stateProgress01);
                    y += Mathf.Sin(stateProgress01 * Mathf.PI) * hopHeight;
                    pose = jumpPose;
                    scale = Vector3.Scale(baseScale, new Vector3(.9f, 1.12f, 1f));
                    break;
                case FrogJumpState.Fallen:
                    pose = fallPose;
                    break;
                case FrogJumpState.Aiming:
                    // Breathing squat so the waiting pose reads as "ready to spring".
                    float squash = 1f - Mathf.PingPong(Time.time * .6f, .08f);
                    scale = Vector3.Scale(baseScale, new Vector3(2f - squash, squash, 1f));
                    break;
            }
            hero.sprite = pose;
            hero.transform.localScale = scale;
            hero.transform.position = new Vector3(Mathf.Lerp(startX, finishX, metres / track), y, 0f);
        }
    }
}
