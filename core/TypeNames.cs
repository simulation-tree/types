using System;
using System.Collections.Generic;
using Unmanaged;

namespace Types
{
    internal static class TypeNames
    {
        private static readonly Dictionary<long, string> hashToValue = new();

        internal static USpan<char> Get(long hash)
        {
            return hashToValue[hash].AsSpan();
        }

        internal static long Set(USpan<char> value)
        {
            long hash = FixedString.GetLongHashCode(value);
            hashToValue[hash] = value.ToString();
            return hash;
        }

        internal static long Set(FixedString value)
        {
            USpan<char> span = stackalloc char[value.Length];
            value.CopyTo(span);
            return Set(span);
        }
    }
}