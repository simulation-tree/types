using System;

namespace Types.Tests
{
    public class RuntimeTypeHandleTests
    {
        [Test]
        public void CastTypeAddressBackToHandle()
        {
            nint address = RuntimeTypeTable.GetAddress<string>();
            Type? type = Type.GetTypeFromHandle(RuntimeTypeTable.GetHandle(address));
            Assert.That(type, Is.EqualTo(typeof(string)));
        }
    }
}