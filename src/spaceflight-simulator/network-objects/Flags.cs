using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace network_objects
{
    public sealed class BooleanFlags : NetworkedDataObject<byte>
    {
        private static int id = 7;
        private static int serialised_length = 1;
        public BooleanFlags() : base(id, serialised_length)
        {
            Value = 0;
        }



        public BooleanFlags(byte value) : base(id, serialised_length)
        {
            Value = value;
        }

        public bool Flag0 {
            get { return (Value & 0b10000000) != 0; }
            set { if (value)
                {
                    Value |= 0b10000000;
                }
                else
                {
                    Value &= 0b01111111;
                }
            }
        }

        public bool Flag1
        {
            get { return (Value & 0b01000000) != 0; }
            set
            {
                if (value)
                {
                    Value |= 0b01000000;
                }
                else
                {
                    Value &= 0b10111111;
                }
            }
        }

        public bool Flag2
        {
            get { return (Value & 0b00100000) != 0; }
            set
            {
                if (value)
                {
                    Value |= 0b00100000;
                }
                else
                {
                    Value &= 0b11011111;
                }
            }
        }

        public bool Flag3
        {
            get { return (Value & 0b00010000) != 0; }
            set
            {
                if (value)
                {
                    Value |= 0b00010000;
                }
                else
                {
                    Value &= 0b11101111;
                }
            }
        }

        public bool Flag4
        {
            get { return (Value & 0b00001000) != 0; }
            set
            {
                if (value)
                {
                    Value |= 0b00001000;
                }
                else
                {
                    Value &= 0b11110111;
                }
            }
        }

        public bool Flag5
        {
            get { return (Value & 0b00000100) != 0; }
            set
            {
                if (value)
                {
                    Value |= 0b00000100;
                }
                else
                {
                    Value &= 0b11111011;
                }
            }
        }

        public bool Flag6
        {
            get { return (Value & 0b00000010) != 0; }
            set
            {
                if (value)
                {
                    Value |= 0b00000010;
                }
                else
                {
                    Value &= 0b11111101;
                }
            }
        }

        public bool Flag7
        {
            get { return (Value & 0b00000001) != 0; }
            set
            {
                if (value)
                {
                    Value |= 0b00000001;
                }
                else
                {
                    Value &= 0b11111110;
                }
            }
        }

        public override void CreateBytes(ref byte[] buffer, ref int offset)
        {
            buffer[offset] = Value;
        }

        internal override void PopulateFromBytes(ref byte[] bytes, ref int offset)
        {
            Value = bytes[offset];
        }

        internal override string? String_Conversion()
        {
            return $"{Flag0.ToString()}, {Flag1.ToString()}, {Flag2.ToString()}, {Flag3.ToString()}, {Flag4.ToString()}, {Flag5.ToString()}, {Flag6.ToString()}, {Flag7.ToString()}";
        }
    }
}
