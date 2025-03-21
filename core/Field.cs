using System;

namespace Types
{
    /// <summary>
    /// Describes a field declared in a <see cref="Types.Type"/>.
    /// </summary>
    public readonly struct Field : IEquatable<Field>
    {
        internal readonly long typeHash;
        internal readonly long nameHash;

        /// <summary>
        /// The type of the field.
        /// </summary>
        public readonly Type Type => TypeRegistry.GetType(typeHash);

        /// <summary>
        /// Name of the field.
        /// </summary>
        public readonly ReadOnlySpan<char> Name => TypeNames.Get(nameHash);

        /// <summary>
        /// Size of the field in bytes.
        /// </summary>
        public readonly ushort Size => Type.size;

#if NET
        /// <summary>
        /// Not supported.
        /// </summary>
        [Obsolete("Default constructor not supported", true)]
        public Field()
        {
        }
#endif

        internal Field(long typeHash, long nameHash)
        {
            this.typeHash = typeHash;
            this.nameHash = nameHash;
        }

        /// <summary>
        /// Creates a new field with the given <paramref name="fieldName"/> and <paramref name="fullTypeName"/>.
        /// </summary>
        public Field(string fieldName, string fullTypeName)
        {
            nameHash = TypeNames.Set(fieldName);
            typeHash = fullTypeName.GetLongHashCode();
        }

        /// <summary>
        /// Creates a new field with the given <paramref name="fieldName"/> and <paramref name="fullTypeName"/>.
        /// </summary>
        public Field(ReadOnlySpan<char> fieldName, ReadOnlySpan<char> fullTypeName)
        {
            nameHash = TypeNames.Set(fieldName);
            typeHash = fullTypeName.GetLongHashCode();
        }

        /// <summary>
        /// Creates a new field with the given <paramref name="fieldName"/> and <paramref name="typeHash"/>.
        /// </summary>
        public Field(string fieldName, int typeHash)
        {
            nameHash = TypeNames.Set(fieldName);
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
        /// Builds a string representation of this field and writes it to <paramref name="buffer"/>.
        /// </summary>
        /// <returns>Amount of characters written.</returns>
        public readonly int ToString(Span<char> buffer)
        {
            Type type = Type;
            type.Name.CopyTo(buffer);
            int length = type.Name.Length;
            buffer[length++] = '=';
            Name.CopyTo(buffer.Slice(length));
            length += Name.Length;
            return length;
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Field field && Equals(field);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Field other)
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
        public static bool operator ==(Field left, Field right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Field left, Field right)
        {
            return !(left == right);
        }
    }
}