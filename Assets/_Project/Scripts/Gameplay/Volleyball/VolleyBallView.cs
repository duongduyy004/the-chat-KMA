using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public sealed class VolleyBallView : MonoBehaviour
    {
        public const int BallSortingOrder = 300;
        public const int ShadowSortingOrder = 5;
        public const int MarkerSortingOrder = 6;
        /// <summary>World units the held ball sits to the side of the server, clear of the 1.2-unit-wide body.</summary>
        public const float HeldBallSideOffset = .8f;
        public const float MinShadowScale = .6f;
        public const float ShadowFullHeight = 5f;
        const float MarkerLeadSeconds = .6f;
        const float MarkerGrowth = 1.5f;

        [SerializeField] SpriteRenderer ball;
        [SerializeField] SpriteRenderer shadow;
        [SerializeField] SpriteRenderer contactMarker;
        [SerializeField] SpriteRenderer aimMarker;

        public void Configure(SpriteRenderer ballRenderer, SpriteRenderer shadowRenderer,
            SpriteRenderer contactRenderer, SpriteRenderer aimRenderer)
        {
            ball = ballRenderer;
            shadow = shadowRenderer;
            contactMarker = contactRenderer;
            aimMarker = aimRenderer;
        }

        // 1 while the server holds the ball beside his hand, fading to 0 as the toss carries it over
        // his head, so the ball never covers his face and the served flight starts without a jump.
        public static float HandOffsetWeight(BallState state, float height)
        {
            if (state == BallState.Held)
                return 1f;
            if (state != BallState.Toss)
                return 0f;
            return 1f - Mathf.InverseLerp(VolleyballMatch.TossStartHeight, VolleyballMatch.TossApexHeight, height);
        }

        public static float ShadowScaleFor(float height) =>
            Mathf.Lerp(1f, MinShadowScale, Mathf.Clamp01(height / ShadowFullHeight));

        // The contact ring starts large and closes to 1 at the ideal moment.
        public static float MarkerScaleFor(float secondsToIdeal) =>
            1f + MarkerGrowth * Mathf.Clamp01(secondsToIdeal / MarkerLeadSeconds);

        public void Render(VolleyballMatch match)
        {
            if (match == null || !ball || !shadow)
                return;

            Vector2 ground = match.BallGround;
            float height = match.BallHeight;
            // Towards the net: +x when the player serves, -x for the opponent.
            float towardNet = -CourtSpace.SideSign(match.Server);
            ball.transform.position = CourtSpace.ToWorld(ground, height)
                + Vector3.right * (towardNet * HeldBallSideOffset * HandOffsetWeight(match.BallState, height));
            ball.sortingOrder = BallSortingOrder;
            Vector3 shadowPosition = CourtSpace.ToWorld(ground, 0f);
            shadow.transform.position = shadowPosition;
            shadow.transform.localScale = Vector3.one * ShadowScaleFor(height);
            shadow.sortingOrder = ShadowSortingOrder;

            if (contactMarker)
            {
                bool cue = match.TryGetPlayerContactCue(out float secondsToIdeal);
                contactMarker.enabled = cue;
                if (cue)
                {
                    contactMarker.transform.position = shadowPosition;
                    contactMarker.transform.localScale = Vector3.one * MarkerScaleFor(secondsToIdeal);
                    contactMarker.sortingOrder = MarkerSortingOrder;
                }
            }

            if (aimMarker)
            {
                aimMarker.enabled = match.OpponentSmashTell;
                if (match.OpponentSmashTell)
                {
                    aimMarker.transform.position = CourtSpace.ToWorld(match.OpponentAim, 0f);
                    aimMarker.sortingOrder = MarkerSortingOrder;
                }
            }
        }
    }
}
