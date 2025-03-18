using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Types
{
    /// <summary>
    /// Describes metadata for a type.
    /// </summary>
    [SkipLocalsInit]
    public readonly struct TypeLayout : IEquatable<TypeLayout>
    {
        /// <summary>
        /// Maximum amount of variables per type.
        /// </summary>
        public const byte Capacity = 32;

        /// <summary>
        /// Size of the type in bytes.
        /// </summary>
        public readonly ushort size;
        
        /// <summary>
        /// Amount of <see cref="Variable"/>s the type has.
        /// </summary>
        public readonly byte variableCount;
        
        private readonly long hash;
        private readonly VariablesCollection variables;

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
        /// Checks if this type metadata represents type <typeparamref name="T"/>.
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
        /// Retrieves the full type name for the given <paramref name="type"/>.
        /// </summary>
        public static string GetFullName(Type type)
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
        private readonly void ThrowIfVariableIsMissing(ReadOnlySpan<char> name)
        {
            if (!ContainsVariable(name))
            {
                throw new InvalidOperationException($"Variable `{name.ToString()}` not found in type {FullName.ToString()}");
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
        public readonly struct Variable : IEquatable<Variable>
        {
            internal readonly long nameHash;
            internal readonly long typeHash;

            /// <summary>
            /// Name of the variable.
            /// </summary>
            public readonly ReadOnlySpan<char> Name => TypeNames.Get(nameHash);

            /// <summary>
            /// Type layout of the variable.
            /// </summary>
            public readonly TypeLayout Type => TypeRegistry.Get(typeHash);

            /// <summary>
            /// Size of the variable in bytes.
            /// </summary>
            public readonly ushort Size => Type.size;

            /// <summary>
            /// Creates a new variable with the given <paramref name="name"/> and <paramref name="fullTypeName"/>.
            /// </summary>
            public Variable(string name, string fullTypeName)
            {
                this.nameHash = TypeNames.Set(name);
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
                this.nameHash = TypeNames.Set(name);
                typeHash = fullTypeName.GetLongHashCode();
            }

            /// <summary>
            /// Creates a new variable with the given <paramref name="name"/> and <paramref name="typeHash"/>.
            /// </summary>
            public Variable(string name, int typeHash)
            {
                this.nameHash = TypeNames.Set(name);
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

        internal unsafe struct VariablesCollection
        {
            private fixed long variables[TypeLayout.Capacity * 2];

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
        }
    }
}