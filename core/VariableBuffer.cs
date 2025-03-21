using System;
using System.Diagnostics;

namespace Types
{
    /// <summary>
    /// Buffer for storing <see cref="Variable"/> values.
    /// </summary>
    public unsafe struct VariableBuffer
    {
        /// <summary>
        /// Maximum amount that can be stored.
        /// </summary>
        public const int Capacity = 32;

        private fixed long variables[Capacity];

        /// <summary>
        /// Indexer for accessing the value at the given <paramref name="index"/>.
        /// </summary>
        public Variable this[int index]
        {
            readonly get
            {
                long typeHash = variables[index * 2 + 0];
                long nameHash = variables[index * 2 + 1];
                return new(typeHash, nameHash);
            }
            set
            {
                variables[index * 2 + 0] = value.typeHash;
                variables[index * 2 + 1] = value.nameHash;
            }
        }

        /// <summary>
        /// Creates a new buffer containing the given <paramref name="variables"/>.
        /// </summary>
        public VariableBuffer(ReadOnlySpan<Variable> variables)
        {
            ThrowIfCantFit(variables.Length);

            for (int i = 0; i < variables.Length; i++)
            {
                this.variables[i * 2 + 0] = variables[i].typeHash;
                this.variables[i * 2 + 1] = variables[i].nameHash;
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