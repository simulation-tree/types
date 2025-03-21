using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Types.Functions;

namespace Types
{
    /// <summary>
    /// Stores metadata about types.
    /// </summary>
    public static class TypeRegistry
    {
        private static readonly List<Type> types = new();
        internal static readonly Dictionary<RuntimeTypeHandle, Type> handleToType = new();
        private static readonly Dictionary<long, RuntimeTypeHandle> typeToHandle = new();
        private static readonly Dictionary<long, Type> hashToType = new();

        /// <summary>
        /// All registered types.
        /// </summary>
        public static IReadOnlyCollection<Type> All => types;

        [SkipLocalsInit]
        static unsafe TypeRegistry()
        {
            Register(new(Type.GetFullName<byte>(), sizeof(byte)), RuntimeTypeTable.GetHandle<byte>());
            Register(new(Type.GetFullName<sbyte>(), sizeof(sbyte)), RuntimeTypeTable.GetHandle<sbyte>());
            Register(new(Type.GetFullName<short>(), sizeof(short)), RuntimeTypeTable.GetHandle<short>());
            Register(new(Type.GetFullName<ushort>(), sizeof(ushort)), RuntimeTypeTable.GetHandle<ushort>());
            Register(new(Type.GetFullName<int>(), sizeof(int)), RuntimeTypeTable.GetHandle<int>());
            Register(new(Type.GetFullName<uint>(), sizeof(uint)), RuntimeTypeTable.GetHandle<uint>());
            Register(new(Type.GetFullName<long>(), sizeof(long)), RuntimeTypeTable.GetHandle<long>());
            Register(new(Type.GetFullName<ulong>(), sizeof(ulong)), RuntimeTypeTable.GetHandle<ulong>());
            Register(new(Type.GetFullName<float>(), sizeof(float)), RuntimeTypeTable.GetHandle<float>());
            Register(new(Type.GetFullName<double>(), sizeof(double)), RuntimeTypeTable.GetHandle<double>());
            Register(new(Type.GetFullName<char>(), sizeof(char)), RuntimeTypeTable.GetHandle<char>());
            Register(new(Type.GetFullName<bool>(), sizeof(bool)), RuntimeTypeTable.GetHandle<bool>());
            Register(new(Type.GetFullName<nint>(), (ushort)sizeof(nint)), RuntimeTypeTable.GetHandle<nint>());
            Register(new(Type.GetFullName<nuint>(), (ushort)sizeof(nuint)), RuntimeTypeTable.GetHandle<nuint>());
#if NET
            Register(new(Type.GetFullName<Half>(), (ushort)sizeof(Half)), RuntimeTypeTable.GetHandle<Half>());
#endif

            FieldBuffer fields = new();
            TypeBuffer interfaces = new();
            fields[0] = new("x", Type.GetFullName<float>());
            fields[1] = new("y", Type.GetFullName<float>());
            fields[2] = new("z", Type.GetFullName<float>());
            fields[3] = new("w", Type.GetFullName<float>());
            Register(new(Type.GetFullName<Vector2>(), (ushort)sizeof(Vector2), fields, 2, interfaces, 0), RuntimeTypeTable.GetHandle<Vector2>());
            Register(new(Type.GetFullName<Vector3>(), (ushort)sizeof(Vector3), fields, 3, interfaces, 0), RuntimeTypeTable.GetHandle<Vector3>());
            Register(new(Type.GetFullName<Vector4>(), (ushort)sizeof(Vector4), fields, 4, interfaces, 0), RuntimeTypeTable.GetHandle<Vector4>());
            Register(new(Type.GetFullName<Quaternion>(), (ushort)sizeof(Quaternion), fields, 4, interfaces, 0), RuntimeTypeTable.GetHandle<Quaternion>());

            fields[0] = new("M11", Type.GetFullName<float>());
            fields[1] = new("M12", Type.GetFullName<float>());
            fields[2] = new("M21", Type.GetFullName<float>());
            fields[3] = new("M22", Type.GetFullName<float>());
            fields[4] = new("M31", Type.GetFullName<float>());
            fields[5] = new("M32", Type.GetFullName<float>());
            Register(new(Type.GetFullName<Matrix3x2>(), (ushort)sizeof(Matrix3x2), fields, 6, interfaces, 0), RuntimeTypeTable.GetHandle<Matrix3x2>());

            fields[0] = new("M11", Type.GetFullName<float>());
            fields[1] = new("M12", Type.GetFullName<float>());
            fields[2] = new("M13", Type.GetFullName<float>());
            fields[3] = new("M14", Type.GetFullName<float>());
            fields[4] = new("M21", Type.GetFullName<float>());
            fields[5] = new("M22", Type.GetFullName<float>());
            fields[6] = new("M23", Type.GetFullName<float>());
            fields[7] = new("M24", Type.GetFullName<float>());
            fields[8] = new("M31", Type.GetFullName<float>());
            fields[9] = new("M32", Type.GetFullName<float>());
            fields[10] = new("M33", Type.GetFullName<float>());
            fields[11] = new("M34", Type.GetFullName<float>());
            fields[12] = new("M41", Type.GetFullName<float>());
            fields[13] = new("M42", Type.GetFullName<float>());
            fields[14] = new("M43", Type.GetFullName<float>());
            fields[15] = new("M44", Type.GetFullName<float>());
            Register(new(Type.GetFullName<Matrix4x4>(), (ushort)sizeof(Matrix4x4), fields, 16, interfaces, 0), RuntimeTypeTable.GetHandle<Matrix4x4>());

            fields[0] = new("_dateData", Type.GetFullName<ulong>());
            Register(new(Type.GetFullName<DateTime>(), (ushort)sizeof(DateTime), fields, 1, interfaces, 0), RuntimeTypeTable.GetHandle<DateTime>());
        }

        /// <summary>
        /// Loads all <see cref="Type"/>s from the bank of type <typeparamref name="T"/>.
        /// </summary>
        public unsafe static void Load<T>() where T : unmanaged, ITypeBank
        {
            T bank = default;
            bank.Load(new(Register));
        }

        /// <summary>
        /// Registers a type using the information in the given <paramref name="input"/>.
        /// </summary>
        public static void Register(Register.Input input)
        {
            Register(input.type, input.Handle);
        }

        /// <summary>
        /// Manually registers the given <paramref name="type"/>.
        /// </summary>
        public static void Register(Type type, RuntimeTypeHandle handle)
        {
            ThrowIfAlreadyRegistered(type);

            types.Add(type);
            handleToType.Add(handle, type);
            typeToHandle.Add(type.Hash, handle);
            hashToType.Add(type.Hash, type);
        }

        /// <summary>
        /// Tries to manually register the given <paramref name="type"/>.
        /// </summary>
        public static bool TryRegister(Type type, RuntimeTypeHandle handle)
        {
            if (types.Contains(type))
            {
                return false;
            }

            types.Add(type);
            handleToType.Add(handle, type);
            typeToHandle.Add(type.Hash, handle);
            hashToType.Add(type.Hash, type);
            return true;
        }

        /// <summary>
        /// Manually registers type <typeparamref name="T"/> without any fields or interfaces.
        /// </summary>
        public unsafe static void Register<T>() where T : unmanaged
        {
            //todo: need to add a warning here when trying to register a type bank itself
            ushort size = (ushort)sizeof(T);
            Type type = new(Type.GetFullName<T>(), size);
            Register(type, RuntimeTypeTable.GetHandle<T>());
        }

        /// <summary>
        /// Tries to manually register type <typeparamref name="T"/> without any fields or interfaces.
        /// </summary>
        public unsafe static bool TryRegister<T>() where T : unmanaged
        {
            ushort size = (ushort)sizeof(T);
            Type type = new(Type.GetFullName<T>(), size);
            return TryRegister(type, RuntimeTypeTable.GetHandle<T>());
        }

        /// <summary>
        /// Retrieves the metadata for <typeparamref name="T"/>.
        /// </summary>
        public static Type Get<T>() where T : unmanaged
        {
            ThrowIfNotRegistered<T>();

            return Cache<T>.value;
        }

        /// <summary>
        /// Retrieves the metadata for <typeparamref name="T"/>, or registers
        /// it if it's not already registered without any variables.
        /// </summary>
        public static Type GetOrRegister<T>() where T : unmanaged
        {
            return LazyCache<T>.value;
        }

        /// <summary>
        /// Retrieves the metadata for the type with the given <paramref name="typeHash"/>.
        /// </summary>
        public static Type Get(long typeHash)
        {
            ThrowIfNotRegistered(typeHash);

            return hashToType[typeHash];
        }

        /// <summary>
        /// Tries to get the metadata for the type with the given <paramref name="typeHash"/>.
        /// </summary>
        public static bool TryGet(long typeHash, out Type type)
        {
            return hashToType.TryGetValue(typeHash, out type);
        }

        /// <summary>
        /// Retrieves the metadata for the <paramref name="handle"/> of the wanted type.
        /// </summary>
        public static Type Get(RuntimeTypeHandle handle)
        {
            ThrowIfNotRegistered(handle);

            return handleToType[handle];
        }

        /// <summary>
        /// Retrieves the raw handle for the <paramref name="typeHash"/>.
        /// </summary>
        public static RuntimeTypeHandle GetRuntimeTypeHandle(long typeHash)
        {
            ThrowIfNotRegistered(typeHash);

            return typeToHandle[typeHash];
        }

        /// <summary>
        /// Checks if type <typeparamref name="T"/> is registered.
        /// </summary>
        public static bool IsRegistered<T>() where T : unmanaged
        {
            return handleToType.ContainsKey(RuntimeTypeTable.GetHandle<T>());
        }

        /// <summary>
        /// Checks if the given <paramref name="type"/> is registered.
        /// </summary>
        public static bool IsRegistered(System.Type type)
        {
            return handleToType.ContainsKey(RuntimeTypeTable.GetHandle(type));
        }

        /// <summary>
        /// Checks if a type with <paramref name="fullTypeName"/> is registered.
        /// </summary>
        public static bool IsRegistered(ReadOnlySpan<char> fullTypeName)
        {
            long hash = fullTypeName.GetLongHashCode();
            foreach (Type type in types)
            {
                if (type.Hash == hash)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a type with <paramref name="fullTypeName"/> is registered.
        /// </summary>
        public static bool IsRegistered(string fullTypeName)
        {
            long hash = fullTypeName.GetLongHashCode();
            foreach (Type type in types)
            {
                if (type.Hash == hash)
                {
                    return true;
                }
            }

            return false;
        }

        [Conditional("DEBUG")]
        private static void ThrowIfNotRegistered<T>() where T : unmanaged
        {
            if (!IsRegistered<T>())
            {
                throw new InvalidOperationException($"Type `{typeof(T)}` is not registered");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfNotRegistered(long hash)
        {
            if (!hashToType.ContainsKey(hash))
            {
                throw new InvalidOperationException($"Type with hash `{hash}` is not registered");
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
        private static void ThrowIfAlreadyRegistered(Type type)
        {
            if (types.Contains(type))
            {
                throw new InvalidOperationException($"Type `{type}` is already registered");
            }
        }

        private static class Cache<T> where T : unmanaged
        {
            public static readonly Type value;

            static Cache()
            {
                if (!handleToType.TryGetValue(RuntimeTypeTable.GetHandle<T>(), out value))
                {
                    throw new InvalidOperationException($"Type `{typeof(T)}` is not registered");
                }
            }
        }

        private unsafe static class LazyCache<T> where T : unmanaged
        {
            public static readonly Type value;

            static LazyCache()
            {
                RuntimeTypeHandle key = RuntimeTypeTable.GetHandle<T>();
                if (!handleToType.TryGetValue(key, out value))
                {
                    value = new(Type.GetFullName<T>(), (ushort)sizeof(T));
                    Register(value, key);
                }
            }
        }
    }
}