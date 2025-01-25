﻿using Unmanaged.Tests;

namespace Types.Tests
{
    public abstract class TypeTests : UnmanagedTests
    {
        static TypeTests()
        {
            TypeRegistry.OnRegister = TypeRegistry.RegisterType;
            TypeRegistry.Load<Types.Tests.TypeBank>();
        }
    }
}