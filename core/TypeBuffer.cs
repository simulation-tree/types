using System;
using System.Diagnostics;

namespace Types
{
    /// <summary>
    /// Buffer for storing <see cref="Type"/> values.
    /// </summary>
    public unsafe struct TypeBuffer
    {
        /// <summary>
        /// Maximum amount of values that can be stored.
        /// </summary>
        public const int Capacity = 32;

        private fixed long buffer[Capacity];

        /// <summary>
        /// Indexer for accessing a value at the given <paramref name="index"/>.
        /// </summary>
        public Type this[int index]
        {
            readonly get
            {
                long hash = buffer[index];
                return TypeRegistry.Get(hash);
            }
            set
            {
                buffer[index] = value.Hash;
            }
        }

        /// <summary>
        /// Creates a new buffer containing the given <paramref name="types"/>.
        /// </summary>
        public TypeBuffer(ReadOnlySpan<Type> types)
        {
            ThrowIfCantFit(types.Length);

            for (int i = 0; i < types.Length; i++)
            {
                buffer[i] = types[i].Hash;
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfCantFit(int length)
        {
            if (length > Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(length), $"Cannot fit {length} types into a buffer of capacity {Capacity}");
            }
        }
    }
}