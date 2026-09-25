using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public enum BallState
    {
        Held,
        Toss,
        InPlay,
        Dead
    }

    public enum ActionKind
    {
        None,
        ServeToss,
        ServeHit,
        Receive,
        FreeBall,
        Smash,
        Dive,
        Block
    }

    public readonly struct ActionDecision
    {
        public readonly ActionKind Kind;
        public readonly TimingGrade Grade;
        public readonly float Offset;

        public ActionDecision(ActionKind kind, TimingGrade grade, float offset)
        {
            Kind = kind;
            Grade = grade;
            Offset = offset;
        }

        public static ActionDecision None => new ActionDecision(ActionKind.None, TimingGrade.Miss, 0f);
        public float Quality => TimingWindows.Quality(Grade);

        // Timed presses feed the accuracy score; the toss and the block are not graded.
        public bool IsTimed => Kind == ActionKind.ServeHit || Kind == ActionKind.Receive ||
                               Kind == ActionKind.FreeBall || Kind == ActionKind.Smash || Kind == ActionKind.Dive;
    }

    public readonly struct ActionContext
    {
        public readonly VolleyAthlete Athlete;
        public readonly BallState Ball;
        public readonly BallFlight Flight;
        public readonly float FlightTime;
        public readonly RallyState Rally;
        public readonly CourtSide Server;
        public readonly bool OpponentSmashTell;
        public readonly Vector2 OpponentAim;

        public ActionContext(VolleyAthlete athlete, BallState ball, BallFlight flight, float flightTime,
            RallyState rally, CourtSide server, bool opponentSmashTell, Vector2 opponentAim)
        {
            Athlete = athlete;
            Ball = ball;
            Flight = flight;
            FlightTime = flightTime;
            Rally = rally;
            Server = server;
            OpponentSmashTell = opponentSmashTell;
            OpponentAim = opponentAim;
        }
    }

    // The single context-sensitive button. Reach is measured to the contact point, the ball's
    // ground position at the ideal moment, so positioning and timing are judged separately.
    public static class ActionResolver
    {
        public const float Reach = 1f;
        public const float ReceiveContactHeight = 1f;
        public const float SmashContactHeight = 2.6f;
        public const float SmashMinHeight = 2.2f;
        public const float SmashNetDistance = 3f;
        public const float DiveMinDistance = 1f;
        public const float DiveMaxDistance = 2.2f;
        public const float BlockNetDistance = 1.2f;
        public const float BlockLateral = 1f;

        public static ActionDecision Resolve(in ActionContext context)
        {
            VolleyAthlete athlete = context.Athlete;
            if (athlete == null || athlete.IsLocked)
                return ActionDecision.None;

            switch (context.Ball)
            {
                case BallState.Held:
                    return context.Server == athlete.Side
                        ? new ActionDecision(ActionKind.ServeToss, TimingGrade.Miss, 0f)
                        : ActionDecision.None;
                case BallState.Toss:
                    return ResolveServeHit(context);
                case BallState.InPlay:
                    break;
                default:
                    return ActionDecision.None;
            }

            if (context.Flight == null || context.Rally == null)
                return ActionDecision.None;
            if (context.Rally.Possession != athlete.Side)
                return ResolveBlock(context);

            return ResolveTouch(context);
        }

        static ActionDecision ResolveServeHit(in ActionContext context)
        {
            if (context.Server != context.Athlete.Side || context.Flight == null)
                return ActionDecision.None;

            float offset = context.FlightTime - context.Flight.ApexTime;
            TimingGrade grade = TimingWindows.Grade(offset, TimingWindows.ServePerfect);
            return grade == TimingGrade.Miss ? ActionDecision.None : new ActionDecision(ActionKind.ServeHit, grade, offset);
        }

        static ActionDecision ResolveTouch(in ActionContext context)
        {
            VolleyAthlete athlete = context.Athlete;
            BallFlight flight = context.Flight;
            int touches = context.Rally.Touches;
            float time = context.FlightTime;

            if (touches >= 1 && touches <= 2 && Mathf.Abs(athlete.Position.x) <= SmashNetDistance &&
                flight.ApexHeight > SmashContactHeight)
            {
                float smashIdeal = flight.TimeAtHeightDescending(SmashContactHeight);
                float smashOffset = time - smashIdeal;
                TimingGrade smashGrade = TimingWindows.Grade(smashOffset);
                // The height gate confirms the set was high enough to smash at all, judged at the
                // fixed ideal contact moment - not at the actual press time, which would otherwise
                // penalize a late-but-still-within-window press on top of the timing grade.
                if (smashGrade != TimingGrade.Miss && flight.HeightAt(smashIdeal) >= SmashMinHeight &&
                    Vector2.Distance(athlete.Position, flight.GroundAt(smashIdeal)) <= Reach)
                    return new ActionDecision(ActionKind.Smash, smashGrade, smashOffset);
            }

            if (touches > 2)
                return ActionDecision.None;

            float ideal = flight.TimeAtHeightDescending(ReceiveContactHeight);
            float offset = time - ideal;
            TimingGrade grade = TimingWindows.Grade(offset);
            if (grade != TimingGrade.Miss && Vector2.Distance(athlete.Position, flight.GroundAt(ideal)) <= Reach)
                return new ActionDecision(touches == 2 ? ActionKind.FreeBall : ActionKind.Receive, grade, offset);

            float landingDistance = Vector2.Distance(athlete.Position, flight.Target);
            if (flight.IsDescending(time) && flight.HeightAt(time) < ReceiveContactHeight &&
                offset > 0f && landingDistance > DiveMinDistance && landingDistance <= DiveMaxDistance)
                return new ActionDecision(ActionKind.Dive, TimingGrade.Late, offset);

            return ActionDecision.None;
        }

        static ActionDecision ResolveBlock(in ActionContext context)
        {
            Vector2 position = context.Athlete.Position;
            Vector2 from = context.Flight.GroundAt(context.FlightTime);
            float netCrossY = NetCrossY(from, context.OpponentAim);
            bool lined = context.OpponentSmashTell &&
                         Mathf.Abs(position.x) <= BlockNetDistance &&
                         Mathf.Abs(position.y - netCrossY) <= BlockLateral;
            return lined ? new ActionDecision(ActionKind.Block, TimingGrade.Miss, 0f) : ActionDecision.None;
        }

        // Where the ball's straight line from `from` to `target` crosses the net (x=0). A block
        // is judged against the line the ball actually travels, not its eventual landing spot.
        public static float NetCrossY(Vector2 from, Vector2 target) =>
            Mathf.Lerp(from.y, target.y, from.x / (from.x - target.x));
    }
}
