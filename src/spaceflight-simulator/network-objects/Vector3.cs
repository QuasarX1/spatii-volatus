using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace network_objects
{
    public class Vector3 : NetworkedDataObject
    {
        private static int id = 0;
        private static int serialised_length = 12;
        public Vector3() : base(id, serialised_length) { }



        private float[] data = new float[3];
        public float x => data[0];
        public float y => data[1];
        public float z => data[2];

        public Vector3(float[] values) : base(id, serialised_length)
        {
            data = values;
        }

        public Vector3(float x, float y, float z) : base(id, serialised_length)
        {
            data[0] = x;
            data[1] = y;
            data[2] = z;
        }   
            
        public override void CreateBytes(ref byte[] buffer, ref int offset)
        {
            BitConverter.GetBytes(data[0]).CopyTo(buffer, offset);
            BitConverter.GetBytes(data[1]).CopyTo(buffer, offset + 4);
            BitConverter.GetBytes(data[2]).CopyTo(buffer, offset + 8);
        }

        internal override void PopulateFromBytes(ref byte[] bytes, ref int offset)
        {
            data[0] = BitConverter.ToSingle(bytes, offset);
            data[1] = BitConverter.ToSingle(bytes, offset + 4);
            data[2] = BitConverter.ToSingle(bytes, offset + 8);
        }
    }
}
