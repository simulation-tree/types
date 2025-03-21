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
        /// Registers a type with the given <paramref name="variables"/> and <paramref name="interfaces"/>.
        /// </summary>
        public unsafe readonly void Invoke<T>(ReadOnlySpan<Variable> variables, ReadOnlySpan<TypeLayout> interfaces) where T : unmanaged
        {
            TypeLayout type = new(TypeLayout.GetFullName<T>(), (ushort)sizeof(T), variables, interfaces);
            Input input = new(type, RuntimeTypeTable.GetHandle<T>());
            action(input);
            TypeInstanceCreator.Initialize<T>(type);
        }

        /// <summary>
        /// Registers a type with the given <paramref name="variables"/> and <paramref name="interfaces"/>.
        /// </summary>
        public unsafe readonly void Invoke<T>(VariableBuffer variables, byte variableCount, TypeBuffer interfaces, byte interfaceCount) where T : unmanaged
        {
            TypeLayout type = new(TypeLayout.GetFullName<T>(), (ushort)sizeof(T), variables, variableCount, interfaces, interfaceCount);
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