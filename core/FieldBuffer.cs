using System;
using System.Diagnostics;

namespace Types
{
    /// <summary>
    /// Buffer for storing <see cref="Field"/> values.
    /// </summary>
    public unsafe struct FieldBuffer
    {
        /// <summary>
        /// Maximum amount that can be stored.
        /// </summary>
        public const int Capacity = 32;

        private fixed long buffer[Capacity];

        /// <summary>
        /// Indexer for accessing the value at the given <paramref name="index"/>.
        /// </summary>
        public Field this[int index]
        {
            readonly get
            {
                long typeHash = buffer[index * 2 + 0];
                long nameHash = buffer[index * 2 + 1];
                return new(typeHash, nameHash);
            }
            set
            {
                buffer[index * 2 + 0] = value.typeHash;
                buffer[index * 2 + 1] = value.nameHash;
            }
        }

        /// <summary>
        /// Creates a new buffer containing the given <paramref name="fields"/>.
        /// </summary>
        public FieldBuffer(ReadOnlySpan<Field> fields)
        {
            ThrowIfCantFit(fields.Length);

            for (int i = 0; i < fields.Length; i++)
            {
                buffer[i * 2 + 0] = fields[i].typeHash;
                buffer[i * 2 + 1] = fields[i].nameHash;
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