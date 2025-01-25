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
            Input input = new(type, typeof(T).TypeHandle);
            Invoke(input);
            TypeInstanceCreator.Initialize<T>(type);
        }

        /// <summary>
        /// Registers a type without variables specified.
        /// </summary>
        public readonly void Invoke<T>() where T : unmanaged
        {
            TypeLayout type = new(TypeLayout.GetFullName<T>(), (ushort)sizeof(T), []);
            Input input = new(type, typeof(T).TypeHandle);
            Invoke(input);
            TypeInstanceCreator.Initialize<T>(type);
        }

        private readonly void Invoke(Input input)
        {
            if (TypeRegistry.OnRegister is not null)
            {
                //todo: annoying branch that only exists here because tests fail on
                //the remote actions runner, despite not happening locally :(
                InvokeManaged(input);
            }
            else
            {
                InvokeUnmanaged(input);
            }
        }

        private readonly void InvokeUnmanaged(Input input)
        {
            function(input);
        }

        private readonly void InvokeManaged(Input input)
        {
            TypeRegistry.OnRegister?.Invoke(input);
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