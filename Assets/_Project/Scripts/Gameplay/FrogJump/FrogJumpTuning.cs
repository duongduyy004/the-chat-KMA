using System;

namespace KMA.Gameplay.FrogJump
{
    [Serializable]
    public sealed class FrogJumpTuning
    {
        public float trackMetres = 50f;
        public float sweepSeconds = .9f;
        public float maxJumpMetres = 3f;
        public float minJumpMetres = .5f;
        // Normalised distance from the centre (0 centre, 1 edge) beyond which the jump is a fall.
        public float safeZone = .4f;
        public float jumpSeconds = .6f;
        public float recoverSeconds = 2.5f;
        public float timeLimitSeconds = 45f;
    }
}
