﻿using Types.Functions;

namespace Types
{
    /// <summary>
    /// Describes a collection of types.
    /// </summary>
    public interface ITypeBank
    {
        /// <summary>
        /// Loads type metadata into <see cref="TypeRegistry"/>.
        /// </summary>
        void Load(Register register);
    }
}