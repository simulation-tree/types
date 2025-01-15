namespace Types.Tests
{
    [Type]
    public partial struct ChildType : IInherit<ParentType>
    {
        public ushort cd;
    }
}