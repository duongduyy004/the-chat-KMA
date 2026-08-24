using System;
using System.Collections.Generic;
using UnityEngine;

namespace KMA.Gameplay
{
    public sealed class ReturnPattern
    {
        static readonly Vector2[] AuthoredExchanges =
        {
            new Vector2(.75f, .35f),
            new Vector2(-.65f, .5f),
            new Vector2(.55f, .7f),
            new Vector2(-.45f, .3f)
        };

        public ReturnPattern(float placementThreshold, float launchForce)
        {
            if (placementThreshold <= 0f || launchForce <= 0f)
                throw new ArgumentOutOfRangeException(nameof(placementThreshold));

            PlacementThreshold = placementThreshold;
            LaunchForce = launchForce;
        }

        public float PlacementThreshold { get; }
        public float LaunchForce { get; }
        public IReadOnlyList<Vector2> Exchanges => AuthoredExchanges;

        public static ReturnPattern AuthoredDefault() => new ReturnPattern(.15f, 10f);

        public bool IsPlacementValid(Vector2 placement)
        {
            for (var i = 0; i < AuthoredExchanges.Length; i++)
                if (Vector2.Distance(AuthoredExchanges[i], placement) <= PlacementThreshold)
                    return true;

            return false;
        }

        public bool TryLaunch(BallRig ball, int exchangeIndex, float launchSpeed)
        {
            if (!ball || exchangeIndex < 0 || exchangeIndex >= AuthoredExchanges.Length || launchSpeed <= 0f)
                return false;

            ball.Launch(AuthoredExchanges[exchangeIndex], launchSpeed, 0f);
            return true;
        }

        public Vector2 LaunchVelocity(int exchangeIndex, float launchSpeed)
        {
            if (launchSpeed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(launchSpeed));
            if (exchangeIndex < 0 || exchangeIndex >= AuthoredExchanges.Length)
                throw new ArgumentOutOfRangeException(nameof(exchangeIndex));

            return AuthoredExchanges[exchangeIndex].normalized * launchSpeed;
        }
    }
}
