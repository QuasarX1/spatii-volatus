using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace network_objects
{
    public sealed class String : NetworkedDataObject<string>
    {
        private static int id = 1;
        private static int serialised_length = 511;// 510 bytes (255 characters) of string data and one byte to specify the length (up to 255)
        public String() : base(id, serialised_length) { }


        
        public String(string value) : base(id, serialised_length)
        {
            if (value.Length > byte.MaxValue)
            {
                throw new InvalidOperationException("String has more than 255 characters.");
            }

            Value = value;
        }
            
        public override void CreateBytes(ref byte[] buffer, ref int offset)
        {
            buffer[offset] = (byte)Value.Length;
            int moving_offset = offset + 1;
            for (int i = 0; i < Value.Length; i++)
            {
                BitConverter.GetBytes(Value[i]).CopyTo(buffer, moving_offset);
                moving_offset += 2;// 16-bit char is 2 bytes
            }
        }

        internal override void PopulateFromBytes(ref byte[] bytes, ref int offset)
        {
            char[] chars = new char[bytes[offset]];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = BitConverter.ToChar(bytes, offset + 1 + (i * 2));
            }
            Value = new string(chars);
        }

        internal override string? String_Conversion()
        {
            return Value;
        }

        public static explicit operator string(String value)
        {
            return value.Value;
        }
    }
}
