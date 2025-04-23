using Types.Functions;

namespace Types
{
    /// <summary>
    /// Describes a collection of <see langword="struct"/> types and interfaces.
    /// </summary>
    public interface IMetadataBank
    {
        /// <summary>
        /// Loads type metadata into <see cref="MetadataRegistry"/>.
        /// </summary>
        void Load(RegisterFunction register);
    }
}