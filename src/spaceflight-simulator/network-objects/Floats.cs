using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace spaceflight_simulator.network_objects.datatypes
{
    public abstract class FloatTemplate<T> : NetworkedDataObject<T>
    {
        public FloatTemplate(int id, int serialised_length) : base(id, serialised_length) { }

        public FloatTemplate(T value, int id, int serialised_length) : base(id, serialised_length)
        {
            Value = value;
        }

        internal override string? String_Conversion()
        {
            return Value?.ToString();
        }
    }

    public sealed class Float32 : FloatTemplate<float>
    {
        private static int id = 5;
        private static int serialised_length = 4;
        public Float32() : base(id, serialised_length) { }



        public Float32(float value) : base(id, serialised_length)
        {
            Value = value;
        }

        public override void CreateBytes(ref byte[] buffer, ref int offset)
        {
            BitConverter.GetBytes(Value).CopyTo(buffer, offset);
        }

        internal override void PopulateFromBytes(ref byte[] bytes, ref int offset)
        {
            Value = BitConverter.ToSingle(bytes, offset);
        }

        public static explicit operator float(Float32 value)
        {
            return value.Value;
        }

        public static explicit operator double(Float32 value)
        {
            return value.Value;
        }

        public static implicit operator Float32(float value)
        {
            return new Float32(value);
        }
    }

    public sealed class Float64 : FloatTemplate<double>
    {
        private static int id = 6;
        private static int serialised_length = 8;
        public Float64() : base(id, serialised_length) { }



        public Float64(double value) : base(id, serialised_length)
        {
            Value = value;
        }

        public override void CreateBytes(ref byte[] buffer, ref int offset)
        {
            BitConverter.GetBytes(Value).CopyTo(buffer, offset);
        }

        internal override void PopulateFromBytes(ref byte[] bytes, ref int offset)
        {
            Value = BitConverter.ToDouble(bytes, offset);
        }

        public static explicit operator double(Float64 value)
        {
            return value.Value;
        }

        public static implicit operator Float64(double value)
        {
            return new Float64(value);
        }
    }
}
