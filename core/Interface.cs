using System;

namespace Types
{
    /// <summary>
    /// Describes an <see langword="interface"/> type that a <see cref="Type"/> implements.
    /// </summary>
    public readonly struct Interface : IEquatable<Interface>
    {
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
        /// Name of the interface.
        /// </summary>
        public readonly ReadOnlySpan<char> Name
        {
            get
            {
                ReadOnlySpan<char> fullName = TypeNames.Get(hash);
                int index = fullName.LastIndexOf('.');
                if (index != -1)
                {
                    return fullName.Slice(index + 1);
                }
                else
                {
                    return fullName;
                }
            }
        }

        /// <summary>
        /// Retrieves the raw handle for this interface.
        /// </summary>
        public readonly RuntimeTypeHandle TypeHandle => TypeRegistry.GetRuntimeInterfaceHandle(hash);

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