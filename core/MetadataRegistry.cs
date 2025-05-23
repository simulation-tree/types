using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Types
{
    /// <summary>
    /// Stores metadata about types and interfaces.
    /// </summary>
    public static class MetadataRegistry
    {
        private static readonly List<TypeMetadata> types = new();
        private static readonly List<Interface> interfaces = new();
        internal static readonly Dictionary<RuntimeTypeHandle, TypeMetadata> handleToType = new();
        internal static readonly Dictionary<RuntimeTypeHandle, Interface> handleToInterface = new();
        private static readonly Dictionary<long, RuntimeTypeHandle> typeToHandle = new();
        private static readonly Dictionary<long, RuntimeTypeHandle> interfaceToHandle = new();
        private static readonly Dictionary<long, TypeMetadata> hashToType = new();
        private static readonly Dictionary<long, Interface> hashToInterface = new();

        /// <summary>
        /// All registered types.
        /// </summary>
        public static IReadOnlyList<TypeMetadata> Types => types;

        /// <summary>
        /// All registered interfaces.
        /// </summary>
        public static IReadOnlyList<Interface> Interfaces => interfaces;

        static unsafe MetadataRegistry()
        {
            RegisterType(new(GetFullName<byte>(), sizeof(byte)), RuntimeTypeTable.GetHandle<byte>());
            RegisterType(new(GetFullName<sbyte>(), sizeof(sbyte)), RuntimeTypeTable.GetHandle<sbyte>());
            RegisterType(new(GetFullName<short>(), sizeof(short)), RuntimeTypeTable.GetHandle<short>());
            RegisterType(new(GetFullName<ushort>(), sizeof(ushort)), RuntimeTypeTable.GetHandle<ushort>());
            RegisterType(new(GetFullName<int>(), sizeof(int)), RuntimeTypeTable.GetHandle<int>());
            RegisterType(new(GetFullName<uint>(), sizeof(uint)), RuntimeTypeTable.GetHandle<uint>());
            RegisterType(new(GetFullName<long>(), sizeof(long)), RuntimeTypeTable.GetHandle<long>());
            RegisterType(new(GetFullName<ulong>(), sizeof(ulong)), RuntimeTypeTable.GetHandle<ulong>());
            RegisterType(new(GetFullName<float>(), sizeof(float)), RuntimeTypeTable.GetHandle<float>());
            RegisterType(new(GetFullName<double>(), sizeof(double)), RuntimeTypeTable.GetHandle<double>());
            RegisterType(new(GetFullName<char>(), sizeof(char)), RuntimeTypeTable.GetHandle<char>());
            RegisterType(new(GetFullName<bool>(), sizeof(bool)), RuntimeTypeTable.GetHandle<bool>());
            RegisterType(new(GetFullName<nint>(), (ushort)sizeof(nint)), RuntimeTypeTable.GetHandle<nint>());
            RegisterType(new(GetFullName<nuint>(), (ushort)sizeof(nuint)), RuntimeTypeTable.GetHandle<nuint>());
#if NET
            RegisterType(new(GetFullName<Half>(), (ushort)sizeof(Half)), RuntimeTypeTable.GetHandle<Half>());
#endif

            FieldBuffer fields = new();
            InterfaceBuffer interfaces = new();
            fields[0] = new("x", GetFullName<float>());
            fields[1] = new("y", GetFullName<float>());
            fields[2] = new("z", GetFullName<float>());
            fields[3] = new("w", GetFullName<float>());
            RegisterType(new(GetFullName<Vector2>(), (ushort)sizeof(Vector2), fields, 2, interfaces, 0), RuntimeTypeTable.GetHandle<Vector2>());
            RegisterType(new(GetFullName<Vector3>(), (ushort)sizeof(Vector3), fields, 3, interfaces, 0), RuntimeTypeTable.GetHandle<Vector3>());
            RegisterType(new(GetFullName<Vector4>(), (ushort)sizeof(Vector4), fields, 4, interfaces, 0), RuntimeTypeTable.GetHandle<Vector4>());
            RegisterType(new(GetFullName<Quaternion>(), (ushort)sizeof(Quaternion), fields, 4, interfaces, 0), RuntimeTypeTable.GetHandle<Quaternion>());

            fields[0] = new("M11", GetFullName<float>());
            fields[1] = new("M12", GetFullName<float>());
            fields[2] = new("M21", GetFullName<float>());
            fields[3] = new("M22", GetFullName<float>());
            fields[4] = new("M31", GetFullName<float>());
            fields[5] = new("M32", GetFullName<float>());
            RegisterType(new(GetFullName<Matrix3x2>(), (ushort)sizeof(Matrix3x2), fields, 6, interfaces, 0), RuntimeTypeTable.GetHandle<Matrix3x2>());

            fields[0] = new("M11", GetFullName<float>());
            fields[1] = new("M12", GetFullName<float>());
            fields[2] = new("M13", GetFullName<float>());
            fields[3] = new("M14", GetFullName<float>());
            fields[4] = new("M21", GetFullName<float>());
            fields[5] = new("M22", GetFullName<float>());
            fields[6] = new("M23", GetFullName<float>());
            fields[7] = new("M24", GetFullName<float>());
            fields[8] = new("M31", GetFullName<float>());
            fields[9] = new("M32", GetFullName<float>());
            fields[10] = new("M33", GetFullName<float>());
            fields[11] = new("M34", GetFullName<float>());
            fields[12] = new("M41", GetFullName<float>());
            fields[13] = new("M42", GetFullName<float>());
            fields[14] = new("M43", GetFullName<float>());
            fields[15] = new("M44", GetFullName<float>());
            RegisterType(new(GetFullName<Matrix4x4>(), (ushort)sizeof(Matrix4x4), fields, 16, interfaces, 0), RuntimeTypeTable.GetHandle<Matrix4x4>());

            fields[0] = new("_dateData", GetFullName<ulong>());
            RegisterType(new(GetFullName<DateTime>(), (ushort)sizeof(DateTime), fields, 1, interfaces, 0), RuntimeTypeTable.GetHandle<DateTime>());
        }

        /// <summary>
        /// Loads all <see cref="TypeMetadata"/>s from the bank of type <typeparamref name="T"/>.
        /// </summary>
        public static void Load<T>() where T : unmanaged, IMetadataBank
        {
            T bank = default;
            bank.Load(new());
        }

        /// <summary>
        /// Manually registers the given <paramref name="type"/>.
        /// </summary>
        public static void RegisterType(TypeMetadata type, RuntimeTypeHandle handle)
        {
            ThrowIfAlreadyRegistered(type);

            types.Add(type);
            handleToType.Add(handle, type);
            typeToHandle.Add(type.hash, handle);
            hashToType.Add(type.hash, type);
        }

        /// <summary>
        /// Manually registers the given <paramref name="interfaceValue"/>.
        /// </summary>
        public static void RegisterInterface(Interface interfaceValue, RuntimeTypeHandle handle)
        {
            ThrowIfAlreadyRegistered(interfaceValue);

            interfaces.Add(interfaceValue);
            handleToInterface.Add(handle, interfaceValue);
            interfaceToHandle.Add(interfaceValue.Hash, handle);
            hashToInterface.Add(interfaceValue.Hash, interfaceValue);
        }

        /// <summary>
        /// Tries to manually register the given <paramref name="type"/>.
        /// </summary>
        public static bool TryRegisterType(TypeMetadata type, RuntimeTypeHandle handle)
        {
            if (types.Contains(type))
            {
                return false;
            }

            types.Add(type);
            handleToType.Add(handle, type);
            typeToHandle.Add(type.hash, handle);
            hashToType.Add(type.hash, type);
            return true;
        }

        /// <summary>
        /// Manually registers type <typeparamref name="T"/> without any fields or interfaces.
        /// </summary>
        public unsafe static void RegisterType<T>() where T : unmanaged
        {
            ushort size = (ushort)sizeof(T);
            TypeMetadata type = new(GetFullName<T>(), size);
            RegisterType(type, RuntimeTypeTable.GetHandle<T>());
        }

        /// <summary>
        /// Manually registers the interface of type <typeparamref name="T"/>.
        /// </summary>
        public static void RegisterInterface<T>()
        {
            Interface interfaceValue = new(GetFullName<T>());
            RegisterInterface(interfaceValue, RuntimeTypeTable.GetHandle<T>());
        }

        /// <summary>
        /// Tries to manually register type <typeparamref name="T"/> without any fields or interfaces.
        /// </summary>
        public unsafe static bool TryRegisterType<T>() where T : unmanaged
        {
            ushort size = (ushort)sizeof(T);
            TypeMetadata type = new(GetFullName<T>(), size);
            return TryRegisterType(type, RuntimeTypeTable.GetHandle<T>());
        }

        /// <summary>
        /// Retrieves the metadata for <typeparamref name="T"/>.
        /// </summary>
        public static TypeMetadata GetType<T>() where T : unmanaged
        {
            ThrowIfTypeNotRegistered<T>();

            return TypeCache<T>.value;
        }

        /// <summary>
        /// Retrieves the metadata for <typeparamref name="T"/>.
        /// </summary>
        public static Interface GetInterface<T>() where T : unmanaged
        {
            ThrowIfTypeNotRegistered<T>();

            return InterfaceCache<T>.value;
        }

        /// <summary>
        /// Retrieves the metadata for <typeparamref name="T"/>, or registers
        /// it if it's not already registered without any variables.
        /// </summary>
        public static TypeMetadata GetOrRegisterType<T>() where T : unmanaged
        {
            return LazyTypeCache<T>.value;
        }

        /// <summary>
        /// Retrieves the metadata for the type with the given <paramref name="typeHash"/>.
        /// </summary>
        public static Interface GetInterface(long typeHash)
        {
            ThrowIfTypeNotRegistered(typeHash);

            return hashToInterface[typeHash];
        }

        /// <summary>
        /// Tries to get the metadata for the type with the given <paramref name="typeHash"/>.
        /// </summary>
        public static bool TryGetType(long typeHash, out TypeMetadata type)
        {
            return hashToType.TryGetValue(typeHash, out type);
        }

        /// <summary>
        /// Retrieves the metadata for the <paramref name="handle"/> of the wanted type.
        /// </summary>
        public static TypeMetadata GetType(RuntimeTypeHandle handle)
        {
            ThrowIfNotRegistered(handle);

            return handleToType[handle];
        }

        /// <summary>
        /// Retrieves the raw handle for the <paramref name="typeHash"/>.
        /// </summary>
        public static RuntimeTypeHandle GetRuntimeTypeHandle(long typeHash)
        {
            ThrowIfTypeNotRegistered(typeHash);

            return typeToHandle[typeHash];
        }

        /// <summary>
        /// Retrieves the raw handle for the <paramref name="typeHash"/>.
        /// </summary>
        public static RuntimeTypeHandle GetRuntimeInterfaceHandle(long typeHash)
        {
            ThrowIfInterfaceNotRegistered(typeHash);

            return interfaceToHandle[typeHash];
        }

        /// <summary>
        /// Checks if type <typeparamref name="T"/> is registered.
        /// </summary>
        public static bool IsTypeRegistered<T>() where T : unmanaged
        {
            return handleToType.ContainsKey(RuntimeTypeTable.GetHandle<T>());
        }

        /// <summary>
        /// Checks if type <typeparamref name="T"/> is registered.
        /// </summary>
        public static bool IsInterfaceRegistered<T>()
        {
            return handleToInterface.ContainsKey(RuntimeTypeTable.GetHandle<T>());
        }

        /// <summary>
        /// Checks if the given <paramref name="type"/> is registered.
        /// </summary>
        public static bool IsTypeRegistered(Type type)
        {
            return handleToType.ContainsKey(RuntimeTypeTable.GetHandle(type));
        }

        /// <summary>
        /// Checks if the given <paramref name="interfaceType"/> is registered.
        /// </summary>
        public static bool IsInterfaceRegistered(Type interfaceType)
        {
            return handleToInterface.ContainsKey(RuntimeTypeTable.GetHandle(interfaceType));
        }

        /// <summary>
        /// Checks if a type with <paramref name="fullTypeName"/> is registered.
        /// </summary>
        public static bool IsTypeRegistered(ReadOnlySpan<char> fullTypeName)
        {
            long hash = fullTypeName.GetLongHashCode();
            foreach (TypeMetadata type in types)
            {
                if (type.hash == hash)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a type with <paramref name="fullTypeName"/> is registered.
        /// </summary>
        public static bool IsTypeRegistered(string fullTypeName)
        {
            long hash = fullTypeName.GetLongHashCode();
            foreach (TypeMetadata type in types)
            {
                if (type.hash == hash)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if an interface type with <paramref name="fullTypeName"/> is registered.
        /// </summary>
        public static bool IsInterfaceRegistered(ReadOnlySpan<char> fullTypeName)
        {
            long hash = fullTypeName.GetLongHashCode();
            foreach (Interface interfaceType in interfaces)
            {
                if (interfaceType.Hash == hash)
                {
                    return true;
                }
            }

            return false;
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
        private static void ThrowIfTypeNotRegistered<T>() where T : unmanaged
        {
            if (!IsTypeRegistered<T>())
            {
                throw new InvalidOperationException($"Type `{typeof(T)}` is not registered");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfTypeNotRegistered(long hash)
        {
            if (!hashToType.ContainsKey(hash))
            {
                throw new InvalidOperationException($"Type with hash `{hash}` is not registered");
            }
        }


        [Conditional("DEBUG")]
        private static void ThrowIfInterfaceNotRegistered(long hash)
        {
            if (!hashToInterface.ContainsKey(hash))
            {
                throw new InvalidOperationException($"Interface with hash `{hash}` is not registered");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfNotRegistered(RuntimeTypeHandle handle)
        {
            if (!handleToType.ContainsKey(handle))
            {
                System.Type? type = System.Type.GetTypeFromHandle(handle);
                if (type is not null)
                {
                    throw new InvalidOperationException($"Type `{type}` is not registered");
                }
                else
                {
                    throw new InvalidOperationException($"Type with handle `{handle}` is not registered");
                }
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfAlreadyRegistered(TypeMetadata type)
        {
            if (types.Contains(type))
            {
                throw new InvalidOperationException($"Type `{type}` is already registered");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfAlreadyRegistered(Interface interfaceValue)
        {
            if (interfaces.Contains(interfaceValue))
            {
                throw new InvalidOperationException($"Interface `{interfaceValue}` is already registered");
            }
        }

        private static class TypeCache<T> where T : unmanaged
        {
            public static readonly TypeMetadata value;

            static TypeCache()
            {
                if (!handleToType.TryGetValue(RuntimeTypeTable.GetHandle<T>(), out value))
                {
                    throw new InvalidOperationException($"Type `{typeof(T)}` is not registered");
                }
            }
        }

        private static class InterfaceCache<T>
        {
            public static readonly Interface value;

            static InterfaceCache()
            {
                if (!handleToInterface.TryGetValue(RuntimeTypeTable.GetHandle<T>(), out value))
                {
                    throw new InvalidOperationException($"Interface `{typeof(T)}` is not registered");
                }
            }
        }

        private unsafe static class LazyTypeCache<T> where T : unmanaged
        {
            public static readonly TypeMetadata value;

            static LazyTypeCache()
            {
                RuntimeTypeHandle key = RuntimeTypeTable.GetHandle<T>();
                if (!handleToType.TryGetValue(key, out value))
                {
                    value = new(GetFullName<T>(), (ushort)sizeof(T));
                    RegisterType(value, key);
                }
            }
        }

        private unsafe static class LazyInterfaceCache<T> where T : unmanaged
        {
            public static readonly Interface value;

            static LazyInterfaceCache()
            {
                RuntimeTypeHandle key = RuntimeTypeTable.GetHandle<T>();
                if (!handleToInterface.TryGetValue(key, out value))
                {
                    value = new(GetFullName<T>());
                    RegisterInterface(value, key);
                }
            }
        }
    }
}