using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Types
{
    /// <summary>
    /// Describes metadata for a type.
    /// </summary>
    [SkipLocalsInit]
    public readonly struct Type : IEquatable<Type>
    {
        /// <summary>
        /// Size of the type in bytes.
        /// </summary>
        public readonly ushort size;

        private readonly byte fieldCount;
        private readonly byte interfaceCount;
        private readonly long hash;
        private readonly FieldBuffer fields;
        private readonly TypeBuffer interfaces;

        /// <summary>
        /// Hash value unique to this type.
        /// </summary>
        public readonly long Hash => hash;

        /// <summary>
        /// All fields declared in the type.
        /// </summary>
        public unsafe readonly ReadOnlySpan<Field> Fields
        {
            get
            {
                fixed (void* pointer = &fields)
                {
                    return new ReadOnlySpan<Field>(pointer, fieldCount);
                }
            }
        }

        /// <summary>
        /// All interfaces implemented by this type.
        /// </summary>
        public unsafe readonly ReadOnlySpan<Type> Interfaces
        {
            get
            {
                fixed (void* pointer = &interfaces)
                {
                    return new ReadOnlySpan<Type>(pointer, interfaceCount);
                }
            }
        }

        /// <summary>
        /// The underlying system type that this represents.
        /// </summary>
        public readonly System.Type SystemType
        {
            get
            {
                RuntimeTypeHandle handle = TypeHandle;
                return System.Type.GetTypeFromHandle(handle) ?? throw new InvalidOperationException($"System type not found for handle {handle}");
            }
        }

        /// <summary>
        /// Retrieves the raw handle for this type.
        /// </summary>
        public readonly RuntimeTypeHandle TypeHandle => TypeRegistry.GetRuntimeTypeHandle(hash);

        /// <summary>
        /// Full name of the type including the namespace.
        /// </summary>
        public readonly ReadOnlySpan<char> FullName => TypeNames.Get(hash);

        /// <summary>
        /// Name of the type.
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

#if NET
        /// <summary>
        /// Default constructor not supported.
        /// </summary>
        [Obsolete("Default constructor not supported", true)]
        public Type()
        {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// Creates a new type without any fields or interfaces set.
        /// </summary>
        public Type(ReadOnlySpan<char> fullName, ushort size)
        {
            this.size = size;
            fieldCount = 0;
            fields = default;
            hash = TypeNames.Set(fullName);
        }

        /// <summary>
        /// Creates a new type without any fields or interfaces set.
        /// </summary>
        public Type(string fullName, ushort size)
        {
            this.size = size;
            fieldCount = 0;
            fields = default;
            hash = TypeNames.Set(fullName);
        }

        /// <summary>
        /// Creates a new type.
        /// </summary>
        public Type(ReadOnlySpan<char> fullName, ushort size, ReadOnlySpan<Field> fields, ReadOnlySpan<Type> interfaces)
        {
            this.size = size;
            fieldCount = (byte)fields.Length;
            this.fields = new(fields);
            interfaceCount = (byte)interfaces.Length;
            this.interfaces = new(interfaces);
            hash = TypeNames.Set(fullName);
        }

        /// <summary>
        /// Creates a new type.
        /// </summary>
        public Type(ReadOnlySpan<char> fullName, ushort size, FieldBuffer fields, byte fieldCount, TypeBuffer interfaces, byte interfaceCount)
        {
            this.size = size;
            this.fieldCount = fieldCount;
            this.fields = fields;
            this.interfaceCount = interfaceCount;
            this.interfaces = interfaces;
            hash = TypeNames.Set(fullName);
        }

        /// <summary>
        /// Creates a new type.
        /// </summary>
        public Type(string fullName, ushort size, ReadOnlySpan<Field> fields, ReadOnlySpan<Type> interfaces)
        {
            this.size = size;
            fieldCount = (byte)fields.Length;
            this.fields = new(fields);
            interfaceCount = (byte)interfaces.Length;
            this.interfaces = new(interfaces);
            hash = TypeNames.Set(fullName);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return TypeNames.Get(hash).ToString();
        }

        /// <summary>
        /// Writes a string representation of this type to <paramref name="destination"/>.
        /// </summary>
        public readonly int ToString(Span<char> destination)
        {
            ReadOnlySpan<char> fullName = TypeNames.Get(hash);
            fullName.CopyTo(destination);
            return fullName.Length;
        }

        /// <summary>
        /// Checks if this type metadata represents type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool Is<T>() where T : unmanaged
        {
            TypeRegistry.handleToType.TryGetValue(RuntimeTypeTable.GetHandle<T>(), out Type otherType);
            return hash == otherType.hash;
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
            Span<byte> bytes = stackalloc byte[size];
            bytes.Clear();
            return CreateInstance(bytes);
        }

        /// <summary>
        /// Copies all fields in this type to the <paramref name="destination"/>.
        /// </summary>
        public readonly byte CopyFieldsTo(Span<Field> destination)
        {
            for (int i = 0; i < fieldCount; i++)
            {
                destination[i] = fields[i];
            }

            return fieldCount;
        }

        /// <summary>
        /// Checks if this type contains a fields with the given <paramref name="fieldName"/>.
        /// </summary>
        public readonly bool ContainsField(string fieldName)
        {
            ReadOnlySpan<char> nameSpan = fieldName.AsSpan();
            for (int i = 0; i < fieldCount; i++)
            {
                if (fields[i].Name.SequenceEqual(nameSpan))
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
            ReadOnlySpan<char> nameSpan = fieldName;
            for (int i = 0; i < fieldCount; i++)
            {
                if (fields[i].Name.SequenceEqual(nameSpan))
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

            for (int i = 0; i < fieldCount; i++)
            {
                Field field = fields[i];
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

            for (int i = 0; i < fieldCount; i++)
            {
                Field field = fields[i];
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

            for (int i = 0; i < fieldCount; i++)
            {
                Field field = fields[i];
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

            for (int i = 0; i < fieldCount; i++)
            {
                Field field = fields[i];
                if (field.Name.SequenceEqual(fieldName))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Retrieves the full type name for the given <paramref name="type"/>.
        /// </summary>
        public static int GetFullName(System.Type type, Span<char> buffer)
        {
            int length = 0;
            AppendType(buffer, ref length, type);
            return length;

            static void Insert(Span<char> buffer, char character, ref int length)
            {
                buffer.Slice(0, length).CopyTo(buffer.Slice(1));
                buffer[0] = character;
                length++;
            }

            static void InsertSpan(Span<char> buffer, ReadOnlySpan<char> text, ref int length)
            {
                buffer.Slice(0, length).CopyTo(buffer.Slice(text.Length));
                text.CopyTo(buffer);
                length += text.Length;
            }

            static void AppendType(Span<char> fullName, ref int length, System.Type type)
            {
                //todo: handle case where the type name is System.Collections.Generic.List`1+Enumerator[etc, etc]
                System.Type? current = type;
                string? currentNameSpace = current.Namespace;
                while (current is not null)
                {
                    System.Type[] genericTypes = current.GenericTypeArguments;
                    string name = current.Name;
                    if (genericTypes.Length > 0)
                    {
                        Insert(fullName, '>', ref length);
                        for (int i = genericTypes.Length - 1; i >= 0; i--)
                        {
                            AppendType(fullName, ref length, genericTypes[i]);
                            if (i > 0)
                            {
                                InsertSpan(fullName, ", ", ref length);
                            }
                        }

                        Insert(fullName, '<', ref length);
                        int index = name.IndexOf('`');
                        if (index != -1)
                        {
                            string trimmedName = name[..index];
                            InsertSpan(fullName, trimmedName, ref length);
                        }
                    }
                    else
                    {
                        InsertSpan(fullName, name, ref length);
                    }

                    current = current.DeclaringType;
                    if (current is not null)
                    {
                        Insert(fullName, '.', ref length);
                    }
                }

                if (currentNameSpace is not null)
                {
                    Insert(fullName, '.', ref length);
                    InsertSpan(fullName, currentNameSpace, ref length);
                }
            }
        }

        /// <summary>
        /// Retrieves the full type name for the given <paramref name="type"/>.
        /// </summary>
        public static string GetFullName(System.Type type)
        {
            Span<char> buffer = stackalloc char[512];
            int length = GetFullName(type, buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Retrieves the full type name for the type <typeparamref name="T"/>.
        /// </summary>
        public static string GetFullName<T>()
        {
            Span<char> buffer = stackalloc char[512];
            int length = GetFullName(typeof(T), buffer);
            return buffer.Slice(0, length).ToString();
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfFieldIsMissing(ReadOnlySpan<char> fieldName)
        {
            if (!ContainsField(fieldName))
            {
                throw new InvalidOperationException($"Field with name `{fieldName.ToString()}` not found in type {FullName.ToString()}");
            }
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Type type && Equals(type);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Type other)
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
        public static bool operator ==(Type left, Type right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Type left, Type right)
        {
            return !(left == right);
        }
    }
}