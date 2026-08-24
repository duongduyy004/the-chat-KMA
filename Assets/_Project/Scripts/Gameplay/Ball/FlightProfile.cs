using UnityEngine;

namespace KMA.Gameplay
{
    [CreateAssetMenu(menuName = "KMA/Gameplay/Ball Flight Profile")]
    public sealed class FlightProfile : ScriptableObject
    {
        [SerializeField] float gravityScale = 1f;
        [SerializeField] float linearDrag;
        [SerializeField] float groundY;
        [SerializeField] float bounceDamping = 1f;

        public float GravityScale => gravityScale;
        public float LinearDrag => linearDrag;
        public float GroundY => groundY;
        public float BounceDamping => bounceDamping;

        public static FlightProfile Create(float gravityScale, float linearDrag, float groundY, float bounceDamping)
        {
            var value = CreateInstance<FlightProfile>();
            value.gravityScale = gravityScale;
            value.linearDrag = linearDrag;
            value.groundY = groundY;
            value.bounceDamping = bounceDamping;
            return value;
        }
    }
}
