using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
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
        private static readonly Dictionary<TypeLayout, RuntimeTypeHandle> typeToHandle = new();
        private static readonly Dictionary<long, TypeLayout> hashToType = new();

        /// <summary>
        /// All registered type layouts.
        /// </summary>
        public static IReadOnlyCollection<TypeLayout> All => types;

        static TypeRegistry()
        {
            Register<BuiltInTypeBank>();
        }

        /// <summary>
        /// Registers all <see cref="TypeLayout"/>s from the bank of type <typeparamref name="T"/>.
        /// </summary>
        public unsafe static void Register<T>() where T : unmanaged, ITypeBank
        {
            T bank = default;
            bank.Load(new(&Register));
        }

        [UnmanagedCallersOnly]
        private static void Register(Register.Input input)
        {
            types.Add(input.type);

            RuntimeTypeHandle handle = input.Handle;
            handleToType.Add(handle, input.type);
            typeToHandle.Add(input.type, handle);
            hashToType.Add(input.type.FullName.GetLongHashCode(), input.type);
        }

        /// <summary>
        /// Retrieves the type metadata for <typeparamref name="T"/>.
        /// </summary>
        public static TypeLayout Get<T>() where T : unmanaged
        {
            ThrowIfNotRegistered<T>();

            return Cache<T>.value;
        }

        /// <summary>
        /// Retrieves the type metadata for the type with the given <paramref name="hash"/>.
        /// </summary>
        public static TypeLayout Get(long hash)
        {
            ThrowIfNotRegistered(hash);

            return hashToType[hash];
        }

        /// <summary>
        /// Retrieves the raw handle for the <paramref name="type"/>.
        /// </summary>
        public static RuntimeTypeHandle GetRuntimeTypeHandle(TypeLayout type)
        {
            ThrowIfNotRegistered(type);

            return typeToHandle[type];
        }

        /// <summary>
        /// Checks if type <typeparamref name="T"/> is registered.
        /// </summary>
        public static bool IsRegistered<T>() where T : unmanaged
        {
            return handleToType.ContainsKey(typeof(T).TypeHandle);
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
        private static void ThrowIfNotRegistered(TypeLayout type)
        {
            if (!typeToHandle.ContainsKey(type))
            {
                throw new InvalidOperationException($"Type `{type}` is not registered");
            }
        }

        private static class Cache<T> where T : unmanaged
        {
            public static readonly TypeLayout value = handleToType[typeof(T).TypeHandle];
        }

        private readonly struct BuiltInTypeBank : ITypeBank
        {
            void ITypeBank.Load(Register register)
            {
                register.Invoke<byte>();
                register.Invoke<sbyte>();
                register.Invoke<short>();
                register.Invoke<ushort>();
                register.Invoke<int>();
                register.Invoke<uint>();
                register.Invoke<long>();
                register.Invoke<ulong>();
                register.Invoke<float>();
                register.Invoke<double>();
                register.Invoke<char>();
                register.Invoke<bool>();
                register.Invoke<nint>();
                register.Invoke<nuint>();
                register.Invoke<FixedString>();
                register.Invoke<Vector2>();
                register.Invoke<Vector3>();
                register.Invoke<Vector4>();
                register.Invoke<Quaternion>();
                register.Invoke<Matrix3x2>();
                register.Invoke<Matrix4x4>();
            }
        }
    }
}