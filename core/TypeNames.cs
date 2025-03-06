using System;
using System.Collections.Generic;

namespace Types
{
    internal static class TypeNames
    {
        private static readonly Dictionary<long, string> hashToValue = new();

        internal static ReadOnlySpan<char> Get(long hash)
        {
            return hashToValue[hash].AsSpan();
        }

        internal static long Set(ReadOnlySpan<char> value)
        {
            long hash = value.GetLongHashCode();
            hashToValue[hash] = value.ToString();
            return hash;
        }
    }
}