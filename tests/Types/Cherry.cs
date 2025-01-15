using Unmanaged;

namespace Types.Tests
{
    [Type]
    public struct Cherry
    {
        public FixedString stones;

        public Cherry(FixedString stones)
        {
            this.stones = stones;
        }
    }
}