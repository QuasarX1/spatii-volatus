using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace network_objects
{
    public class String : NetworkedDataObject
    {
        private static int id = 1;
        private static int serialised_length = 511;// 510 bytes (255 characters) of string data and one byte to specify the length (up to 255)
        public String() : base(id, serialised_length) { }


        
        private string data;
        public string Data => data;

        public String(string value) : base(id, serialised_length)
        {
            if (value.Length > byte.MaxValue)
            {
                throw new InvalidOperationException("String has more than 255 characters.");
            }

            data = value;
        }
            
        public override void CreateBytes(ref byte[] buffer, ref int offset)
        {
            buffer[offset] = (byte)data.Length;
            int moving_offset = offset + 1;
            for (int i = 0; i < data.Length; i++)
            {
                BitConverter.GetBytes(data[i]).CopyTo(buffer, moving_offset);
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
            data = new string(chars);
        }
    }
}
