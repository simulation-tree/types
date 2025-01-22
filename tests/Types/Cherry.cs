using Unmanaged;

namespace Types.Tests
{
    public struct Cherry
    {
        public FixedString stones;

        public Cherry(FixedString stones)
        {
            this.stones = stones;
        }
    }
}