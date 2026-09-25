using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    public enum TouchOutcome
    {
        Kept,
        SentOver,
        FourthTouchFault
    }

    public sealed class RallyState
    {
        public const int MaxTouches = 3;

        public CourtSide Possession { get; private set; }
        public int Touches { get; private set; }
        public CourtSide LastToucher { get; private set; }

        public void BeginServe(CourtSide server)
        {
            Possession = server;
            LastToucher = server;
            Touches = 0;
        }

        public void RegisterServe(CourtSide server)
        {
            LastToucher = server;
            Possession = server.Other();
            Touches = 0;
        }

        public TouchOutcome RegisterTouch(CourtSide side, bool sendsOver)
        {
            if (side != Possession)
            {
                Possession = side;
                Touches = 0;
            }

            LastToucher = side;
            Touches++;
            if (Touches > MaxTouches)
                return TouchOutcome.FourthTouchFault;
            if (!sendsOver)
                return TouchOutcome.Kept;

            Possession = side.Other();
            Touches = 0;
            return TouchOutcome.SentOver;
        }

        public CourtSide WinnerForLanding(Vector2 landing) => CourtSpace.IsIn(landing)
            ? CourtSpace.SideOf(landing).Other()
            : LastToucher.Other();

        public CourtSide WinnerForNetFault() => LastToucher.Other();
    }
}
