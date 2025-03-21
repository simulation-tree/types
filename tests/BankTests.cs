using Types.Functions;

namespace Types.Tests
{
    public unsafe class BankTests : TypeTests
    {
        [Test]
        public void LoadCustomBank()
        {
            Assert.That(TypeRegistry.IsTypeRegistered<Field>(), Is.False);
            TypeRegistry.Load<CustomTypeBank>();
            Assert.That(TypeRegistry.IsTypeRegistered<Field>(), Is.True);
        }

        public readonly struct CustomTypeBank : ITypeBank
        {
            void ITypeBank.Load(RegisterFunction register)
            {
                register.RegisterType<Field>();
            }
        }
    }
}