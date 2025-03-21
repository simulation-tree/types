using System;
using System.Diagnostics;

namespace Types
{
    /// <summary>
    /// Buffer for storing <see cref="TypeLayout"/> values.
    /// </summary>
    public unsafe struct TypeBuffer
    {
        /// <summary>
        /// Maximum amount of values that can be stored.
        /// </summary>
        public const int Capacity = 32;

        private fixed long hashes[Capacity];

        /// <summary>
        /// Indexer for accessing a value at the given <paramref name="index"/>.
        /// </summary>
        public TypeLayout this[int index]
        {
            readonly get
            {
                long hash = hashes[index];
                return TypeRegistry.Get(hash);
            }
            set
            {
                hashes[index] = value.Hash;
            }
        }

        /// <summary>
        /// Creates a new buffer containing the given <paramref name="types"/>.
        /// </summary>
        public TypeBuffer(ReadOnlySpan<TypeLayout> types)
        {
            ThrowIfCantFit(types.Length);

            for (int i = 0; i < types.Length; i++)
            {
                hashes[i] = types[i].Hash;
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