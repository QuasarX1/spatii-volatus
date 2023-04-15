using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace network_objects
{
    public abstract class IntegerTemplate<T> : NetworkedDataObject<T>
    {
        public IntegerTemplate(int id, int serialised_length) : base(id, serialised_length) { }

        public IntegerTemplate(T value, int id, int serialised_length) : base(id, serialised_length)
        {
            Value = value;
        }

        internal override string? String_Conversion()
        {
            return Value?.ToString();
        }
    }

    public sealed class Integer16 : IntegerTemplate<short>
    {
        private static int id = 2;
        private static int serialised_length = 2;
        public Integer16() : base(id, serialised_length) { }



        public Integer16(short value) : base(value, id, serialised_length) { }

        public override void CreateBytes(ref byte[] buffer, ref int offset)
        {
            BitConverter.GetBytes(Value).CopyTo(buffer, offset);
        }

        internal override void PopulateFromBytes(ref byte[] bytes, ref int offset)
        {
            Value = BitConverter.ToInt16(bytes, offset);
        }

        public static explicit operator short(Integer16 value)
        {
            return value.Value;
        }

        public static explicit operator int(Integer16 value)
        {
            return value.Value;
        }

        public static explicit operator long(Integer16 value)
        {
            return value.Value;
        }
    }

    public sealed class Integer32 : IntegerTemplate<int>
    {
        private static int id = 3;
        private static int serialised_length = 4;
        public Integer32() : base(id, serialised_length) { }


        
        public Integer32(int value) : base(id, serialised_length)
        {
            Value = value;
        }
            
        public override void CreateBytes(ref byte[] buffer, ref int offset)
        {
            BitConverter.GetBytes(Value).CopyTo(buffer, offset);
        }

        internal override void PopulateFromBytes(ref byte[] bytes, ref int offset)
        {
            Value = BitConverter.ToInt32(bytes, offset);
        }

        public static explicit operator int(Integer32 value)
        {
            return value.Value;
        }

        public static explicit operator long(Integer32 value)
        {
            return value.Value;
        }
    }

    public sealed class Integer64 : IntegerTemplate<long>
    {
        private static int id = 4;
        private static int serialised_length = 8;
        public Integer64() : base(id, serialised_length) { }



        public Integer64(long value) : base(id, serialised_length)
        {
            Value = value;
        }

        public override void CreateBytes(ref byte[] buffer, ref int offset)
        {
            BitConverter.GetBytes(Value).CopyTo(buffer, offset);
        }

        internal override void PopulateFromBytes(ref byte[] bytes, ref int offset)
        {
            Value = BitConverter.ToInt64(bytes, offset);
        }

        public static explicit operator long(Integer64 value)
        {
            return value.Value;
        }
    }
}
