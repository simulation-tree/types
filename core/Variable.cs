using System;

namespace Types
{
    /// <summary>
    /// Describes a variable part of a <see cref="TypeLayout"/>.
    /// </summary>
    public readonly struct Variable : IEquatable<Variable>
    {
        internal readonly long typeHash;
        internal readonly long nameHash;

        /// <summary>
        /// Type layout of the variable.
        /// </summary>
        public readonly TypeLayout Type => TypeRegistry.Get(typeHash);

        /// <summary>
        /// Name of the variable.
        /// </summary>
        public readonly ReadOnlySpan<char> Name => TypeNames.Get(nameHash);

        /// <summary>
        /// Size of the variable in bytes.
        /// </summary>
        public readonly ushort Size => Type.size;

#if NET
        /// <summary>
        /// Not supported.
        /// </summary>
        [Obsolete("Default constructor not supported", true)]
        public Variable()
        {
        }
#endif

        /// <summary>
        /// Creates a new variable with the given <paramref name="name"/> and <paramref name="fullTypeName"/>.
        /// </summary>
        public Variable(string name, string fullTypeName)
        {
            nameHash = TypeNames.Set(name);
            typeHash = fullTypeName.GetLongHashCode();
        }

        internal Variable(long typeHash, long nameHash)
        {
            this.typeHash = typeHash;
            this.nameHash = nameHash;
        }

        /// <summary>
        /// Creates a new variable with the given <paramref name="name"/> and <paramref name="fullTypeName"/>.
        /// </summary>
        public Variable(ReadOnlySpan<char> name, ReadOnlySpan<char> fullTypeName)
        {
            nameHash = TypeNames.Set(name);
            typeHash = fullTypeName.GetLongHashCode();
        }

        /// <summary>
        /// Creates a new variable with the given <paramref name="name"/> and <paramref name="typeHash"/>.
        /// </summary>
        public Variable(string name, int typeHash)
        {
            nameHash = TypeNames.Set(name);
            this.typeHash = typeHash;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            Span<char> buffer = stackalloc char[256];
            int length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this variable and writes it to <paramref name="buffer"/>.
        /// </summary>
        /// <returns>Amount of characters written.</returns>
        public readonly int ToString(Span<char> buffer)
        {
            TypeLayout typeLayout = Type;
            typeLayout.Name.CopyTo(buffer);
            int length = typeLayout.Name.Length;
            buffer[length++] = '=';
            Name.CopyTo(buffer.Slice(length));
            length += Name.Length;
            return length;
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Variable variable && Equals(variable);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Variable other)
        {
            return nameHash == other.nameHash && typeHash == other.typeHash;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 17;
                ReadOnlySpan<char> name = Name;
                for (int i = 0; i < name.Length; i++)
                {
                    hashCode = hashCode * 31 + name[i];
                }

                hashCode = hashCode * 31 + (int)typeHash;
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public static bool operator ==(Variable left, Variable right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Variable left, Variable right)
        {
            return !(left == right);
        }
    }
}