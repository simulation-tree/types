using Unmanaged.Tests;

namespace Types.Tests
{
    public abstract class TypeTests : UnmanagedTests
    {
        static TypeTests()
        {
            TypeRegistry.Load<TypesTestsTypeBank>();
        }
    }
}