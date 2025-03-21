using System;
using System.Diagnostics;

namespace Types
{
    /// <summary>
    /// Buffer for storing <see cref="Interface"/> values.
    /// </summary>
    public unsafe struct InterfaceTypeBuffer
    {
        /// <summary>
        /// Maximum amount of values that can be stored.
        /// </summary>
        public const int Capacity = 32;

        private fixed long buffer[Capacity];

        /// <summary>
        /// Indexer for accessing a value at the given <paramref name="index"/>.
        /// </summary>
        public Interface this[int index]
        {
            readonly get
            {
                long hash = buffer[index];
                return TypeRegistry.GetInterface(hash);
            }
            set
            {
                buffer[index] = value.Hash;
            }
        }

        /// <summary>
        /// Creates a new buffer containing the given <paramref name="types"/>.
        /// </summary>
        public InterfaceTypeBuffer(ReadOnlySpan<Interface> types)
        {
            ThrowIfCantFit(types.Length);

            for (int i = 0; i < types.Length; i++)
            {
                buffer[i] = types[i].Hash;
            }
        }

        /// <summary>
        /// Retrieves the raw interface hash at the given <paramref name="index"/>.
        /// </summary>
        public readonly long Get(int index)
        {
            return buffer[index];
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