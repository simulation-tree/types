using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Types
{
    /// <summary>
    /// Describes metadata for a <see langword="struct"/> type.
    /// </summary>
    [SkipLocalsInit]
    public readonly struct TypeMetadata : IEquatable<TypeMetadata>
    {
        /// <summary>
        /// All registered types.
        /// </summary>
        public static IReadOnlyList<TypeMetadata> All => MetadataRegistry.Types;

        /// <summary>
        /// Hash value unique to this type.
        /// </summary>
        public readonly long hash;

        /// <summary>
        /// Size of the type in bytes.
        /// </summary>
        public readonly ushort Size => TypeData.Get(hash).size;

        /// <summary>
        /// All fields declared in the type.
        /// </summary>
        public unsafe readonly ReadOnlySpan<Field> Fields
        {
            get
            {
                ref TypeData data = ref TypeData.Get(hash);
                fixed (void* fields = &data.fields)
                {
                    return new ReadOnlySpan<Field>(fields, data.fieldCount);
                }
            }
        }

        /// <summary>
        /// All interfaces implemented by this type.
        /// </summary>
        public unsafe readonly ReadOnlySpan<Interface> Interfaces
        {
            get
            {
                ref TypeData data = ref TypeData.Get(hash);
                fixed (void* interfaces = &data.interfaces)
                {
                    return new ReadOnlySpan<Interface>(interfaces, data.interfaceCount);
                }
            }
        }

        /// <summary>
        /// The underlying system type that this represents.
        /// </summary>
        public readonly Type SystemType
        {
            get
            {
                RuntimeTypeHandle handle = TypeHandle;
                return Type.GetTypeFromHandle(handle) ?? throw new InvalidOperationException($"System type not found for handle {handle}");
            }
        }

        /// <summary>
        /// Retrieves the raw handle for this type.
        /// </summary>
        public readonly RuntimeTypeHandle TypeHandle => MetadataRegistry.GetRuntimeTypeHandle(hash);

        /// <summary>
        /// Full name of the type including the namespace.
        /// </summary>
        public readonly ReadOnlySpan<char> FullName => TypeNames.Get(hash);

#if NET
        /// <summary>
        /// Default constructor not supported.
        /// </summary>
        [Obsolete("Default constructor not supported", true)]
        public TypeMetadata()
        {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// Initializes an existing type from the given <paramref name="hash"/>.
        /// </summary>
        public TypeMetadata(long hash)
        {
            this.hash = hash;
        }

        /// <summary>
        /// Initializes an existing value type.
        /// </summary>
        public TypeMetadata(ReadOnlySpan<char> fullName, ushort size)
        {
            hash = TypeNames.Set(fullName);
            ref TypeData data = ref TypeData.Get(hash);
            data.size = size;
            data.fieldCount = 0;
            data.interfaceCount = 0;
            data.fields = default;
            data.interfaces = default;
        }

        /// <summary>
        /// Initializes an existing value type.
        /// </summary>
        public TypeMetadata(string fullName, ushort size)
        {
            hash = TypeNames.Set(fullName);
            ref TypeData data = ref TypeData.Get(hash);
            data.size = size;
            data.fieldCount = 0;
            data.interfaceCount = 0;
            data.fields = default;
            data.interfaces = default;
        }

        /// <summary>
        /// Creates a new type.
        /// </summary>
        public TypeMetadata(ReadOnlySpan<char> fullName, ushort size, ReadOnlySpan<Field> fields, ReadOnlySpan<Interface> interfaces)
        {
            hash = TypeNames.Set(fullName);
            ref TypeData data = ref TypeData.Get(hash);
            data.size = size;
            data.fieldCount = (byte)fields.Length;
            data.fields = FieldBuffer.Create(fields);
            data.interfaceCount = (byte)interfaces.Length;
            data.interfaces = InterfaceBuffer.Create(interfaces);
        }

        /// <summary>
        /// Creates a new type.
        /// </summary>
        public TypeMetadata(ReadOnlySpan<char> fullName, ushort size, FieldBuffer fields, byte fieldCount, InterfaceBuffer interfaces, byte interfaceCount)
        {
            hash = TypeNames.Set(fullName);
            ref TypeData data = ref TypeData.Get(hash);
            data.size = size;
            data.fieldCount = fieldCount;
            data.fields = fields;
            data.interfaceCount = interfaceCount;
            data.interfaces = interfaces;
        }

        /// <summary>
        /// Creates a new type.
        /// </summary>
        public TypeMetadata(string fullName, ushort size, ReadOnlySpan<Field> fields, ReadOnlySpan<Interface> interfaces)
        {
            hash = TypeNames.Set(fullName);
            ref TypeData data = ref TypeData.Get(hash);
            data.size = size;
            data.fieldCount = (byte)fields.Length;
            data.fields = FieldBuffer.Create(fields);
            data.interfaceCount = (byte)interfaces.Length;
            data.interfaces = InterfaceBuffer.Create(interfaces);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return FullName.ToString();
        }

        /// <summary>
        /// Writes a string representation of this type to <paramref name="destination"/>.
        /// </summary>
        public readonly int ToString(Span<char> destination)
        {
            ReadOnlySpan<char> fullName = FullName;
            fullName.CopyTo(destination);
            return fullName.Length;
        }

        /// <summary>
        /// Checks if this type metadata represents type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool Is<T>() where T : unmanaged
        {
            return hash == MetadataRegistry.GetOrRegisterType<T>().hash;
        }

        /// <summary>
        /// Checks if the type implements the given <typeparamref name="T"/>
        /// <see langword="interface"/>.
        /// </summary>
        public unsafe readonly bool Implements<T>()
        {
            Span<char> buffer = stackalloc char[512];
            int length = MetadataRegistry.GetFullName(typeof(T), buffer);
            TypeData data = TypeData.Get(hash);
            ReadOnlySpan<long> span = new(&data.interfaces, data.interfaceCount);
            return span.IndexOf(buffer.Slice(0, length).GetLongHashCode()) != -1;
        }

        /// <summary>
        /// Checks if the type implements the given <paramref name="interfaceValue"/>.
        /// </summary>
        public unsafe readonly bool Implements(Interface interfaceValue)
        {
            TypeData data = TypeData.Get(hash);
            ReadOnlySpan<long> span = new(&data.interfaces, data.interfaceCount);
            return span.IndexOf(interfaceValue.Hash) != -1;
        }

        /// <summary>
        /// Checks if the type implements an inteface with the <paramref name="fullTypeName"/>.
        /// </summary>
        public unsafe readonly bool Implements(ReadOnlySpan<char> fullTypeName)
        {
            TypeData data = TypeData.Get(hash);
            ReadOnlySpan<long> span = new(&data.interfaces, data.interfaceCount);
            return span.IndexOf(fullTypeName.GetLongHashCode()) != -1;
        }

        /// <summary>
        /// Creates an <see cref="object"/> instance of this type.
        /// </summary>
        public readonly object CreateInstance(ReadOnlySpan<byte> bytes)
        {
            return TypeInstanceCreator.Do(this, bytes);
        }

        /// <summary>
        /// Creates an <see cref="object"/> instance of this type
        /// with default state.
        /// </summary>
        public readonly object CreateInstance()
        {
            Span<byte> bytes = stackalloc byte[Size];
            bytes.Clear();
            return TypeInstanceCreator.Do(this, bytes);
        }

        /// <summary>
        /// Copies all fields in this type to the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyFieldsTo(Span<Field> destination)
        {
            TypeData data = TypeData.Get(hash);
            for (int i = 0; i < data.fieldCount; i++)
            {
                destination[i] = data.fields[i];
            }
        }

        /// <summary>
        /// Copies all interfaces that this type implements to the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyInterfacesTo(Span<Interface> destination)
        {
            TypeData data = TypeData.Get(hash);
            for (int i = 0; i < data.interfaceCount; i++)
            {
                destination[i] = data.interfaces[i];
            }
        }

        /// <summary>
        /// Checks if this type contains a fields with the given <paramref name="fieldName"/>.
        /// </summary>
        public readonly bool ContainsField(string fieldName)
        {
            TypeData data = TypeData.Get(hash);
            ReadOnlySpan<char> nameSpan = fieldName.AsSpan();
            for (int i = 0; i < data.fieldCount; i++)
            {
                if (data.fields[i].Name.SequenceEqual(nameSpan))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if this type contains a fields with the given <paramref name="fieldName"/>.
        /// </summary>
        public readonly bool ContainsField(ReadOnlySpan<char> fieldName)
        {
            TypeData data = TypeData.Get(hash);
            ReadOnlySpan<char> nameSpan = fieldName;
            for (int i = 0; i < data.fieldCount; i++)
            {
                if (data.fields[i].Name.SequenceEqual(nameSpan))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieves the field in this type with the given <paramref name="fieldName"/>.
        /// </summary>
        public readonly Field GetField(string fieldName)
        {
            ThrowIfFieldIsMissing(fieldName);

            TypeData data = TypeData.Get(hash);
            for (int i = 0; i < data.fieldCount; i++)
            {
                Field field = data.fields[i];
                if (field.Name.SequenceEqual(fieldName))
                {
                    return field;
                }
            }

            return default;
        }

        /// <summary>
        /// Retrieves the field in this type with the given <paramref name="fieldName"/>.
        /// </summary>
        public readonly Field GetField(ReadOnlySpan<char> fieldName)
        {
            ThrowIfFieldIsMissing(fieldName);

            TypeData data = TypeData.Get(hash);
            for (int i = 0; i < data.fieldCount; i++)
            {
                Field field = data.fields[i];
                if (field.Name.SequenceEqual(fieldName))
                {
                    return field;
                }
            }

            return default;
        }

        /// <summary>
        /// Retrieves the index of the field with the given <paramref name="fieldName"/>.
        /// </summary>
        public readonly int IndexOf(string fieldName)
        {
            ThrowIfFieldIsMissing(fieldName);

            TypeData data = TypeData.Get(hash);
            for (int i = 0; i < data.fieldCount; i++)
            {
                Field field = data.fields[i];
                if (field.Name.SequenceEqual(fieldName))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Retrieves the index of the field with the given <paramref name="fieldName"/>.
        /// </summary>
        public readonly int IndexOf(ReadOnlySpan<char> fieldName)
        {
            ThrowIfFieldIsMissing(fieldName);

            TypeData data = TypeData.Get(hash);
            for (int i = 0; i < data.fieldCount; i++)
            {
                Field field = data.fields[i];
                if (field.Name.SequenceEqual(fieldName))
                {
                    return i;
                }
            }

            return -1;
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfFieldIsMissing(ReadOnlySpan<char> fieldName)
        {
            if (!ContainsField(fieldName))
            {
                throw new InvalidOperationException($"Field with name `{fieldName.ToString()}` not found in type {FullName.ToString()}");
            }
        }

        [Conditional("DEBUG")]
        private unsafe readonly void ThrowIfTypeMismatch<T>() where T : unmanaged
        {
            if (SystemType != typeof(T))
            {
                throw new InvalidOperationException($"Type {FullName.ToString()} does not match {typeof(T).FullName}");
            }
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is TypeMetadata type && Equals(type);
        }

        /// <inheritdoc/>
        public readonly bool Equals(TypeMetadata other)
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

        /// <summary>
        /// Retrieves all types that implement the given <typeparamref name="T"/> interface.
        /// </summary>
        public static IEnumerable<TypeMetadata> GetAllThatImplement<T>()
        {
            Span<char> buffer = stackalloc char[512];
            int length = MetadataRegistry.GetFullName(typeof(T), buffer);
            long hash = buffer.Slice(0, length).GetLongHashCode();
            foreach (TypeMetadata type in All)
            {
                TypeData data = TypeData.Get(type.hash);
                for (int i = 0; i < data.interfaceCount; i++)
                {
                    if (data.interfaces[i].Hash == hash)
                    {
                        yield return type;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves all types that implement the given <paramref name="interfaceValue"/>.
        /// </summary>
        public static IEnumerable<TypeMetadata> GetAllThatImplement(Interface interfaceValue)
        {
            long hash = interfaceValue.Hash;
            foreach (TypeMetadata type in All)
            {
                TypeData data = TypeData.Get(type.hash);
                for (int i = 0; i < data.interfaceCount; i++)
                {
                    if (data.interfaces[i].Hash == hash)
                    {
                        yield return type;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the metadata of type <typeparamref name="T"/>.
        /// </summary>
        public static TypeMetadata Get<T>() where T : unmanaged
        {
            return MetadataRegistry.GetType<T>();
        }

        /// <summary>
        /// Retrieves an existing metadata of type <typeparamref name="T"/>, or
        /// registers if missing.
        /// </summary>
        public static TypeMetadata GetOrRegister<T>() where T : unmanaged
        {
            return MetadataRegistry.GetOrRegisterType<T>();
        }

        /// <inheritdoc/>
        public static bool operator ==(TypeMetadata left, TypeMetadata right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(TypeMetadata left, TypeMetadata right)
        {
            return !(left == right);
        }
    }
}