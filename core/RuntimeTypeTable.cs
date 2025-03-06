#if !NET
using System.Collections.Generic;
#endif

namespace System
{
    /// <summary>
    /// Table of <see cref="RuntimeTypeHandle"/>s.
    /// </summary>
    public static class RuntimeTypeTable
    {
#if !NET
        private static Dictionary<nint, RuntimeTypeHandle> table = new();
#else
        private static class HandleCache<T>
        {
            public static readonly RuntimeTypeHandle value = typeof(T).TypeHandle;
        }
#endif

        /// <summary>
        /// Retrieves the <see cref="RuntimeTypeHandle"/> for the given <paramref name="address"/>.
        /// </summary>
        public static RuntimeTypeHandle GetHandle(nint address)
        {
#if NET
            return RuntimeTypeHandle.FromIntPtr(address);
#else
            return table[address];
#endif
        }

        /// <summary>
        /// Retrieves the <see cref="RuntimeTypeHandle"/> for the given <paramref name="type"/>.
        /// </summary>
        public static RuntimeTypeHandle GetHandle(Type type)
        {
            RuntimeTypeHandle handle = type.TypeHandle;
#if !NET
            table[handle.Value] = handle;
#endif
            return handle;
        }

        /// <summary>
        /// Retrieves the <see cref="RuntimeTypeHandle"/> for the given <typeparamref name="T"/>.
        /// </summary>
        public static RuntimeTypeHandle GetHandle<T>()
        {
#if NET
            return HandleCache<T>.value;
#else
            return GetHandle(typeof(T));
#endif
        }

        /// <summary>
        /// Retrieves the address of the <see cref="RuntimeTypeHandle"/> for the given <paramref name="handle"/>.
        /// </summary>
        public static nint GetAddress(RuntimeTypeHandle handle)
        {
            return handle.Value;
        }

        /// <summary>
        /// Retrieves the address of the <see cref="RuntimeTypeHandle"/> for the given <typeparamref name="T"/>.
        /// </summary>
        public static nint GetAddress<T>()
        {
            return GetAddress(GetHandle<T>());
        }

        /// <summary>
        /// Retrieves the address of the <see cref="RuntimeTypeHandle"/> for the given <paramref name="type"/>.
        /// </summary>
        public static nint GetAddress(Type type)
        {
            return GetAddress(GetHandle(type));
        }
    }
}