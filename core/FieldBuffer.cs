using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Types
{
#if NET
    /// <summary>
    /// Buffer for storing <see cref="Field"/> values.
    /// </summary>
    [InlineArray(Capacity)]
    public struct FieldBuffer
    {
        /// <summary>
        /// Maximum amount that can be stored.
        /// </summary>
        public const int Capacity = 64;

        private Field element0;

        /// <summary>
        /// Creates a buffer containing the given <paramref name="fields"/>.
        /// </summary>
        public static FieldBuffer Create(ReadOnlySpan<Field> fields)
        {
            ThrowIfCantFit(fields.Length);

            FieldBuffer buffer = default;
            for (int i = 0; i < fields.Length; i++)
            {
                buffer[i] = fields[i];
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
    /// Buffer for storing <see cref="Field"/> values.
    /// </summary>
    public unsafe struct FieldBuffer
    {
        /// <summary>
        /// Maximum amount that can be stored.
        /// </summary>
        public const int Capacity = 32;

        private fixed long buffer[Capacity * 2];

        /// <summary>
        /// Indexer for accessing the value at the given <paramref name="index"/>.
        /// </summary>
        public Field this[int index]
        {
            readonly get
            {
                ThrowIfOutOfRange(index);

                long typeHash = buffer[index * 2 + 0];
                long nameHash = buffer[index * 2 + 1];
                return new(typeHash, nameHash);
            }
            set
            {
                ThrowIfOutOfRange(index);

                buffer[index * 2 + 0] = value.typeHash;
                buffer[index * 2 + 1] = value.nameHash;
            }
        }

        /// <summary>
        /// Creates a buffer containing the given <paramref name="fields"/>.
        /// </summary>
        public static FieldBuffer Create(ReadOnlySpan<Field> fields)
        {
            ThrowIfCantFit(fields.Length);

            FieldBuffer buffer = default;
            for (int i = 0; i < fields.Length; i++)
            {
                buffer[i] = fields[i];
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