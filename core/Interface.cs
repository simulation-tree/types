using System;
using System.Collections.Generic;

namespace Types
{
    /// <summary>
    /// Describes an <see langword="interface"/> type that a <see cref="TypeMetadata"/> implements.
    /// </summary>
    public readonly struct Interface : IEquatable<Interface>
    {
        /// <summary>
        /// All registered interfaces.
        /// </summary>
        public static IReadOnlyList<Interface> All => MetadataRegistry.Interfaces;

        private readonly long hash;

        /// <summary>
        /// The unique hash for this interface.
        /// </summary>
        public readonly long Hash => hash;

        /// <summary>
        /// The full name of the interface.
        /// </summary>
        public readonly ReadOnlySpan<char> FullName => TypeNames.Get(hash);

        /// <summary>
        /// Retrieves the raw handle for this interface.
        /// </summary>
        public readonly RuntimeTypeHandle TypeHandle => MetadataRegistry.GetRuntimeInterfaceHandle(hash);

        /// <summary>
        /// Initializes an existing interface.
        /// </summary>
        public Interface(string fullTypeName)
        {
            hash = TypeNames.Set(fullTypeName);
        }

        /// <summary>
        /// Initializes an existing interface.
        /// </summary>
        public Interface(ReadOnlySpan<char> fullTypeName)
        {
            hash = TypeNames.Set(fullTypeName);
        }

        internal Interface(long hash)
        {
            this.hash = hash;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return FullName.ToString();
        }

        /// <summary>
        /// Writes a string representation of this interface to <paramref name="destination"/>.
        /// </summary>
        public readonly int ToString(Span<char> destination)
        {
            ReadOnlySpan<char> fullName = FullName;
            fullName.CopyTo(destination);
            return fullName.Length;
        }

        /// <summary>
        /// Checks if this interface is <typeparamref name="T"/>.
        /// </summary>
        public readonly bool Is<T>()
        {
            Span<char> buffer = stackalloc char[512];
            int length = MetadataRegistry.GetFullName(typeof(T), buffer);
            return hash == buffer.Slice(0, length).GetLongHashCode();
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Interface type && Equals(type);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Interface other)
        {
            return hash == other.hash;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            unchecked
            {
                return (int)hash;
            }
        }

        /// <inheritdoc/>
        public static bool operator ==(Interface left, Interface right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Interface left, Interface right)
        {
            return !(left == right);
        }
    }
}