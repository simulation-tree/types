using Unmanaged;

namespace Types.Tests
{
    public struct ParentType : IEmpty, ISomething
    {
        public byte a;
        public byte b;

        public readonly FixedString Text => "yes";

        public void Increment()
        {
            a++;
            b++;
        }
    }

    public partial struct ChildType : IInherits<ParentType>
    {
        public ushort cd;
    }

    public partial struct GrandChildType : IInherits<ChildType>
    {
        public uint value;
    }
}