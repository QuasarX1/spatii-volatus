using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace network_objects
{
    public sealed class Vector3 : NetworkedDataObject<System.Numerics.Vector3>
    {
        private static int id = 0;
        private static int serialised_length = 12;
        public Vector3() : base(id, serialised_length) { }



        //public System.Numerics.Vector3 Value { get; private set; }
        public float X { get { return Value.X; } }
        public float Y { get { return Value.X; } }
        public float Z { get { return Value.X; } }

        public Vector3(System.Numerics.Vector3 values) : base(id, serialised_length)
        {
            Value = values;
        }

        public Vector3(float x, float y, float z) : base(id, serialised_length)
        {
            Value = new System.Numerics.Vector3(x, y, z);
        }   
            
        public override void CreateBytes(ref byte[] buffer, ref int offset)
        {
            BitConverter.GetBytes(X).CopyTo(buffer, offset);
            BitConverter.GetBytes(Y).CopyTo(buffer, offset + 4);
            BitConverter.GetBytes(Z).CopyTo(buffer, offset + 8);
        }

        internal override void PopulateFromBytes(ref byte[] bytes, ref int offset)
        {
            Value = new System.Numerics.Vector3(BitConverter.ToSingle(bytes, offset),
                                                BitConverter.ToSingle(bytes, offset + 4),
                                                BitConverter.ToSingle(bytes, offset + 8));
        }

        internal override string? String_Conversion()
        {
            return $"{Value.X}, {Value.Y}, {Value.Z}";
        }

        public static explicit operator System.Numerics.Vector3(Vector3 value)
        {
            return value.Value;
        }
    }
}
