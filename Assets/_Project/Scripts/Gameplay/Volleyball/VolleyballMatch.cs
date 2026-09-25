using System;
using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public readonly struct OpponentTuning
    {
        public readonly float SpeedFactor;
        public readonly float ReactionDelay;

        public OpponentTuning(float speedFactor, float reactionDelay)
        {
            SpeedFactor = Mathf.Max(0f, speedFactor);
            ReactionDelay = Mathf.Max(0f, reactionDelay);
        }

        public static OpponentTuning Default => new OpponentTuning(.85f, .25f);
    }

    // The whole game as plain state: serve, rally, points, clock and result. Advanced only by
    // Tick, so pausing (dt 0) freezes it and every run with the same inputs is identical.
    public sealed partial class VolleyballMatch
    {
        public const int PointsToWin = 5;
        public const float TimeLimit = 120f;
        public const float PointPause = 1.2f;
        public const float OpponentServeDelay = 1f;
        public const float PlayerSpeed = 5f;
        public const float TossStartHeight = 1.2f;
        public const float TossApexHeight = 3.2f;
        public const float SetApexHeight = 4f;
        public const float FreeBallApexHeight = 4.5f;
        public const float ReceiveSeconds = .35f;
        public const float SmashSeconds = .45f;
        public const float BlockSeconds = .6f;
        public const float SmashRise = .2f;
        const float ServeSeconds = .4f;
        const float DiveSeconds = .8f;
        const float DiveLunge = 1.2f;
        const float DiveApexHeight = 3f;
        // .8 keeps a PERFECT serve to the short line (x 3) clear of the net with margin.
        const float PerfectServeRise = .8f;
        const float NormalServeApexHeight = 5f;
        const float OpponentServeApexHeight = 4.5f;

        public static readonly Vector2 PlayerServeSpot = new Vector2(-8.5f, 0f);
        public static readonly Vector2 OpponentServeSpot = new Vector2(8.5f, 0f);
        public static readonly Vector2 PlayerReadySpot = new Vector2(-5f, 0f);
        public static readonly Vector2 OpponentReadySpot = new Vector2(5f, 0f);
        public static readonly Vector2 PlayerSetSpot = new Vector2(-1.5f, 0f);
        public static readonly Vector2 OpponentSetSpot = new Vector2(1.5f, 0f);
        static readonly Vector2 NormalServeTarget = new Vector2(5f, 0f);
        static readonly Vector2 FreeBallTarget = new Vector2(5f, 0f);

        readonly OpponentTuning tuning;
        Vector2 move;
        float serveTimer;
        float pauseLeft;
        bool lastHitWasPlayerSmash;

        public VolleyballMatch(OpponentPlan plan = null, OpponentTuning? tuning = null)
        {
            this.tuning = tuning ?? OpponentTuning.Default;
            Plan = plan ?? OpponentPlan.Authored();
            Player = new VolleyAthlete(CourtSide.Player, PlayerSpeed);
            Opponent = new VolleyAthlete(CourtSide.Opponent, PlayerSpeed * this.tuning.SpeedFactor);
            Server = CourtSide.Player;
            BeginPoint();
        }

        public event Action<CourtSide> PointScored;
        public event Action<ActionDecision> PlayerActed;
        public event Action Completed;

        public VolleyAthlete Player { get; }
        public VolleyAthlete Opponent { get; }
        public RallyState Rally { get; } = new RallyState();
        public OpponentPlan Plan { get; }
        public BallState BallState { get; private set; }
        public BallFlight Flight { get; private set; }
        public float FlightTime { get; private set; }
        public CourtSide Server { get; private set; }
        public int PlayerPoints { get; private set; }
        public int OpponentPoints { get; private set; }
        public float Elapsed { get; private set; }
        public bool IsOver { get; private set; }
        public int Winners { get; private set; }
        public int TimedPresses { get; private set; }
        public float QualitySum { get; private set; }
        public ActionDecision LastDecision { get; private set; }
        public bool OpponentSmashTell { get; private set; }
        public Vector2 OpponentAim { get; private set; }
        public float TimeRemaining => Mathf.Max(0f, TimeLimit - Elapsed);

        public Vector2 BallGround => BallState == BallState.Held || Flight == null
            ? ServerAthlete.Position
            : Flight.GroundAt(FlightTime);

        public float BallHeight => BallState == BallState.Held || Flight == null
            ? TossStartHeight
            : Flight.HeightAt(FlightTime);

        VolleyAthlete ServerAthlete => Server == CourtSide.Player ? Player : Opponent;

        public static Vector2 AimAtOpponent(Vector2 stick) =>
            new Vector2(stick.x < -.3f ? 3f : 7f, Mathf.Clamp(stick.y, -1f, 1f) * 3f);

        public void SetMove(Vector2 stick) => move = Vector2.ClampMagnitude(stick, 1f);

        public ActionDecision PressAction()
        {
            if (IsOver)
                return ActionDecision.None;

            ActionDecision decision = ActionResolver.Resolve(new ActionContext(Player, BallState, Flight, FlightTime,
                Rally, Server, OpponentSmashTell, OpponentAim));
            if (decision.Kind == ActionKind.None)
                return decision;

            // Count the press before applying it: applying can end the match, and the result
            // must already include this press.
            if (decision.IsTimed)
            {
                TimedPresses++;
                QualitySum += decision.Quality;
            }

            LastDecision = decision;
            switch (decision.Kind)
            {
                case ActionKind.ServeToss:
                    StartToss();
                    break;
                case ActionKind.ServeHit:
                    PlayerServe(decision);
                    break;
                case ActionKind.Block:
                    Player.BeginAction(AthleteAction.Block, BlockSeconds);
                    break;
                default:
                    PlayerHit(decision);
                    break;
            }

            PlayerActed?.Invoke(decision);
            return decision;
        }

        public void Tick(float deltaTime)
        {
            if (IsOver || deltaTime <= 0f)
                return;

            Elapsed += deltaTime;
            Player.Tick(deltaTime);
            Opponent.Tick(deltaTime);

            switch (BallState)
            {
                case BallState.Held:
                    if (Server == CourtSide.Player)
                    {
                        Player.Move(new Vector2(0f, move.y), deltaTime);
                    }
                    else
                    {
                        Player.Move(move, deltaTime);
                        serveTimer += deltaTime;
                        if (serveTimer >= OpponentServeDelay)
                            StartToss();
                    }
                    break;
                case BallState.Toss:
                    FlightTime += deltaTime;
                    if (Server == CourtSide.Opponent)
                    {
                        Player.Move(move, deltaTime);
                        if (FlightTime >= Flight.ApexTime)
                            OpponentServe();
                    }
                    else if (FlightTime >= Flight.Duration)
                    {
                        AwardPoint(CourtSide.Opponent, false);
                    }
                    break;
                case BallState.InPlay:
                    FlightTime += deltaTime;
                    Player.Move(move, deltaTime);
                    TickOpponent(deltaTime);
                    if (BallState == BallState.InPlay)
                        ResolveBall();
                    break;
                case BallState.Dead:
                    FlightTime += deltaTime;
                    Player.Move(move, deltaTime);
                    pauseLeft -= deltaTime;
                    if (pauseLeft <= 0f && !IsOver)
                        BeginPoint();
                    break;
            }

            if (!IsOver && Elapsed >= TimeLimit)
                Complete();
        }

        public bool TryGetPlayerContactCue(out float secondsToIdeal)
        {
            secondsToIdeal = 0f;
            if (Flight == null)
                return false;

            float ideal;
            if (BallState == BallState.Toss && Server == CourtSide.Player)
            {
                ideal = Flight.ApexTime;
            }
            else if (BallState == BallState.InPlay && Rally.Possession == CourtSide.Player)
            {
                bool smash = Rally.Touches >= 1 &&
                             Mathf.Abs(Player.Position.x) <= ActionResolver.SmashNetDistance &&
                             Flight.ApexHeight > ActionResolver.SmashContactHeight;
                ideal = Flight.TimeAtHeightDescending(smash
                    ? ActionResolver.SmashContactHeight
                    : ActionResolver.ReceiveContactHeight);
            }
            else
            {
                return false;
            }

            secondsToIdeal = ideal - FlightTime;
            return secondsToIdeal >= -TimingWindows.Late;
        }

        public MinigameResult BuildResult()
        {
            bool pass = IsOver && PlayerPoints > OpponentPoints;
            float accuracy = TimedPresses == 0 ? 0f : 2f * QualitySum / TimedPresses;
            float efficiency = 1f - OpponentPoints / (float)PointsToWin;
            float mastery = Mathf.Min(Winners, PointsToWin) / (float)PointsToWin;
            return ScoreUtil.Build(pass, accuracy, efficiency, mastery);
        }

        public void SetScoreForTest(int playerPoints, int opponentPoints)
        {
            PlayerPoints = Mathf.Max(0, playerPoints);
            OpponentPoints = Mathf.Max(0, opponentPoints);
        }

        public void ForceServerForTest(CourtSide server)
        {
            Server = server;
            BeginPoint();
        }

        void BeginPoint()
        {
            Rally.BeginServe(Server);
            BallState = BallState.Held;
            Flight = null;
            FlightTime = 0f;
            serveTimer = 0f;
            lastHitWasPlayerSmash = false;
            OpponentSmashTell = false;
            Player.PlaceAt(Server == CourtSide.Player ? PlayerServeSpot : PlayerReadySpot);
            Opponent.PlaceAt(Server == CourtSide.Opponent ? OpponentServeSpot : OpponentReadySpot);
            ResetOpponentState();
        }

        void StartToss()
        {
            VolleyAthlete server = ServerAthlete;
            Flight = new BallFlight(server.Position, TossStartHeight, server.Position, TossApexHeight, Server);
            FlightTime = 0f;
            BallState = BallState.Toss;
            server.BeginAction(AthleteAction.Serve, 0f);
        }

        void PlayerServe(ActionDecision decision)
        {
            bool perfect = decision.Grade == TimingGrade.Perfect;
            Vector2 target = perfect ? AimAtOpponent(move) : NormalServeTarget;
            float apex = perfect ? BallHeight + PerfectServeRise : NormalServeApexHeight;
            Rally.RegisterServe(CourtSide.Player);
            Player.BeginAction(AthleteAction.Serve, ServeSeconds);
            Launch(target, apex, CourtSide.Player);
        }

        void OpponentServe()
        {
            OpponentStep step = Plan.Current;
            Rally.RegisterServe(CourtSide.Opponent);
            Opponent.BeginAction(AthleteAction.Serve, ServeSeconds);
            Launch(step.ServeTarget, OpponentServeApexHeight, CourtSide.Opponent);
            Plan.Advance();
        }

        void PlayerHit(ActionDecision decision)
        {
            float startHeight = BallHeight;
            Vector2 target;
            float apex;
            AthleteAction animation;
            float lockSeconds;

            switch (decision.Kind)
            {
                case ActionKind.Receive:
                    target = SetTarget(decision.Grade, decision.Offset);
                    apex = SetApexHeight;
                    animation = AthleteAction.Receive;
                    lockSeconds = ReceiveSeconds;
                    break;
                case ActionKind.FreeBall:
                    target = FreeBallTarget;
                    apex = FreeBallApexHeight;
                    animation = AthleteAction.Receive;
                    lockSeconds = ReceiveSeconds;
                    break;
                case ActionKind.Smash:
                    if (decision.Grade == TimingGrade.Late && decision.Offset < 0f)
                    {
                        target = new Vector2(-.3f, BallGround.y);
                        apex = startHeight + .1f;
                    }
                    else if (decision.Grade == TimingGrade.Late)
                    {
                        target = AimAtOpponent(move);
                        apex = SetApexHeight;
                    }
                    else
                    {
                        target = AimAtOpponent(move);
                        apex = startHeight + SmashRise;
                    }
                    animation = AthleteAction.Smash;
                    lockSeconds = SmashSeconds;
                    break;
                default:
                    Player.Lunge(Flight.Target, DiveLunge);
                    target = SetTarget(TimingGrade.Late, decision.Offset);
                    apex = DiveApexHeight;
                    animation = AthleteAction.Dive;
                    lockSeconds = DiveSeconds;
                    break;
            }

            bool sendsOver = CourtSpace.SideOf(target) == CourtSide.Opponent;
            Player.BeginAction(animation, lockSeconds);
            if (Rally.RegisterTouch(CourtSide.Player, sendsOver) == TouchOutcome.FourthTouchFault)
            {
                AwardPoint(CourtSide.Opponent, false);
                return;
            }

            if (decision.Kind == ActionKind.Smash && sendsOver && OpponentBlocks(target))
                return;

            lastHitWasPlayerSmash = decision.Kind == ActionKind.Smash && sendsOver && decision.Grade != TimingGrade.Late;
            Launch(target, apex, CourtSide.Player);
        }

        Vector2 SetTarget(TimingGrade grade, float offset)
        {
            float error = grade == TimingGrade.Perfect ? 0f : grade == TimingGrade.Good ? 1f : 2.5f;
            float x = PlayerSetSpot.x + (offset < 0f ? -error : error);
            return new Vector2(Mathf.Clamp(x, -7.5f, -.5f), Mathf.Clamp(Player.Position.y, -3f, 3f));
        }

        void Launch(Vector2 target, float apex, CourtSide hitter)
        {
            float startHeight = BallHeight;
            Vector2 from = BallGround;
            Flight = new BallFlight(from, startHeight, target, Mathf.Max(apex, startHeight + .01f), hitter);
            FlightTime = 0f;
            BallState = BallState.InPlay;
        }

        void ResolveBall()
        {
            if (Flight.CrossesNet && !Flight.ClearsNet && FlightTime >= Flight.NetCrossTime)
            {
                AwardPoint(Rally.WinnerForNetFault(), false);
                return;
            }

            if (FlightTime < Flight.Duration)
                return;

            CourtSide winner = Rally.WinnerForLanding(Flight.Target);
            bool winnerShot = winner == CourtSide.Player && lastHitWasPlayerSmash && CourtSpace.IsIn(Flight.Target);
            AwardPoint(winner, winnerShot);
        }

        void AwardPoint(CourtSide winner, bool winnerShot)
        {
            if (winner == CourtSide.Player)
            {
                PlayerPoints++;
                if (winnerShot)
                    Winners++;
            }
            else
            {
                OpponentPoints++;
            }

            Server = winner;
            BallState = BallState.Dead;
            pauseLeft = PointPause;
            OpponentSmashTell = false;
            lastHitWasPlayerSmash = false;
            PointScored?.Invoke(winner);
            if (PlayerPoints >= PointsToWin || OpponentPoints >= PointsToWin)
                Complete();
        }

        void Complete()
        {
            if (IsOver)
                return;

            IsOver = true;
            Completed?.Invoke();
        }
    }
}
