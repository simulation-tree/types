using System;

namespace Types.Functions
{
    /// <summary>
    /// Function to register a type.
    /// </summary>
    public readonly struct RegisterFunction
    {
        /// <summary>
        /// Registers a type with the given <paramref name="variables"/> and <paramref name="interfaces"/>.
        /// </summary>
        public unsafe readonly void RegisterType<T>(ReadOnlySpan<Field> variables, ReadOnlySpan<Interface> interfaces) where T : unmanaged
        {
            TypeMetadata type = new(MetadataRegistry.GetFullName<T>(), (ushort)sizeof(T), variables, interfaces);
            MetadataRegistry.RegisterType(type, RuntimeTypeTable.GetHandle<T>());
            TypeInstanceCreator.Initialize<T>(type);
        }

        /// <summary>
        /// Registers a type with the given <paramref name="variables"/> and <paramref name="interfaces"/>.
        /// </summary>
        public unsafe readonly void RegisterType<T>(FieldBuffer variables, byte variableCount, InterfaceBuffer interfaces, byte interfaceCount) where T : unmanaged
        {
            TypeMetadata type = new(MetadataRegistry.GetFullName<T>(), (ushort)sizeof(T), variables, variableCount, interfaces, interfaceCount);
            MetadataRegistry.RegisterType(type, RuntimeTypeTable.GetHandle<T>());
            TypeInstanceCreator.Initialize<T>(type);
        }

        /// <summary>
        /// Registers a type without variables specified.
        /// </summary>
        public unsafe readonly void RegisterType<T>() where T : unmanaged
        {
            TypeMetadata type = new(MetadataRegistry.GetFullName<T>(), (ushort)sizeof(T));
            MetadataRegistry.RegisterType(type, RuntimeTypeTable.GetHandle<T>());
            TypeInstanceCreator.Initialize<T>(type);
        }

        /// <summary>
        /// Registers an interface type.
        /// </summary>
        public readonly void RegisterInterface<T>()
        {
            Interface interfaceValue = new(MetadataRegistry.GetFullName<T>());
            MetadataRegistry.RegisterInterface(interfaceValue, RuntimeTypeTable.GetHandle<T>());
        }

        /// <summary>
        /// Registers an interface type.
        /// </summary>
        public readonly void RegisterInterface(ReadOnlySpan<char> fullTypeName, RuntimeTypeHandle typeHandle)
        {
            Interface interfaceValue = new(fullTypeName);
            MetadataRegistry.RegisterInterface(interfaceValue, typeHandle);
        }

        /// <summary>
        /// Input parameter.
        /// </summary>
        public readonly struct Input
        {
            /// <summary>
            /// Metadata of the type.
            /// </summary>
            public readonly TypeMetadata type;

            private readonly nint handle;

            /// <summary>
            /// Handle of the registering type.
            /// </summary>
            public readonly RuntimeTypeHandle Handle => RuntimeTypeTable.GetHandle(handle);

            /// <summary>
            /// Creates the input argument.
            /// </summary>
            public Input(TypeMetadata type, RuntimeTypeHandle handle)
            {
                this.type = type;
                this.handle = RuntimeTypeTable.GetAddress(handle);
            }
        }
    }
}