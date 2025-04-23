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
            MetadataRegistry.Load<CustomMetadataBank>();
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

        public readonly struct CustomMetadataBank : IMetadataBank
        {
            void IMetadataBank.Load(RegisterFunction register)
            {
                register.RegisterType<Field>();
            }
        }
    }
}