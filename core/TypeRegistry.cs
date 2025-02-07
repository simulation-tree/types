using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Types.Functions;
using Unmanaged;

namespace Types
{
    /// <summary>
    /// Stores all types registered with a <see cref="ITypeBank"/>.
    /// </summary>
    public static class TypeRegistry
    {
        private static readonly List<TypeLayout> types = new();
        private static readonly Dictionary<RuntimeTypeHandle, TypeLayout> handleToType = new();
        private static readonly Dictionary<long, RuntimeTypeHandle> typeToHandle = new();
        private static readonly Dictionary<long, TypeLayout> hashToType = new();

        /// <summary>
        /// All registered type layouts.
        /// </summary>
        public static IReadOnlyCollection<TypeLayout> All => types;

        static unsafe TypeRegistry()
        {
            Register(new(TypeLayout.GetFullName<byte>(), sizeof(byte)), RuntimeTypeTable.GetHandle<byte>());
            Register(new(TypeLayout.GetFullName<sbyte>(), sizeof(sbyte)), RuntimeTypeTable.GetHandle<sbyte>());
            Register(new(TypeLayout.GetFullName<short>(), sizeof(short)), RuntimeTypeTable.GetHandle<short>());
            Register(new(TypeLayout.GetFullName<ushort>(), sizeof(ushort)), RuntimeTypeTable.GetHandle<ushort>());
            Register(new(TypeLayout.GetFullName<int>(), sizeof(int)), RuntimeTypeTable.GetHandle<int>());
            Register(new(TypeLayout.GetFullName<uint>(), sizeof(uint)), RuntimeTypeTable.GetHandle<uint>());
            Register(new(TypeLayout.GetFullName<long>(), sizeof(long)), RuntimeTypeTable.GetHandle<long>());
            Register(new(TypeLayout.GetFullName<ulong>(), sizeof(ulong)), RuntimeTypeTable.GetHandle<ulong>());
            Register(new(TypeLayout.GetFullName<float>(), sizeof(float)), RuntimeTypeTable.GetHandle<float>());
            Register(new(TypeLayout.GetFullName<double>(), sizeof(double)), RuntimeTypeTable.GetHandle<double>());
            Register(new(TypeLayout.GetFullName<char>(), sizeof(char)), RuntimeTypeTable.GetHandle<char>());
            Register(new(TypeLayout.GetFullName<bool>(), sizeof(bool)), RuntimeTypeTable.GetHandle<bool>());
            Register(new(TypeLayout.GetFullName<nint>(), (ushort)sizeof(nint)), RuntimeTypeTable.GetHandle<nint>());
            Register(new(TypeLayout.GetFullName<nuint>(), (ushort)sizeof(nuint)), RuntimeTypeTable.GetHandle<nuint>());
            Register(new(TypeLayout.GetFullName<FixedString>(), (ushort)sizeof(FixedString)), RuntimeTypeTable.GetHandle<FixedString>());

            USpan<TypeLayout.Variable> buffer = stackalloc TypeLayout.Variable[16];
            buffer[0] = new("x", TypeLayout.GetFullName<float>());
            buffer[1] = new("y", TypeLayout.GetFullName<float>());
            buffer[2] = new("z", TypeLayout.GetFullName<float>());
            buffer[3] = new("w", TypeLayout.GetFullName<float>());
            Register(new(TypeLayout.GetFullName<Vector2>(), (ushort)sizeof(Vector2), buffer.Slice(0, 2)), RuntimeTypeTable.GetHandle<Vector2>());
            Register(new(TypeLayout.GetFullName<Vector3>(), (ushort)sizeof(Vector3), buffer.Slice(0, 3)), RuntimeTypeTable.GetHandle<Vector3>());
            Register(new(TypeLayout.GetFullName<Vector4>(), (ushort)sizeof(Vector4), buffer.Slice(0, 4)), RuntimeTypeTable.GetHandle<Vector4>());
            Register(new(TypeLayout.GetFullName<Quaternion>(), (ushort)sizeof(Quaternion), buffer.Slice(0, 4)), RuntimeTypeTable.GetHandle<Quaternion>());
            buffer[0] = new("M11", TypeLayout.GetFullName<float>());
            buffer[1] = new("M12", TypeLayout.GetFullName<float>());
            buffer[2] = new("M21", TypeLayout.GetFullName<float>());
            buffer[3] = new("M22", TypeLayout.GetFullName<float>());
            buffer[4] = new("M31", TypeLayout.GetFullName<float>());
            buffer[5] = new("M32", TypeLayout.GetFullName<float>());
            Register(new(TypeLayout.GetFullName<Matrix3x2>(), (ushort)sizeof(Matrix3x2), buffer.Slice(0, 6)), RuntimeTypeTable.GetHandle<Matrix3x2>());
            buffer[0] = new("M11", TypeLayout.GetFullName<float>());
            buffer[1] = new("M12", TypeLayout.GetFullName<float>());
            buffer[2] = new("M13", TypeLayout.GetFullName<float>());
            buffer[3] = new("M14", TypeLayout.GetFullName<float>());
            buffer[4] = new("M21", TypeLayout.GetFullName<float>());
            buffer[5] = new("M22", TypeLayout.GetFullName<float>());
            buffer[6] = new("M23", TypeLayout.GetFullName<float>());
            buffer[7] = new("M24", TypeLayout.GetFullName<float>());
            buffer[8] = new("M31", TypeLayout.GetFullName<float>());
            buffer[9] = new("M32", TypeLayout.GetFullName<float>());
            buffer[10] = new("M33", TypeLayout.GetFullName<float>());
            buffer[11] = new("M34", TypeLayout.GetFullName<float>());
            buffer[12] = new("M41", TypeLayout.GetFullName<float>());
            buffer[13] = new("M42", TypeLayout.GetFullName<float>());
            buffer[14] = new("M43", TypeLayout.GetFullName<float>());
            buffer[15] = new("M44", TypeLayout.GetFullName<float>());
            Register(new(TypeLayout.GetFullName<Matrix4x4>(), (ushort)sizeof(Matrix4x4), buffer.Slice(0, 16)), RuntimeTypeTable.GetHandle<Matrix4x4>());
        }

        /// <summary>
        /// Loads all <see cref="TypeLayout"/>s from the bank of type <typeparamref name="T"/>.
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
        public static void Register(TypeLayout type, RuntimeTypeHandle handle)
        {
            long hash = type.Hash;
            types.Add(type);
            handleToType.Add(handle, type);
            typeToHandle.Add(hash, handle);
            hashToType.Add(hash, type);
        }

        /// <summary>
        /// Manually registers type <typeparamref name="T"/> without any variables.
        /// </summary>
        public unsafe static void Register<T>() where T : unmanaged
        {
            ushort size = (ushort)sizeof(T);
            TypeLayout type = new(TypeLayout.GetFullName<T>(), size);
            Register(type, RuntimeTypeTable.GetHandle<T>());
        }

        /// <summary>
        /// Retrieves the metadata for <typeparamref name="T"/>.
        /// </summary>
        public static TypeLayout Get<T>() where T : unmanaged
        {
            ThrowIfNotRegistered<T>();

            return Cache<T>.value;
        }

        /// <summary>
        /// Retrieves the metadata for the type with the given <paramref name="hash"/>.
        /// </summary>
        public static TypeLayout Get(long hash)
        {
            ThrowIfNotRegistered(hash);

            return hashToType[hash];
        }

        /// <summary>
        /// Tries to get the metadata for the type with the given <paramref name="hash"/>.
        /// </summary>
        public static bool TryGet(long hash, out TypeLayout type)
        {
            return hashToType.TryGetValue(hash, out type);
        }

        /// <summary>
        /// Retrieves the metadata for the <paramref name="handle"/> of the wanted type.
        /// </summary>
        public static TypeLayout Get(RuntimeTypeHandle handle)
        {
            ThrowIfNotRegistered(handle);

            return handleToType[handle];
        }

        /// <summary>
        /// Retrieves the raw handle for the <paramref name="hash"/>.
        /// </summary>
        public static RuntimeTypeHandle GetRuntimeTypeHandle(long hash)
        {
            ThrowIfNotRegistered(hash);

            return typeToHandle[hash];
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
        public static bool IsRegistered(Type type)
        {
            return handleToType.ContainsKey(RuntimeTypeTable.GetHandle(type));
        }

        /// <summary>
        /// Checks if a type with <paramref name="fullTypeName"/> is registered.
        /// </summary>
        public static bool IsRegistered(FixedString fullTypeName)
        {
            foreach (TypeLayout type in types)
            {
                if (type.FullName == fullTypeName)
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
                Type? type = Type.GetTypeFromHandle(handle);
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

        private static class Cache<T> where T : unmanaged
        {
            public static readonly TypeLayout value;

            static Cache()
            {
                if (!handleToType.TryGetValue(RuntimeTypeTable.GetHandle<T>(), out value))
                {
                    throw new InvalidOperationException($"Type `{typeof(T)}` is not registered");
                }
            }
        }
    }
}