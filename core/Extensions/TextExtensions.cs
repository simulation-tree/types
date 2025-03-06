using System;

namespace Types
{
    internal static class TextExtensions
    {
        public static long GetLongHashCode(this string text)
        {
            unchecked
            {
                long hash = 3074457345618258791;
                for (int i = 0; i < text.Length; i++)
                {
                    hash += text[i];
                    hash *= 3074457345618258799;
                }

                return hash;
            }
        }

        public static long GetLongHashCode(this ReadOnlySpan<char> text)
        {
            unchecked
            {
                long hash = 3074457345618258791;
                for (int i = 0; i < text.Length; i++)
                {
                    hash += text[i];
                    hash *= 3074457345618258799;
                }

                return hash;
            }
        }
    }
}
