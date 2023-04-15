using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace network_objects
{
    public abstract class Message
    {
        public static Dictionary<int, Type> MakeLookup(Type[] types)
        {
            var lookup = new Dictionary<int, Type>();
            for (int i = 0; i < types.Length; i++)
            {
                var constructor = types[i].GetConstructor(Type.EmptyTypes);
                if (constructor != null)
                {
                    lookup.Add(((Message)constructor.Invoke(null)).MessageTypeID, types[i]);
                }
                else
                {
                    throw new InvalidOperationException("Type provided is not compatible. It must define a blank constructor.");
                }
            }
            return lookup;
        }



        protected NetworkedDataObject[] data;

        public readonly short MessageTypeID;

        protected Message(byte type_id, int n_items)
        {
            MessageTypeID = type_id;
            data = new NetworkedDataObject[n_items];
        }

        protected abstract bool ValidateDataValues(ref Type[] types, ref NetworkedDataObject[] input_data);
        private bool _ValidateData(ref Type[] types, ref NetworkedDataObject[] input_data)
        {
            return input_data.Length != data.Length && ValidateDataValues(ref types, ref input_data);
        }

        internal static void Populate(Message new_object, Type[] types, NetworkedDataObject[] input_data)
        {
            if (!new_object._ValidateData(ref types, ref input_data))
            {
                throw new ArgumentException($"Data was invalid for a message of type {new_object.MessageTypeID}.");
            }

            new_object.data = input_data;
        }

        internal NetworkedDataObject[] GetData()
        {
            return data;
        }

        public abstract string GetDisplayString();

        public override string ToString()
        {
            return GetDisplayString();
        }
    }
}
