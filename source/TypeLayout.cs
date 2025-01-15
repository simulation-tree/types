using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;

namespace Types
{
    [DebuggerTypeProxy(typeof(TypeLayoutDebugView))]
    public unsafe struct TypeLayout : IEquatable<TypeLayout>, ISerializable
    {
        private static readonly Dictionary<long, TypeLayout> nameToType = new();
        private static readonly List<Type> systemTypes = new();
        private static readonly List<TypeLayout> all = new();

        /// <summary>
        /// Maximum amount of variables per type.
        /// </summary>
        public const uint Capacity = 16;

        private FixedString fullName;
        private ushort size;
        private byte count;

        private fixed byte data[(int)(Capacity * 264)];

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
                for (int i = 0; i < systemTypes.Count; i++)
                {
                    if (all[i] == this)
                    {
                        return systemTypes[i];
                    }
                }

                throw new InvalidOperationException($"System type not found for {this}");
            }
        }

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
        [Obsolete("Default constructor not supported", true)]
        public TypeLayout()
        {
            throw new NotSupportedException();
        }
#endif

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
            int index = systemTypes.IndexOf(typeof(T));
            if (index >= 0)
            {
                return all[index] == this;
            }

            return false;
        }

        /// <summary>
        /// Creates an <see cref="object"/> instance of this type.
        /// </summary>
        public readonly object Create(USpan<byte> bytes)
        {
            return ObjectCreator.Create(this, bytes);
        }

        /// <summary>
        /// Creates an <see cref="object"/> instance of this type
        /// with default state.
        /// </summary>
        public readonly object Create()
        {
            USpan<byte> bytes = stackalloc byte[size];
            return Create(bytes);
        }

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
        /// Checks if <typeparamref name="T"/> is registered.
        /// </summary>
        public static bool IsRegistered<T>() where T : unmanaged
        {
            return systemTypes.Contains(typeof(T));
        }

        public static bool IsRegistered(FixedString fullTypeName)
        {
            return nameToType.ContainsKey(fullTypeName.GetLongHashCode());
        }

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

        public unsafe static void Register<T>() where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, stackalloc Variable[0]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.Initialize(layout);
        }

        public unsafe static void Register<T>(USpan<Variable> variables) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, variables);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.Initialize(layout);
        }

        public static TypeLayout Get<T>() where T : unmanaged
        {
            ThrowIfTypeIsNotRegistered<T>();

            return Cache<T>.Value;
        }

        public static TypeLayout Get(FixedString fullName)
        {
            ThrowIfTypeIsNotRegistered(fullName);

            return Get(fullName.GetLongHashCode());
        }

        public static TypeLayout Get(USpan<char> fullName)
        {
            return Get(new FixedString(fullName));
        }

        public static TypeLayout Get(string fullName)
        {
            return Get(new FixedString(fullName));
        }

        public static TypeLayout Get(long fullNameHash)
        {
            return nameToType[fullNameHash];
        }

        [Conditional("DEBUG")]
        public static void ThrowIfGreaterThanCapacity(uint length)
        {
            if (length >= Capacity)
            {
                throw new InvalidOperationException($"TypeLayout has reached its capacity of {Capacity} variables");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfTypeIsNotRegistered<T>()
        {
            if (!systemTypes.Contains(typeof(T)))
            {
                throw new InvalidOperationException($"TypeLayout for {typeof(T)} is not registered");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfAlreadyRegistered<T>()
        {
            if (systemTypes.Contains(typeof(T)))
            {
                throw new InvalidOperationException($"TypeLayout for {typeof(T)} is already registered");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfTypeIsNotRegistered(FixedString fullName)
        {
            if (!nameToType.ContainsKey(fullName.GetLongHashCode()))
            {
                throw new InvalidOperationException($"TypeLayout for {fullName} is not registered");
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

        public static bool operator ==(TypeLayout left, TypeLayout right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TypeLayout left, TypeLayout right)
        {
            return !(left == right);
        }

        [DebuggerTypeProxy(typeof(VariableDebugView))]
        public struct Variable : IEquatable<Variable>, ISerializable
        {
            private FixedString name;
            private long typeFullNameHash;

            public readonly FixedString Name => name;
            public readonly TypeLayout TypeLayout => Get(typeFullNameHash);
            public readonly ushort Size => TypeLayout.Size;

            public Variable(FixedString name, FixedString typeFullName)
            {
                this.name = name;
                typeFullNameHash = typeFullName.GetLongHashCode();
            }

            public Variable(string name, string typeFullName)
            {
                this.name = name;
                typeFullNameHash = new FixedString(typeFullName).GetLongHashCode();
            }

            public Variable(FixedString name, int typeFullNameHash)
            {
                this.name = name;
                this.typeFullNameHash = typeFullNameHash;
            }

            public readonly override string ToString()
            {
                USpan<char> buffer = stackalloc char[256];
                uint length = ToString(buffer);
                return buffer.Slice(0, length).ToString();
            }

            public readonly uint ToString(USpan<char> buffer)
            {
                TypeLayout typeLayout = TypeLayout;
                uint length = name.CopyTo(buffer);
                buffer[length++] = ' ';
                buffer[length++] = '(';
                length += typeLayout.Name.CopyTo(buffer.Slice(length));
                buffer[length++] = ')';
                return length;
            }

            public readonly override bool Equals(object? obj)
            {
                return obj is Variable variable && Equals(variable);
            }

            public readonly bool Equals(Variable other)
            {
                return Name.Equals(other.Name) && typeFullNameHash == other.typeFullNameHash;
            }

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

            void ISerializable.Write(BinaryWriter writer)
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

            public static bool operator ==(Variable left, Variable right)
            {
                return left.Equals(right);
            }

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
                    if (nameToType.TryGetValue(variable.typeFullNameHash, out TypeLayout layout))
                    {
                        typeFullName = layout.FullName.ToString();
                        typeName = layout.Name.ToString();
                        typeSize = layout.Size;
                    }
                    else
                    {
                        typeFullName = variable.typeFullNameHash.ToString();
                        typeName = "Unknown";
                    }
                }
            }
        }

        internal static class ObjectCreator
        {
            private static readonly Dictionary<TypeLayout, Func<USpan<byte>, object>> functions = new();

            public static void Set(TypeLayout type, Func<USpan<byte>, object> action)
            {
                functions[type] = action;
            }

            public static object Create(TypeLayout type, USpan<byte> bytes)
            {
                Func<USpan<byte>, object> action = functions[type];
                return action(bytes);
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

        internal static class Cache<T> where T : unmanaged
        {
            private static TypeLayout value;

            internal static TypeLayout Value => value;

            internal static void Initialize(TypeLayout value)
            {
                Cache<T>.value = value;
                ObjectCreator.Set(value, static (bytes) =>
                {
                    T instance = default;
                    void* ptr = &instance;
                    bytes.CopyTo(ptr, (uint)sizeof(T));
                    return instance;
                });
            }
        }
    }
}