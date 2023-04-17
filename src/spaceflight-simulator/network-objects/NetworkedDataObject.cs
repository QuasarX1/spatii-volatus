using System;
using System.Collections.Generic;
using System.Text;

namespace spaceflight_simulator.network_objects.datatypes
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



        public static explicit operator System.Numerics.Vector3?(NetworkedDataObject value)
        {
            try
            {
                return ((Vector3)value).Value;
            }
            catch (InvalidCastException)
            {
                return null;
            }
        }

        public static explicit operator string?(NetworkedDataObject value)
        {
            try
            {
                return ((spaceflight_simulator.network_objects.datatypes.String)value).Value;
            }
            catch (InvalidCastException)
            {
                return null;
            }
        }

        public static explicit operator short?(NetworkedDataObject value)
        {
            try
            {
                return ((Integer16)value).Value;
            }
            catch (InvalidCastException)
            {
                return null;
            }
        }

        public static explicit operator int?(NetworkedDataObject value)
        {
            try
            {
                return ((Integer32)value).Value;
            }
            catch (InvalidCastException)
            {
                try
                {
                    return ((Integer16)value).Value;
                }
                catch (InvalidCastException)
                {
                    return null;
                }
            }
        }

        public static explicit operator long?(NetworkedDataObject value)
        {
            try
            {
                return ((Integer64)value).Value;
            }
            catch (InvalidCastException)
            {
                try
                {
                    return ((Integer32)value).Value;
                }
                catch (InvalidCastException)
                {
                    try
                    {
                        return ((Integer16)value).Value;
                    }
                    catch (InvalidCastException)
                    {
                        return null;
                    }
                }
            }
        }

        public static explicit operator float?(NetworkedDataObject value)
        {
            try
            {
                return ((Float32)value).Value;
            }
            catch (InvalidCastException)
            {
                return null;
            }
        }

        public static explicit operator double?(NetworkedDataObject value)
        {
            try
            {
                return ((Float64)value).Value;
            }
            catch (InvalidCastException)
            {
                try
                {
                    return ((Float32)value).Value;
                }
                catch (InvalidCastException)
                {
                    return null;
                }
            }
        }

        public static implicit operator bool[]?(NetworkedDataObject value)
        {
            try
            {
                return (bool[])(BooleanFlags)value;
            }
            catch (InvalidCastException)
            {
                return null;
            }
        }
    }

    public abstract class NetworkedDataObject<T> : NetworkedDataObject
    {
        public T Value { get { return (T)_value; } protected set { _value = value; } }
        public NetworkedDataObject(int class_id, int serialised_length) : base(class_id, serialised_length) { }
    }
}
