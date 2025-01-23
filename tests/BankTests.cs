using System;
using Types.Functions;

namespace Types.Tests
{
    public class BankTests
    {
        [Test]
        public void LoadCustomBank()
        {
            Assert.That(TypeRegistry.IsRegistered<DateTime>(), Is.False);
            TypeRegistry.Register<CustomTypeBank>();
            Assert.That(TypeRegistry.IsRegistered<DateTime>(), Is.True);
        }

        public readonly struct CustomTypeBank : ITypeBank
        {
            void ITypeBank.Load(Register register)
            {
                register.Invoke<DateTime>();
            }
        }
    }
}