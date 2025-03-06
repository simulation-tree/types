using System;
using System.Collections.Generic;

namespace Types
{
    internal static class TypeInstanceCreator
    {
        private static readonly Dictionary<TypeLayout, Create> functions = new();

        public unsafe static void Initialize<T>(TypeLayout type) where T : unmanaged
        {
            functions[type] = static (bytes) =>
            {
                T instance = default;
                void* ptr = &instance;
                Span<byte> instanceBytes = new(ptr, sizeof(T));
                bytes.CopyTo(instanceBytes);
                return instance;
            };
        }

        public static object Do(TypeLayout type, ReadOnlySpan<byte> bytes)
        {
            Create action = functions[type];
            return action(bytes);
        }

        public delegate object Create(ReadOnlySpan<byte> bytes);
    }
}