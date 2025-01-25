using System;
using System.Diagnostics;
using Unmanaged;

namespace Types
{
    /// <summary>
    /// Describes metadata for a type.
    /// </summary>
    [DebuggerTypeProxy(typeof(TypeLayoutDebugView))]
    public unsafe struct TypeLayout : IEquatable<TypeLayout>, ISerializable
    {
        /// <summary>
        /// Maximum amount of variables per type.
        /// </summary>
        public const uint Capacity = 16;

        private FixedString fullName;
        private ushort size;
        private byte count;

        private fixed byte data[(int)(Capacity * 264)];

        /// <summary>
        /// All variables defined by this type.
        /// </summary>
        public readonly USpan<Variable> Variables
        {
            get
            {
                fixed (byte* ptr = data)
                {
                    return new(ptr, count);
                }
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
        public readonly RuntimeTypeHandle TypeHandle => TypeRegistry.GetRuntimeTypeHandle(this);

        /// <summary>
        /// Full name of the type including the namespace.
        /// </summary>
        public readonly FixedString FullName => fullName;

        /// <summary>
        /// Size of the type in bytes.
        /// </summary>
        public readonly ushort Size => size;

        /// <summary>
        /// Name of the type.
        /// </summary>
        public readonly FixedString Name
        {
            get
            {
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
        /// Creates a new type layout.
        /// </summary>
        public TypeLayout(FixedString fullName, ushort size, USpan<Variable> variables)
        {
            ThrowIfGreaterThanCapacity(variables.Length);

            this.fullName = fullName;
            this.size = size;
            count = (byte)variables.Length;
            variables.CopyTo(Variables);
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
            return fullName.CopyTo(destination);
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
        /// Checks if this type contains a variable with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool ContainsVariable(FixedString name)
        {
            USpan<Variable> variables = Variables;
            for (uint i = 0; i < variables.Length; i++)
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
            USpan<Variable> variables = Variables;
            for (uint i = 0; i < variables.Length; i++)
            {
                if (variables[i].Name.Equals(name))
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
            USpan<Variable> variables = Variables;
            for (uint i = 0; i < variables.Length; i++)
            {
                if (variables[i].Name.Equals(name))
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
            return fullName.Equals(other.fullName) && count == other.count && Variables.SequenceEqual(other.Variables);
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return fullName.GetHashCode();
        }

        readonly void ISerializable.Write(BinaryWriter writer)
        {
            //full name
            writer.WriteValue(fullName.Length);
            for (byte i = 0; i < fullName.Length; i++)
            {
                writer.WriteValue((byte)fullName[i]);
            }

            writer.WriteValue(size);

            //variables
            writer.WriteValue(count);
            USpan<Variable> variables = Variables;
            foreach (Variable variable in variables)
            {
                writer.WriteObject(variable);
            }
        }

        void ISerializable.Read(BinaryReader reader)
        {
            //full name
            byte fullNameLength = reader.ReadValue<byte>();
            fullName = default;
            fullName.Length = fullNameLength;
            for (byte i = 0; i < fullNameLength; i++)
            {
                fullName[i] = (char)reader.ReadValue<byte>();
            }

            size = reader.ReadValue<ushort>();

            //variables
            count = reader.ReadValue<byte>();
            USpan<Variable> variables = stackalloc Variable[count];
            for (byte i = 0; i < count; i++)
            {
                variables[i] = reader.ReadObject<Variable>();
            }

            fixed (byte* ptr = data)
            {
                variables.CopyTo(new(ptr, count));
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
        /// Describes a variable part of a <see cref="Types.TypeLayout"/>.
        /// </summary>
        [DebuggerTypeProxy(typeof(VariableDebugView))]
        public struct Variable : IEquatable<Variable>, ISerializable
        {
            private FixedString name;
            private long typeFullNameHash;

            /// <summary>
            /// Name of the variable.
            /// </summary>
            public readonly FixedString Name => name;

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
                this.name = name;
                typeFullNameHash = fullTypeName.GetLongHashCode();
            }

            /// <summary>
            /// Creates a new variable with the given <paramref name="name"/> and <paramref name="fullTypeName"/>.
            /// </summary>
            public Variable(string name, string fullTypeName)
            {
                this.name = name;
                typeFullNameHash = FixedString.GetLongHashCode(fullTypeName);
            }

            /// <summary>
            /// Creates a new variable with the given <paramref name="name"/> and <paramref name="typeHash"/>.
            /// </summary>
            public Variable(FixedString name, int typeHash)
            {
                this.name = name;
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
                uint length = name.CopyTo(buffer);
                buffer[length++] = ' ';
                buffer[length++] = '(';
                length += typeLayout.Name.CopyTo(buffer.Slice(length));
                buffer[length++] = ')';
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
                return Name.Equals(other.Name) && typeFullNameHash == other.typeFullNameHash;
            }

            /// <inheritdoc/>
            public readonly override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = 17;
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
                writer.WriteValue(name.Length);
                for (byte i = 0; i < name.Length; i++)
                {
                    writer.WriteValue((byte)name[i]);
                }

                writer.WriteValue(typeFullNameHash);
            }

            void ISerializable.Read(BinaryReader reader)
            {
                byte nameLength = reader.ReadValue<byte>();
                name = default;
                name.Length = nameLength;
                for (byte i = 0; i < nameLength; i++)
                {
                    name[i] = (char)reader.ReadValue<byte>();
                }

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

            internal class VariableDebugView
            {
                public readonly string name;
                public readonly string typeFullName;
                public readonly string typeName;
                public readonly ushort typeSize;

                public VariableDebugView(Variable variable)
                {
                    name = variable.Name.ToString();
                    try
                    {
                        TypeLayout type = TypeRegistry.Get(variable.typeFullNameHash);
                        typeFullName = type.FullName.ToString();
                        typeName = type.Name.ToString();
                        typeSize = type.Size;
                    }
                    catch
                    {
                        typeFullName = variable.typeFullNameHash.ToString();
                        typeName = "Unknown";
                    }
                }
            }
        }

        internal class TypeLayoutDebugView
        {
            public readonly string fullName;
            public readonly string name;
            public readonly ushort size;

            public TypeLayoutDebugView(TypeLayout layout)
            {
                fullName = layout.FullName.ToString();
                name = layout.Name.ToString();
                size = layout.Size;
            }
        }
    }
}