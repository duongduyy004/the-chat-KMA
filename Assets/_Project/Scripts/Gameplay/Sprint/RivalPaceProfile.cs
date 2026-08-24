namespace KMA.Gameplay
{
    public sealed class RivalPaceProfile
    {
        public string Name { get; }
        public float OpeningSpeed { get; }
        public float SustainedSpeed { get; }

        public RivalPaceProfile(string name, float openingSpeed, float sustainedSpeed)
        {
            Name = name;
            OpeningSpeed = openingSpeed;
            SustainedSpeed = sustainedSpeed;
        }
    }
}
