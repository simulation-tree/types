using Unmanaged;

namespace Types.Tests
{
    [Type]
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

    [Type]
    public partial struct ChildType : IInherits<ParentType>
    {
        public ushort cd;
    }

    [Type]
    public partial struct GrandChildType : IInherits<ChildType>
    {
        public uint value;
    }
}