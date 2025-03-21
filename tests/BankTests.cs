using System;
using Types.Functions;

namespace Types.Tests
{
    public unsafe class BankTests : Tests
    {
        [Test]
        public void LoadCustomBank()
        {
            Assert.That(TypeRegistry.IsTypeRegistered<Field>(), Is.False);
            TypeRegistry.Load<CustomTypeBank>();
            Assert.That(TypeRegistry.IsTypeRegistered<Field>(), Is.True);
        }

#if DEBUG
        [Test]
        public void ThrowWhenRegisteringTwice()
        {
            Assert.That(TypeRegistry.IsInterfaceRegistered<IComparable>(), Is.False);
            TypeRegistry.RegisterInterface<IComparable>();
            Assert.That(TypeRegistry.IsInterfaceRegistered<IComparable>(), Is.True);
            Assert.Throws<InvalidOperationException>(() => TypeRegistry.RegisterInterface<IComparable>());
        }
#endif

        public readonly struct CustomTypeBank : ITypeBank
        {
            void ITypeBank.Load(RegisterFunction register)
            {
                register.RegisterType<Field>();
            }
        }
    }
}