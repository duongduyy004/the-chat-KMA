using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    // The authored AI. It reacts after a fixed delay, runs to the contact point at a tuned
    // speed, sets on its first touch and attacks on its second according to Plan.Current. It
    // telegraphs smashes and blocks on the steps authored to block. No randomness.
    public sealed partial class VolleyballMatch
    {
        public const float SmashTellSeconds = .35f;
        public const float OpponentBlockX = .6f;
        public const float TipApexHeight = 3f;
        public const float LobApexHeight = 5f;
        public static readonly Vector2 WeakReceiveTarget = new Vector2(-5f, 0f);
        const float ReboundRise = .3f;
        const float ReboundDepth = 2.5f;

        BallFlight opponentResolvedFlight;

        void ResetOpponentState()
        {
            opponentResolvedFlight = null;
        }

        void TickOpponent(float deltaTime)
        {
            if (Rally.Possession != CourtSide.Opponent)
            {
                PositionOpponentWhileDefending(deltaTime);
                return;
            }

            if (FlightTime < tuning.ReactionDelay || ReferenceEquals(opponentResolvedFlight, Flight))
                return;

            OpponentStep step = Plan.Current;
            bool attacking = Rally.Touches >= 1;
            bool smashing = attacking && step.Attack == AttackKind.Smash &&
                            Flight.ApexHeight > ActionResolver.SmashContactHeight;
            float ideal = Flight.TimeAtHeightDescending(smashing
                ? ActionResolver.SmashContactHeight
                : ActionResolver.ReceiveContactHeight);
            Vector2 contact = Flight.GroundAt(ideal);
            Opponent.MoveToward(contact, deltaTime);

            if (smashing && !OpponentSmashTell && FlightTime >= ideal - SmashTellSeconds)
            {
                OpponentSmashTell = true;
                OpponentAim = OpponentPlan.AttackTarget(step, Player.Position);
            }

            if (FlightTime < ideal)
                return;

            opponentResolvedFlight = Flight;
            if (Vector2.Distance(Opponent.Position, contact) > ActionResolver.Reach)
            {
                OpponentSmashTell = false;
                return;
            }

            if (attacking)
                OpponentAttack(step, smashing);
            else
                OpponentReceive(step);
        }

        void PositionOpponentWhileDefending(float deltaTime)
        {
            if (BallState != BallState.InPlay || Flight == null)
                return;

            if (Plan.Current.BlocksPlayerSmash && Rally.Possession == CourtSide.Player)
            {
                float lineY = Rally.Touches >= 1 && Flight.ApexHeight > ActionResolver.SmashContactHeight
                    ? Flight.GroundAt(Flight.TimeAtHeightDescending(ActionResolver.SmashContactHeight)).y
                    : Flight.Target.y;
                Opponent.MoveToward(new Vector2(OpponentBlockX, Mathf.Clamp(lineY, -3f, 3f)), deltaTime);
                return;
            }

            Opponent.MoveToward(OpponentReadySpot, deltaTime);
        }

        void OpponentReceive(OpponentStep step)
        {
            Opponent.BeginAction(AthleteAction.Receive, ReceiveSeconds);
            if (step.WeakReceive)
            {
                Rally.RegisterTouch(CourtSide.Opponent, true);
                Launch(WeakReceiveTarget, FreeBallApexHeight, CourtSide.Opponent);
                Plan.Advance();
                return;
            }

            Rally.RegisterTouch(CourtSide.Opponent, false);
            Launch(new Vector2(OpponentSetSpot.x, Mathf.Clamp(Opponent.Position.y, -3f, 3f)), SetApexHeight,
                CourtSide.Opponent);
        }

        void OpponentAttack(OpponentStep step, bool smashing)
        {
            Vector2 target = OpponentSmashTell ? OpponentAim : OpponentPlan.AttackTarget(step, Player.Position);
            OpponentSmashTell = false;
            Plan.Advance();

            if (smashing && PlayerBlocks(target))
            {
                Vector2 from = BallGround;
                float startHeight = BallHeight;
                AwardPoint(CourtSide.Player, true);
                Rebound(from, startHeight, new Vector2(ReboundDepth, Player.Position.y), CourtSide.Player);
                return;
            }

            Opponent.BeginAction(smashing ? AthleteAction.Smash : AthleteAction.Receive,
                smashing ? SmashSeconds : ReceiveSeconds);
            Rally.RegisterTouch(CourtSide.Opponent, true);
            float apex = smashing ? BallHeight + SmashRise : step.Attack == AttackKind.Tip ? TipApexHeight : LobApexHeight;
            Launch(target, apex, CourtSide.Opponent);
        }

        bool PlayerBlocks(Vector2 target) =>
            Player.Action == AthleteAction.Block &&
            Mathf.Abs(Player.Position.x) <= ActionResolver.BlockNetDistance &&
            Mathf.Abs(Player.Position.y - target.y) <= ActionResolver.BlockLateral;

        bool OpponentBlocks(Vector2 target)
        {
            if (!Plan.Current.BlocksPlayerSmash ||
                Opponent.Position.x > ActionResolver.BlockNetDistance ||
                Mathf.Abs(Opponent.Position.y - target.y) > ActionResolver.BlockLateral)
                return false;

            Vector2 from = BallGround;
            float startHeight = BallHeight;
            Opponent.BeginAction(AthleteAction.Block, BlockSeconds);
            AwardPoint(CourtSide.Opponent, false);
            Rebound(from, startHeight, new Vector2(-ReboundDepth, Opponent.Position.y), CourtSide.Opponent);
            return true;
        }

        // Visual only: the point is already awarded; the ball just drops back off the block.
        void Rebound(Vector2 from, float startHeight, Vector2 target, CourtSide blocker)
        {
            Flight = new BallFlight(from, startHeight, target, startHeight + ReboundRise, blocker);
            FlightTime = 0f;
        }
    }
}
