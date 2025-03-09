using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Types
{
    /// <summary>
    /// Describes metadata for a type.
    /// </summary>
    [SkipLocalsInit]
    public struct TypeLayout : IEquatable<TypeLayout>
    {
        /// <summary>
        /// Maximum amount of variables per type.
        /// </summary>
        public const byte Capacity = 16;

        private long hash;
        private ushort size;
        private byte variableCount;
        private Variables16 variables;

        /// <summary>
        /// Hash value unique to this type.
        /// </summary>
        public readonly long Hash => hash;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly Variable[] Variables
        {
            get
            {
                Variable[] variables = new Variable[variableCount];
                for (int i = 0; i < variableCount; i++)
                {
                    variables[i] = this.variables[i];
                }

                return variables;
            }
        }

        /// <summary>
        /// The underlying system type that this layout represents.
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
        public readonly RuntimeTypeHandle TypeHandle => TypeRegistry.GetRuntimeTypeHandle(hash);

        /// <summary>
        /// Full name of the type including the namespace.
        /// </summary>
        public readonly ReadOnlySpan<char> FullName => TypeNames.Get(hash);

        /// <summary>
        /// Size of the type in bytes.
        /// </summary>
        public readonly ushort Size => size;

        /// <summary>
        /// Amount of <see cref="Variable"/>s the type has.
        /// </summary>
        public readonly byte Count => variableCount;

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

        /// <summary>
        /// Indexer for variables.
        /// </summary>
        public readonly Variable this[int index] => variables[index];

        /// <summary>
        /// Indexer for variables.
        /// </summary>
        public readonly Variable this[uint index] => variables[(int)index];

#if NET
        /// <summary>
        /// Default constructor not supported.
        /// </summary>
        [Obsolete("Default constructor not supported", true)]
        public TypeLayout()
        {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// Creates a new type layout without any variables set.
        /// </summary>
        public TypeLayout(ReadOnlySpan<char> fullName, ushort size)
        {
            this.size = size;
            variableCount = 0;
            variables = default;
            hash = TypeNames.Set(fullName);
        }

        /// <summary>
        /// Creates a new type layout without any variables set.
        /// </summary>
        public TypeLayout(string fullName, ushort size)
        {
            this.size = size;
            variableCount = 0;
            variables = default;
            hash = TypeNames.Set(fullName);
        }

        /// <summary>
        /// Creates a new type layout.
        /// </summary>
        public TypeLayout(ReadOnlySpan<char> fullName, ushort size, ReadOnlySpan<Variable> variables)
        {
            ThrowIfGreaterThanCapacity(variables.Length);

            this.size = size;
            variableCount = (byte)variables.Length;
            this.variables = new();
            for (int i = 0; i < variableCount; i++)
            {
                this.variables[i] = variables[i];
            }

            hash = TypeNames.Set(fullName);
        }

        /// <summary>
        /// Creates a new type layout.
        /// </summary>
        public TypeLayout(string fullName, ushort size, ReadOnlySpan<Variable> variables)
        {
            ThrowIfGreaterThanCapacity(variables.Length);

            this.size = size;
            variableCount = (byte)variables.Length;
            this.variables = new();
            for (int i = 0; i < variableCount; i++)
            {
                this.variables[i] = variables[i];
            }

            hash = TypeNames.Set(fullName);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return TypeNames.Get(hash).ToString();
        }

        /// <summary>
        /// Writes a string representation of this type layout to <paramref name="destination"/>.
        /// </summary>
        public readonly int ToString(Span<char> destination)
        {
            ReadOnlySpan<char> fullName = TypeNames.Get(hash);
            fullName.CopyTo(destination);
            return fullName.Length;
        }

        /// <summary>
        /// Checks if this type layout matches <typeparamref name="T"/>.
        /// </summary>
        public readonly bool Is<T>() where T : unmanaged
        {
            TypeRegistry.handleToType.TryGetValue(RuntimeTypeTable.GetHandle<T>(), out TypeLayout otherType);
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
        /// Copies all variables in this type to the <paramref name="destination"/>.
        /// </summary>
        public readonly byte CopyVariablesTo(Span<Variable> destination)
        {
            for (int i = 0; i < variableCount; i++)
            {
                destination[i] = variables[i];
            }

            return variableCount;
        }

        /// <summary>
        /// Checks if this type contains a variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool ContainsVariable(string name)
        {
            ReadOnlySpan<char> nameSpan = name.AsSpan();
            for (int i = 0; i < variableCount; i++)
            {
                if (variables[i].Name.SequenceEqual(nameSpan))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if this type contains a variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool ContainsVariable(ReadOnlySpan<char> name)
        {
            ReadOnlySpan<char> nameSpan = name;
            for (int i = 0; i < variableCount; i++)
            {
                if (variables[i].Name.SequenceEqual(nameSpan))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieves the variable in this type with the given <paramref name="name"/>.
        /// </summary>
        public readonly Variable GetVariable(string name)
        {
            ThrowIfVariableIsMissing(name);

            for (int i = 0; i < variableCount; i++)
            {
                Variable variable = variables[i];
                if (variable.Name.SequenceEqual(name))
                {
                    return variable;
                }
            }

            return default;
        }

        /// <summary>
        /// Retrieves the variable in this type with the given <paramref name="name"/>.
        /// </summary>
        public readonly Variable GetVariable(ReadOnlySpan<char> name)
        {
            ThrowIfVariableIsMissing(name);

            for (int i = 0; i < variableCount; i++)
            {
                Variable variable = variables[i];
                if (variable.Name.SequenceEqual(name))
                {
                    return variable;
                }
            }

            return default;
        }

        /// <summary>
        /// Retrieves the index of the variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly int IndexOf(string name)
        {
            ThrowIfVariableIsMissing(name);

            for (int i = 0; i < variableCount; i++)
            {
                Variable variable = variables[i];
                if (variable.Name.SequenceEqual(name))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Retrieves the index of the variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly int IndexOf(ReadOnlySpan<char> name)
        {
            ThrowIfVariableIsMissing(name);

            for (int i = 0; i < variableCount; i++)
            {
                Variable variable = variables[i];
                if (variable.Name.SequenceEqual(name))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Retrieves the full type name for the given <paramref name="type"/>.
        /// </summary>
        public static int GetFullName(Type type, Span<char> buffer)
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

            static void AppendType(Span<char> fullName, ref int length, Type type)
            {
                //todo: handle case where the type name is System.Collections.Generic.List`1+Enumerator[etc, etc]
                Type? current = type;
                string? currentNameSpace = current.Namespace;
                while (current is not null)
                {
                    Type[] genericTypes = current.GenericTypeArguments;
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
        /// Retrieves the full type name for the type <typeparamref name="T"/>.
        /// </summary>
        public static string GetFullName<T>()
        {
            Span<char> buffer = stackalloc char[512];
            int length = GetFullName(typeof(T), buffer);
            return buffer.Slice(0, length).ToString();
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfVariableIsMissing(ReadOnlySpan<char> name)
        {
            if (!ContainsVariable(name))
            {
                throw new InvalidOperationException($"Variable `{name}` not found in type {FullName}");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfGreaterThanCapacity(int length)
        {
            if (length > Capacity)
            {
                throw new InvalidOperationException($"TypeLayout has reached its capacity of {Capacity} variables");
            }
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is TypeLayout layout && Equals(layout);
        }

        /// <inheritdoc/>
        public readonly bool Equals(TypeLayout other)
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
        public static bool operator ==(TypeLayout left, TypeLayout right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(TypeLayout left, TypeLayout right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Describes a variable part of a <see cref="TypeLayout"/>.
        /// </summary>
        public struct Variable : IEquatable<Variable>
        {
            private long nameHash;
            private long typeFullNameHash;

            /// <summary>
            /// Name of the variable.
            /// </summary>
            public readonly ReadOnlySpan<char> Name => TypeNames.Get(nameHash);

            /// <summary>
            /// Type layout of the variable.
            /// </summary>
            public readonly TypeLayout Type => TypeRegistry.Get(typeFullNameHash);

            /// <summary>
            /// Size of the variable in bytes.
            /// </summary>
            public readonly ushort Size => Type.Size;

            /// <summary>
            /// Creates a new variable with the given <paramref name="name"/> and <paramref name="fullTypeName"/>.
            /// </summary>
            public Variable(string name, string fullTypeName)
            {
                this.nameHash = TypeNames.Set(name);
                typeFullNameHash = fullTypeName.GetLongHashCode();
            }

            /// <summary>
            /// Creates a new variable with the given <paramref name="name"/> and <paramref name="fullTypeName"/>.
            /// </summary>
            public Variable(ReadOnlySpan<char> name, ReadOnlySpan<char> fullTypeName)
            {
                this.nameHash = TypeNames.Set(name);
                typeFullNameHash = fullTypeName.GetLongHashCode();
            }

            /// <summary>
            /// Creates a new variable with the given <paramref name="name"/> and <paramref name="typeHash"/>.
            /// </summary>
            public Variable(string name, int typeHash)
            {
                this.nameHash = TypeNames.Set(name);
                this.typeFullNameHash = typeHash;
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
                return nameHash == other.nameHash && typeFullNameHash == other.typeFullNameHash;
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

                    hashCode = hashCode * 31 + (int)typeFullNameHash;
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

        internal struct Variables4
        {
            public Variable a;
            public Variable b;
            public Variable c;
            public Variable d;

            public Variable this[int index]
            {
                readonly get
                {
                    return index switch
                    {
                        0 => a,
                        1 => b,
                        2 => c,
                        3 => d,
                        _ => throw new IndexOutOfRangeException()
                    };
                }
                set
                {
                    switch (index)
                    {
                        case 0:
                            a = value;
                            break;
                        case 1:
                            b = value;
                            break;
                        case 2:
                            c = value;
                            break;
                        case 3:
                            d = value;
                            break;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
            }

            private Variables4(Variable a, Variable b, Variable c, Variable d)
            {
                this.a = a;
                this.b = b;
                this.c = c;
                this.d = d;
            }
        }

        internal struct Variables16
        {
            public Variables4 a;
            public Variables4 b;
            public Variables4 c;
            public Variables4 d;

            public Variable this[int index]
            {
                readonly get
                {
                    int innerIndex = index & 3;
                    int outerIndex = index >> 2;
                    return outerIndex switch
                    {
                        0 => a[innerIndex],
                        1 => b[innerIndex],
                        2 => c[innerIndex],
                        3 => d[innerIndex],
                        _ => throw new IndexOutOfRangeException()
                    };
                }
                set
                {
                    int innerIndex = index & 3;
                    int outerIndex = index >> 2;
                    switch (outerIndex)
                    {
                        case 0:
                            a[innerIndex] = value;
                            break;
                        case 1:
                            b[innerIndex] = value;
                            break;
                        case 2:
                            c[innerIndex] = value;
                            break;
                        case 3:
                            d[innerIndex] = value;
                            break;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
            }

            private Variables16(Variables4 a, Variables4 b, Variables4 c, Variables4 d)
            {
                this.a = a;
                this.b = b;
                this.c = c;
                this.d = d;
            }
        }
    }
}