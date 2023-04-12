using System;
using System.Collections.Generic;
using System.Text;

namespace network_objects
{
    public abstract class NetworkedDataObject : _NetworkedDataObject
    {
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
    }
}
