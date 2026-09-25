using UnityEngine;

namespace KMA.Gameplay.Volleyball
{
    // Passive opponent: serves (handled in VolleyballMatch) but never plays the ball.
    // Replaced by the authored AI in the next task.
    public sealed partial class VolleyballMatch
    {
        void ResetOpponentState()
        {
        }

        void TickOpponent(float deltaTime)
        {
        }

        bool OpponentBlocks(Vector2 target) => false;
    }
}
