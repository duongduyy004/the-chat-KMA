using System;
using KMA.Gameplay.Volleyball;
using UnityEngine;

namespace KMA.Tests.Gameplay.Volleyball
{
    static class MatchDriver
    {
        public const float Step = 1f / 120f;

        public static void Advance(VolleyballMatch match, float seconds)
        {
            float elapsed = 0f;
            while (seconds - elapsed > 1e-6f)
            {
                float step = Mathf.Min(Step, seconds - elapsed);
                match.Tick(step);
                elapsed += step;
            }
        }

        public static bool AdvanceUntil(VolleyballMatch match, Func<bool> condition, float maxSeconds)
        {
            for (float elapsed = 0f; elapsed < maxSeconds; elapsed += Step)
            {
                if (condition())
                    return true;
                match.Tick(Step);
            }

            return condition();
        }

        public static void AdvanceToFlightTime(VolleyballMatch match, float flightTime) =>
            Advance(match, flightTime - match.FlightTime);

        // A perfect serve aimed at the far deep corner (7, 3), out of reach of an AI that cannot
        // move from its ready spot (5, 0). Vector2.up keeps the aim exact: a diagonal stick is
        // clamped to length 1 and would aim at (7, 2.12).
        public static ActionDecision ServePerfectAce(VolleyballMatch match)
        {
            match.SetMove(Vector2.zero);
            match.PressAction();
            AdvanceToFlightTime(match, match.Flight.ApexTime);
            match.SetMove(Vector2.up);
            ActionDecision decision = match.PressAction();
            match.SetMove(Vector2.zero);
            return decision;
        }

        // A GOOD serve: slow, high, to the middle of the AI's half at (5, 0).
        public static ActionDecision ServeGood(VolleyballMatch match)
        {
            match.SetMove(Vector2.zero);
            match.PressAction();
            AdvanceToFlightTime(match, match.Flight.ApexTime + .15f);
            return match.PressAction();
        }
    }
}
