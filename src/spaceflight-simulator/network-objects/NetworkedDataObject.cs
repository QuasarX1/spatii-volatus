﻿using System;
using System.Collections.Generic;
using System.Text;

namespace network_objects
{
    public abstract class NetworkedDataObject : _NetworkedDataObject
    {
        protected object _value { get; set; }

        public NetworkedDataObject(int class_id, int serialised_length)
        {
            ID = class_id;
            SerialisedLength = serialised_length;
        }

        public int ID { get; private set; }

        public int SerialisedLength { get; private set; }

        public bool IsPopulated { get; private set; } = false;

        public int Serialise(ref byte[] buffer, ref int offset)
        {
            CreateBytes(ref buffer, ref offset);
            return SerialisedLength;
        }

        public abstract void CreateBytes(ref byte[] buffer, ref int offset);

        internal abstract void PopulateFromBytes(ref byte[] bytes, ref int offset);

        public void _PopulateFromBytes(ref byte[] bytes, ref int offset)
        {
            if (IsPopulated)
            {
                throw new InvalidOperationException("This object has already had a value");
            }
            PopulateFromBytes(ref bytes, ref offset);
        }

        public static NetworkedDataObject FromBytes(Type type, ref byte[] bytes, int offset)
        {
            NetworkedDataObject ob;
            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor != null)
            {
                ob = (NetworkedDataObject)constructor.Invoke(null);
            }
            else
            {
                throw new InvalidOperationException("Type provided is not compatible.");
            }

            ob._PopulateFromBytes(ref bytes, ref offset);
            ob.IsPopulated = true;
            return ob;
        }

        public static T FromBytes<T>(ref byte[] bytes, int offset) where T : NetworkedDataObject, new()
        {
            T ob = new T();
            ob._PopulateFromBytes(ref bytes, ref offset);
            ob.IsPopulated = true;
            return ob;
        }

        internal abstract string? String_Conversion();

        public override string? ToString()
        {
            return String_Conversion();
        }



        public static implicit operator NetworkedDataObject(System.Numerics.Vector3 value)
        {
            return new Vector3(value);
        }

        public static implicit operator NetworkedDataObject(string value)
        {
            return new String(value);
        }

        public static implicit operator NetworkedDataObject(short value)
        {
            return new Integer16(value);
        }

        public static implicit operator NetworkedDataObject(int value)
        {
            return new Integer32(value);
        }

        public static implicit operator NetworkedDataObject(long value)
        {
            return new Integer64(value);
        }

        public static implicit operator NetworkedDataObject(float value)
        {
            return new Float32(value);
        }

        public static implicit operator NetworkedDataObject(double value)
        {
            return new Float64(value);
        }

        public static implicit operator NetworkedDataObject(bool[] value)
        {
            var new_object = new BooleanFlags();
            new_object.Flag0 = value[0];
            new_object.Flag0 = value[1];
            new_object.Flag0 = value[2];
            new_object.Flag0 = value[3];
            new_object.Flag0 = value[4];
            new_object.Flag0 = value[5];
            new_object.Flag0 = value[6];
            new_object.Flag0 = value[7];
            return new_object;
        }
    }

    public abstract class NetworkedDataObject<T> : NetworkedDataObject
    {
        public T Value { get { return (T)_value; } protected set { _value = value; } }
        public NetworkedDataObject(int class_id, int serialised_length) : base(class_id, serialised_length) { }
    }
}
