using System;

namespace Types.Tests
{
    public class RuntimeTypeHandleTests
    {
        [Test]
        public void CastTypeAddressBackToHandle()
        {
            nint address = RuntimeTypeTable.GetAddress<string>();
            System.Type? type = System.Type.GetTypeFromHandle(RuntimeTypeTable.GetHandle(address));
            Assert.That(type, Is.EqualTo(typeof(string)));
        }
    }
}