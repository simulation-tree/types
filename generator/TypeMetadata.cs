﻿using Microsoft.CodeAnalysis;

namespace Types
{
    public class TypeMetadata
    {
        public readonly ITypeSymbol value;
        public uint references;

        public TypeMetadata(ITypeSymbol value, uint references)
        {
            this.value = value;
            this.references = references;
        }
    }
}