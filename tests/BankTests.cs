using System;
using Types.Functions;

namespace Types.Tests
{
    public unsafe class BankTests : Tests
    {
        [Test]
        public void LoadCustomBank()
        {
            Assert.That(MetadataRegistry.IsTypeRegistered<Field>(), Is.False);
            MetadataRegistry.Load<CustomTypeBank>();
            Assert.That(MetadataRegistry.IsTypeRegistered<Field>(), Is.True);
        }

#if DEBUG
        [Test]
        public void ThrowWhenRegisteringTwice()
        {
            Assert.That(MetadataRegistry.IsInterfaceRegistered<IComparable>(), Is.False);
            MetadataRegistry.RegisterInterface<IComparable>();
            Assert.That(MetadataRegistry.IsInterfaceRegistered<IComparable>(), Is.True);
            Assert.Throws<InvalidOperationException>(() => MetadataRegistry.RegisterInterface<IComparable>());
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