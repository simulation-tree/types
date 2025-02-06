﻿using System;
using System.Diagnostics;
using Unmanaged;

namespace Types
{
    internal struct Variables4 : IEquatable<Variables4>
    {
        public TypeLayout.Variable a;
        public TypeLayout.Variable b;
        public TypeLayout.Variable c;
        public TypeLayout.Variable d;

        public TypeLayout.Variable this[byte index]
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

        public Variables4(TypeLayout.Variable a, TypeLayout.Variable b, TypeLayout.Variable c, TypeLayout.Variable d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Variables4 variables && Equals(variables);
        }

        public readonly bool Equals(Variables4 other)
        {
            return a.Equals(other.a) && b.Equals(other.b) && c.Equals(other.c) && d.Equals(other.d);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(a, b, c, d);
        }

        public static bool operator ==(Variables4 left, Variables4 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Variables4 left, Variables4 right)
        {
            return !(left == right);
        }
    }

    internal struct Variables16 : IEquatable<Variables16>
    {
        public Variables4 a;
        public Variables4 b;
        public Variables4 c;
        public Variables4 d;

        public TypeLayout.Variable this[byte index]
        {
            readonly get
            {
                byte innerIndex = (byte)(index & 3);
                byte outerIndex = (byte)(index >> 2);
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
                byte innerIndex = (byte)(index & 3);
                byte outerIndex = (byte)(index >> 2);
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

        public Variables16(Variables4 a, Variables4 b, Variables4 c, Variables4 d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Variables16 variables && Equals(variables);
        }

        public readonly bool Equals(Variables16 other)
        {
            return a.Equals(other.a) && b.Equals(other.b) && c.Equals(other.c) && d.Equals(other.d);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(a, b, c, d);
        }

        public static bool operator ==(Variables16 left, Variables16 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Variables16 left, Variables16 right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// Describes metadata for a type.
    /// </summary>
    public struct TypeLayout : IEquatable<TypeLayout>, ISerializable
    {
        /// <summary>
        /// Maximum amount of variables per type.
        /// </summary>
        public const uint Capacity = 16;

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
                for (byte i = 0; i < variableCount; i++)
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
        public readonly USpan<char> FullName => TypeNames.Get(hash);

        /// <summary>
        /// Size of the type in bytes.
        /// </summary>
        public readonly ushort Size => size;

        /// <summary>
        /// Amount of variables stored.
        /// </summary>
        public readonly byte Count => variableCount;

        /// <summary>
        /// Name of the type.
        /// </summary>
        public readonly USpan<char> Name
        {
            get
            {
                USpan<char> fullName = FullName;
                if (fullName.TryLastIndexOf('.', out uint index))
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
        public readonly Variable this[byte index] => variables[index];

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
        public TypeLayout(USpan<char> fullName, ushort size)
        {
            this.size = size;
            variableCount = 0;
            variables = default;
            hash = TypeNames.Set(fullName);
        }

        /// <summary>
        /// Creates a new type layout without any variables set.
        /// </summary>
        public TypeLayout(FixedString fullName, ushort size)
        {
            this.size = size;
            variableCount = 0;
            variables = default;
            hash = TypeNames.Set(fullName);
        }

        /// <summary>
        /// Creates a new type layout.
        /// </summary>
        public TypeLayout(USpan<char> fullName, ushort size, USpan<Variable> variables)
        {
            ThrowIfGreaterThanCapacity(variables.Length);

            this.size = size;
            variableCount = (byte)variables.Length;
            this.variables = new();
            for (byte i = 0; i < variableCount; i++)
            {
                this.variables[i] = variables[i];
            }

            hash = TypeNames.Set(fullName);
        }

        /// <summary>
        /// Creates a new type layout.
        /// </summary>
        public TypeLayout(FixedString fullName, ushort size, USpan<Variable> variables)
        {
            ThrowIfGreaterThanCapacity(variables.Length);

            this.size = size;
            variableCount = (byte)variables.Length;
            this.variables = new();
            for (byte i = 0; i < variableCount; i++)
            {
                this.variables[i] = variables[i];
            }

            hash = TypeNames.Set(fullName);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Writes a string representation of this type layout to <paramref name="destination"/>.
        /// </summary>
        public readonly uint ToString(USpan<char> destination)
        {
            return FullName.CopyTo(destination);
        }

        /// <summary>
        /// Checks if this type layout matches <typeparamref name="T"/>.
        /// </summary>
        public readonly bool Is<T>() where T : unmanaged
        {
            if (TypeRegistry.IsRegistered<T>())
            {
                return TypeRegistry.Get<T>() == this;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Creates an <see cref="object"/> instance of this type.
        /// </summary>
        public readonly object CreateInstance(USpan<byte> bytes)
        {
            return TypeInstanceCreator.Do(this, bytes);
        }

        /// <summary>
        /// Creates an <see cref="object"/> instance of this type
        /// with default state.
        /// </summary>
        public readonly object CreateInstance()
        {
            USpan<byte> bytes = stackalloc byte[size];
            return CreateInstance(bytes);
        }

        /// <summary>
        /// Copies all variables in this type to the <paramref name="destination"/>.
        /// </summary>
        public readonly byte CopyVariablesTo(USpan<Variable> destination)
        {
            for (byte i = 0; i < variableCount; i++)
            {
                destination[i] = variables[i];
            }

            return variableCount;
        }

        /// <summary>
        /// Checks if this type contains a variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool ContainsVariable(FixedString name)
        {
            for (byte i = 0; i < variableCount; i++)
            {
                if (variables[i].Name == name)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if this type contains a variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool ContainsVariable(string name)
        {
            USpan<char> nameSpan = name.AsSpan();
            for (byte i = 0; i < variableCount; i++)
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
        public readonly bool ContainsVariable(USpan<char> name)
        {
            USpan<char> nameSpan = name;
            for (byte i = 0; i < variableCount; i++)
            {
                if (variables[i].Name.SequenceEqual(nameSpan))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieves the full type name for the given <typeparamref name="T"/>.
        /// </summary>
        public static FixedString GetFullName<T>()
        {
            FixedString fullName = default;
            AppendType(ref fullName, typeof(T));
            return fullName;

            static void AppendType(ref FixedString text, Type type)
            {
                Type? current = type;
                string? currentNameSpace = current.Namespace;
                while (current is not null)
                {
                    Type[] genericTypes = current.GenericTypeArguments;
                    string name = current.Name;
                    if (genericTypes.Length > 0)
                    {
                        text.Insert(0, '>');
                        for (int i = genericTypes.Length - 1; i >= 0; i--)
                        {
                            AppendType(ref text, genericTypes[i]);
                            if (i > 0)
                            {
                                text.Insert(0, ", ");
                            }
                        }

                        text.Insert(0, '<');
                        text.Insert(0, name[..name.IndexOf('`')]);
                    }
                    else
                    {
                        text.Insert(0, name);
                    }

                    current = current.DeclaringType;
                    if (current is not null)
                    {
                        text.Insert(0, '.');
                    }
                }

                if (currentNameSpace is not null)
                {
                    text.Insert(0, '.');
                    text.Insert(0, currentNameSpace);
                }
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfGreaterThanCapacity(uint length)
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
            return hash == other.hash && variableCount == other.variableCount && variables == other.variables;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            unchecked
            {
                return (int)hash;
            }
        }

        readonly void ISerializable.Write(BinaryWriter writer)
        {
            USpan<char> fullName = FullName;
            writer.WriteValue((ushort)fullName.Length);
            for (uint i = 0; i < fullName.Length; i++)
            {
                writer.WriteValue((byte)fullName[i]);
            }

            writer.WriteValue(size);
            writer.WriteValue(variableCount);
            for (byte i = 0; i < variableCount; i++)
            {
                writer.WriteObject(variables[i]);
            }
        }

        void ISerializable.Read(BinaryReader reader)
        {
            ushort fullNameLength = reader.ReadValue<ushort>();
            USpan<char> fullName = stackalloc char[fullNameLength];
            for (uint i = 0; i < fullNameLength; i++)
            {
                fullName[i] = (char)reader.ReadValue<byte>();
            }

            hash = TypeNames.Set(fullName);
            size = reader.ReadValue<ushort>();
            variableCount = reader.ReadValue<byte>();
            for (byte i = 0; i < variableCount; i++)
            {
                variables[i] = reader.ReadObject<Variable>();
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
        public struct Variable : IEquatable<Variable>, ISerializable
        {
            private long nameHash;
            private long typeFullNameHash;

            /// <summary>
            /// Name of the variable.
            /// </summary>
            public readonly USpan<char> Name => TypeNames.Get(nameHash);

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
            public Variable(FixedString name, FixedString fullTypeName)
            {
                this.nameHash = TypeNames.Set(name);
                typeFullNameHash = fullTypeName.GetLongHashCode();
            }

            /// <summary>
            /// Creates a new variable with the given <paramref name="name"/> and <paramref name="fullTypeName"/>.
            /// </summary>
            public Variable(string name, string fullTypeName)
            {
                this.nameHash = TypeNames.Set(name);
                typeFullNameHash = FixedString.GetLongHashCode(fullTypeName);
            }

            /// <summary>
            /// Creates a new variable with the given <paramref name="name"/> and <paramref name="typeHash"/>.
            /// </summary>
            public Variable(FixedString name, int typeHash)
            {
                this.nameHash = TypeNames.Set(name);
                this.typeFullNameHash = typeHash;
            }

            /// <inheritdoc/>
            public readonly override string ToString()
            {
                USpan<char> buffer = stackalloc char[256];
                uint length = ToString(buffer);
                return buffer.Slice(0, length).ToString();
            }

            /// <summary>
            /// Builds a string representation of this variable and writes it to <paramref name="buffer"/>.
            /// </summary>
            /// <returns>Amount of characters written.</returns>
            public readonly uint ToString(USpan<char> buffer)
            {
                TypeLayout typeLayout = Type;
                uint length = typeLayout.Name.CopyTo(buffer);
                buffer[length++] = '=';
                length += Name.CopyTo(buffer.Slice(length));
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
                    USpan<char> name = Name;
                    for (byte i = 0; i < name.Length; i++)
                    {
                        hashCode = hashCode * 31 + name[i];
                    }

                    hashCode = hashCode * 31 + (int)typeFullNameHash;
                    return hashCode;
                }
            }

            readonly void ISerializable.Write(BinaryWriter writer)
            {
                USpan<char> name = Name;
                writer.WriteValue((ushort)name.Length);
                for (byte i = 0; i < name.Length; i++)
                {
                    writer.WriteValue((byte)name[i]);
                }

                writer.WriteValue(typeFullNameHash);
            }

            void ISerializable.Read(BinaryReader reader)
            {
                ushort nameLength = reader.ReadValue<ushort>();
                USpan<char> name = stackalloc char[nameLength];
                for (byte i = 0; i < nameLength; i++)
                {
                    name[i] = (char)reader.ReadValue<byte>();
                }

                nameHash = TypeNames.Set(name);
                typeFullNameHash = reader.ReadValue<long>();
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
}