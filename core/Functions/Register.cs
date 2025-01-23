using System;
using Unmanaged;

namespace Types.Functions
{
    /// <summary>
    /// Function to register a type.
    /// </summary>
    public unsafe readonly struct Register
    {
        private readonly delegate* unmanaged<Input, void> function;

        /// <inheritdoc/>
        public Register(delegate* unmanaged<Input, void> function)
        {
            this.function = function;
        }

        /// <summary>
        /// Registers a type with the given <paramref name="variables"/>.
        /// </summary>
        public readonly void Invoke<T>(USpan<TypeLayout.Variable> variables) where T : unmanaged
        {
            TypeLayout type = new(TypeLayout.GetFullName<T>(), (ushort)sizeof(T), variables);
            function(new(type, typeof(T).TypeHandle));
            TypeInstanceCreator.Initialize<T>();
        }

        /// <summary>
        /// Registers a type without variables specified.
        /// </summary>
        public readonly void Invoke<T>() where T : unmanaged
        {
            TypeLayout type = new(TypeLayout.GetFullName<T>(), (ushort)sizeof(T), []);
            function(new(type, typeof(T).TypeHandle));
            TypeInstanceCreator.Initialize<T>();
        }

        /// <summary>
        /// Input parameter.
        /// </summary>
        public readonly struct Input
        {
            /// <summary>
            /// Metadata of the type.
            /// </summary>
            public readonly TypeLayout type;

            private readonly nint handle;

            /// <summary>
            /// Handle of the registering type.
            /// </summary>
            public readonly RuntimeTypeHandle Handle => RuntimeTypeHandle.FromIntPtr(handle);

            /// <summary>
            /// Creates the input argument.
            /// </summary>
            public Input(TypeLayout type, RuntimeTypeHandle handle)
            {
                this.type = type;
                this.handle = handle.Value;
            }
        }
    }
}