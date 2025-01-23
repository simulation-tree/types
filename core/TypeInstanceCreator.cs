using System;
using System.Collections.Generic;
using Unmanaged;

namespace Types
{
    internal static class TypeInstanceCreator
    {
        private static readonly Dictionary<RuntimeTypeHandle, Func<USpan<byte>, object>> functions = new();

        public unsafe static void Initialize<T>() where T : unmanaged
        {
            RuntimeTypeHandle handle = typeof(T).TypeHandle;
            functions[handle] = static (bytes) =>
            {
                T instance = default;
                void* ptr = &instance;
                bytes.CopyTo(ptr, (uint)sizeof(T));
                return instance;
            };
        }

        public static object Do(RuntimeTypeHandle type, USpan<byte> bytes)
        {
            Func<USpan<byte>, object> action = functions[type];
            return action(bytes);
        }
    }
}