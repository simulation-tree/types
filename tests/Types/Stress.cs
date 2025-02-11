namespace Types.Tests
{
    public readonly struct Stress
    {
        public readonly byte first;
        public readonly ushort second;
        public readonly uint third;
        public readonly float fourth;
        public readonly Cherry cherry;

        public readonly override string ToString()
        {
            return $"Stress: {first}, {second}, {third}, {fourth}, {cherry}";
        }
    }
}