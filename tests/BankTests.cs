﻿using Types.Functions;

namespace Types.Tests
{
    public unsafe class BankTests : TypeTests
    {
        [Test]
        public void LoadCustomBank()
        {
            Assert.That(TypeRegistry.IsRegistered<Variable>(), Is.False);
            TypeRegistry.Load<CustomTypeBank>();
            Assert.That(TypeRegistry.IsRegistered<Variable>(), Is.True);
        }

        public readonly struct CustomTypeBank : ITypeBank
        {
            void ITypeBank.Load(Register register)
            {
                register.Invoke<Variable>();
            }
        }
    }
}