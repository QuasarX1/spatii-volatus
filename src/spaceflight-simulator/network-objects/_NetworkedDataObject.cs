using System;
using System.Collections.Generic;
using System.Text;

namespace spaceflight_simulator.network_objects.datatypes
{
    internal interface _NetworkedDataObject
    {
        public int ID { get; }

        public int SerialisedLength { get; }

        public bool IsPopulated { get; }

        public int Serialise(ref byte[] buffer, ref int offset);

        public void _PopulateFromBytes(ref byte[] bytes, ref int offset);
    }
}
