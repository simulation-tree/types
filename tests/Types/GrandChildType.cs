namespace Types.Tests
{
    public partial struct ChildType : IInherits<ParentType>
    {
        public ushort cd;
    }

    public partial struct GrandChildType : IInherits<ChildType>
    {
        public uint value;
    }

    public partial struct AnotherChildType : IChildType
    {

    }
}