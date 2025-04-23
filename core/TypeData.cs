using System;
using System.Collections.Generic;

namespace Types
{
    internal struct TypeData
    {
        private static readonly Dictionary<long, int> map = [];
        private static TypeData[] array = [];

        public ushort size;
        public byte fieldCount;
        public byte interfaceCount;
        public FieldBuffer fields;
        public InterfaceBuffer interfaces;

        public static ref TypeData Get(long hash)
        {
            if (!map.TryGetValue(hash, out int index))
            {
                index = array.Length;
                Array.Resize(ref array, index + 1);
                map.Add(hash, index);
            }

            return ref array[index];
        }
    }
}