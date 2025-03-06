using System;

namespace Types.Tests
{
    public abstract class TypeTests
    {
        static TypeTests()
        {
            TypeRegistry.Load<TypesTestsTypeBank>();
        }

        [SetUp]
        protected virtual void SetUp()
        {
        }

        [TearDown]
        protected virtual void TearDown()
        {
        }

        protected static bool IsRunningRemotely()
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") is not null)
            {
                return true;
            }

            return false;
        }
    }
}