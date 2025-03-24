using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Types
{
#if NET
    /// <summary>
    /// Buffer for storing <see cref="Interface"/> values.
    /// </summary>
    [InlineArray(Capacity)]
    public struct InterfaceBuffer
    {
        /// <summary>
        /// Maximum amount of values that can be stored.
        /// </summary>
        public const int Capacity = 32;

        private Interface element0;

        /// <summary>
        /// Creates a buffer containing the given <paramref name="interfaces"/>.
        /// </summary>
        public static InterfaceBuffer Create(ReadOnlySpan<Interface> interfaces)
        {
            ThrowIfCantFit(interfaces.Length);

            InterfaceBuffer buffer = default;
            for (int i = 0; i < interfaces.Length; i++)
            {
                buffer[i] = interfaces[i];
            }

            return buffer;
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
#else
    /// <summary>
    /// Buffer for storing <see cref="Interface"/> values.
    /// </summary>
    public unsafe struct InterfaceBuffer
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
                ThrowIfOutOfRange(index);

                return new(buffer[index]);
            }
            set
            {
                ThrowIfOutOfRange(index);

                buffer[index] = value.Hash;
            }
        }

        /// <summary>
        /// Creates a buffer containing the given <paramref name="interfaces"/>.
        /// </summary>
        public static InterfaceBuffer Create(ReadOnlySpan<Interface> interfaces)
        {
            ThrowIfCantFit(interfaces.Length);

            InterfaceBuffer buffer = default;
            for (int i = 0; i < interfaces.Length; i++)
            {
                buffer[i] = interfaces[i];
            }

            return buffer;
        }

        [Conditional("DEBUG")]
        private static void ThrowIfCantFit(int length)
        {
            if (length > Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(length), $"Cannot fit {length} types into a buffer of capacity {Capacity}");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfOutOfRange(int index)
        {
            if (index >= Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range for a buffer of capacity {Capacity}");
            }
        }
    }
#endif
}