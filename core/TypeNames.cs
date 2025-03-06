using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
            long hash = ASCIIText256.GetLongHashCode(value);
            hashToValue[hash] = value.ToString();
            return hash;
        }

        [SkipLocalsInit]
        internal static long Set(ASCIIText256 value)
        {
            USpan<char> span = stackalloc char[value.Length];
            value.CopyTo(span);
            return Set(span);
        }
    }
}