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

        static unsafe TypeRegistry()
        {
            Register(new(TypeLayout.GetFullName<byte>(), sizeof(byte), []), typeof(byte).TypeHandle);
            Register(new(TypeLayout.GetFullName<sbyte>(), sizeof(sbyte), []), typeof(sbyte).TypeHandle);
            Register(new(TypeLayout.GetFullName<short>(), sizeof(short), []), typeof(short).TypeHandle);
            Register(new(TypeLayout.GetFullName<ushort>(), sizeof(ushort), []), typeof(ushort).TypeHandle);
            Register(new(TypeLayout.GetFullName<int>(), sizeof(int), []), typeof(int).TypeHandle);
            Register(new(TypeLayout.GetFullName<uint>(), sizeof(uint), []), typeof(uint).TypeHandle);
            Register(new(TypeLayout.GetFullName<long>(), sizeof(long), []), typeof(long).TypeHandle);
            Register(new(TypeLayout.GetFullName<ulong>(), sizeof(ulong), []), typeof(ulong).TypeHandle);
            Register(new(TypeLayout.GetFullName<float>(), sizeof(float), []), typeof(float).TypeHandle);
            Register(new(TypeLayout.GetFullName<double>(), sizeof(double), []), typeof(double).TypeHandle);
            Register(new(TypeLayout.GetFullName<char>(), sizeof(char), []), typeof(char).TypeHandle);
            Register(new(TypeLayout.GetFullName<bool>(), sizeof(bool), []), typeof(bool).TypeHandle);
            Register(new(TypeLayout.GetFullName<nint>(), (ushort)sizeof(nint), []), typeof(nint).TypeHandle);
            Register(new(TypeLayout.GetFullName<nuint>(), (ushort)sizeof(nuint), []), typeof(nuint).TypeHandle);
            Register(new(TypeLayout.GetFullName<FixedString>(), (ushort)sizeof(FixedString), []), typeof(FixedString).TypeHandle);
            Register(new(TypeLayout.GetFullName<Vector2>(), (ushort)sizeof(Vector2), [new("x", TypeLayout.GetFullName<float>()), new("y", TypeLayout.GetFullName<float>())]), typeof(Vector2).TypeHandle);
            Register(new(TypeLayout.GetFullName<Vector3>(), (ushort)sizeof(Vector3), [new("x", TypeLayout.GetFullName<float>()), new("y", TypeLayout.GetFullName<float>()), new("z", TypeLayout.GetFullName<float>())]), typeof(Vector3).TypeHandle);
            Register(new(TypeLayout.GetFullName<Vector4>(), (ushort)sizeof(Vector4), [new("x", TypeLayout.GetFullName<float>()), new("y", TypeLayout.GetFullName<float>()), new("z", TypeLayout.GetFullName<float>()), new("w", TypeLayout.GetFullName<float>())]), typeof(Vector4).TypeHandle);
            Register(new(TypeLayout.GetFullName<Quaternion>(), (ushort)sizeof(Quaternion), [new("x", TypeLayout.GetFullName<float>()), new("y", TypeLayout.GetFullName<float>()), new("z", TypeLayout.GetFullName<float>()), new("w", TypeLayout.GetFullName<float>())]), typeof(Quaternion).TypeHandle);
            Register(new(TypeLayout.GetFullName<Matrix3x2>(), (ushort)sizeof(Matrix3x2), [new("M11", TypeLayout.GetFullName<float>()), new("M12", TypeLayout.GetFullName<float>()), new("M21", TypeLayout.GetFullName<float>()), new("M22", TypeLayout.GetFullName<float>()), new("M31", TypeLayout.GetFullName<float>()), new("M32", TypeLayout.GetFullName<float>())]), typeof(Matrix3x2).TypeHandle);
            Register(new(TypeLayout.GetFullName<Matrix4x4>(), (ushort)sizeof(Matrix4x4), [new("M11", TypeLayout.GetFullName<float>()), new("M12", TypeLayout.GetFullName<float>()), new("M13", TypeLayout.GetFullName<float>()), new("M14", TypeLayout.GetFullName<float>()), new("M21", TypeLayout.GetFullName<float>()), new("M22", TypeLayout.GetFullName<float>()), new("M23", TypeLayout.GetFullName<float>()), new("M24", TypeLayout.GetFullName<float>()), new("M31", TypeLayout.GetFullName<float>()), new("M32", TypeLayout.GetFullName<float>()), new("M33", TypeLayout.GetFullName<float>()), new("M34", TypeLayout.GetFullName<float>()), new("M41", TypeLayout.GetFullName<float>()), new("M42", TypeLayout.GetFullName<float>()), new("M43", TypeLayout.GetFullName<float>()), new("M44", TypeLayout.GetFullName<float>())]), typeof(Matrix4x4).TypeHandle);
        }

        /// <summary>
        /// Loads all <see cref="TypeLayout"/>s from the bank of type <typeparamref name="T"/>.
        /// </summary>
        public unsafe static void Load<T>() where T : unmanaged, ITypeBank
        {
            T bank = default;
            bank.Load(new(&Register));
        }

        [UnmanagedCallersOnly]
        private static void Register(Register.Input input)
        {
            Register(input.type, input.Handle);
        }

        private static void Register(TypeLayout type, RuntimeTypeHandle handle)
        {
            types.Add(type);
            handleToType.Add(handle, type);
            typeToHandle.Add(type, handle);
            hashToType.Add(type.FullName.GetLongHashCode(), type);
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
        /// Retrieves the type metadata for the <paramref name="handle"/> of the wanted type.
        /// </summary>
        public static TypeLayout Get(RuntimeTypeHandle handle)
        {
            ThrowIfNotRegistered(handle);

            return handleToType[handle];
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
            public static readonly TypeLayout value = handleToType[typeof(T).TypeHandle];
        }
    }
}