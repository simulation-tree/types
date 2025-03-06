using System;

namespace Types.Functions
{
    /// <summary>
    /// Function to register a type.
    /// </summary>
    public readonly struct Register
    {
        private readonly Action<Input> action;

        internal Register(Action<Input> action)
        {
            this.action = action;
        }

        /// <summary>
        /// Registers a type with the given <paramref name="variables"/>.
        /// </summary>
        public unsafe readonly void Invoke<T>(ReadOnlySpan<TypeLayout.Variable> variables) where T : unmanaged
        {
            TypeLayout type = new(TypeLayout.GetFullName<T>(), (ushort)sizeof(T), variables);
            Input input = new(type, RuntimeTypeTable.GetHandle<T>());
            action(input);
            TypeInstanceCreator.Initialize<T>(type);
        }

        /// <summary>
        /// Registers a type without variables specified.
        /// </summary>
        public unsafe readonly void Invoke<T>() where T : unmanaged
        {
            TypeLayout type = new(TypeLayout.GetFullName<T>(), (ushort)sizeof(T));
            Input input = new(type, RuntimeTypeTable.GetHandle<T>());
            action(input);
            TypeInstanceCreator.Initialize<T>(type);
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
            public readonly RuntimeTypeHandle Handle => RuntimeTypeTable.GetHandle(handle);

            /// <summary>
            /// Creates the input argument.
            /// </summary>
            public Input(TypeLayout type, RuntimeTypeHandle handle)
            {
                this.type = type;
                this.handle = RuntimeTypeTable.GetAddress(handle);
            }
        }
    }
}