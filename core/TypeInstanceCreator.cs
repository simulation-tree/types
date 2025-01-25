using System;
using System.Collections.Generic;
using Unmanaged;

namespace Types
{
    internal static class TypeInstanceCreator
    {
        private static readonly Dictionary<TypeLayout, Func<USpan<byte>, object>> functions = new();

        public unsafe static void Initialize<T>(TypeLayout type) where T : unmanaged
        {
            functions[type] = static (bytes) =>
            {
                T instance = default;
                void* ptr = &instance;
                bytes.CopyTo(ptr, (uint)sizeof(T));
                return instance;
            };
        }

        public static object Do(TypeLayout type, USpan<byte> bytes)
        {
            Func<USpan<byte>, object> action = functions[type];
            return action(bytes);
        }
    }
}