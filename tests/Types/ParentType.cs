using Unmanaged;

namespace Types.Tests
{
    public struct ParentType : IEmpty, ISomething
    {
        public byte a;
        public byte b;

        public readonly FixedString Text => "yes";

        public ParentType(byte a, byte b)
        {
            this.a = a;
            this.b = b;
        }

        public void Increment()
        {
            a++;
            b++;
        }

        public static implicit operator ParentType((byte a, byte b) tuple)
        {
            return new ParentType(tuple.a, tuple.b);
        }
    }
}