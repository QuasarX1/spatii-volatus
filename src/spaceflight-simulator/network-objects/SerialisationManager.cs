using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using spaceflight_simulator.network_objects.datatypes;

namespace spaceflight_simulator.network_objects
{
    public sealed class SerialisationManager
    {
        private static Type[] _registered_types = { typeof(Vector3), typeof(spaceflight_simulator.network_objects.datatypes.String), typeof(Integer16), typeof(Integer32), typeof(Integer64), typeof(Float32), typeof(Float64), typeof(BooleanFlags) };
        private Dictionary<int, Type> _registered_type_by_id = new Dictionary<int, Type>(from Type type in _registered_types select new KeyValuePair<int, Type>((int)type.GetProperty("ID").GetValue(type.GetConstructor(Type.EmptyTypes).Invoke(null)), type));
        
        private static int id_n_bytes = 4;
        private static int serialisation_length_n_bytes = 4;
        private static int object_info_n_bytes = id_n_bytes + serialisation_length_n_bytes;

        public static Tuple<int, int[], int[]> GetObjectsInfo(ref byte[] data)
        {
            // data_start_index = 8 bytes for each set of two int values representing each object, plus the one byte for number of objects
            Tuple<int, int[], int[]> info = new Tuple<int, int[], int[]>(1 + (data[0] * object_info_n_bytes), new int[data[0]], new int[data[0]]);
            
            for (byte i = 0; i < data[0]; i++)
            {
                info.Item2[i] = BitConverter.ToInt32(data, 1 + (i * object_info_n_bytes));
                info.Item3[i] = BitConverter.ToInt32(data, 1 + (i * object_info_n_bytes) + id_n_bytes);
            }

            return info;
        }

        public static Tuple<int, int[], int[]> GetObjectsInfo(ref NetworkedDataObject[] data)
        {
            // data_start_index = 8 bytes for each set of two int values representing each object, plus the one byte for number of objects
            Tuple<int, int[], int[]> info = new Tuple<int, int[], int[]>(1 + (data.Length * object_info_n_bytes), new int[data.Length], new int[data.Length]);

            for (byte i = 0; i < data.Length; i++)
            {
                info.Item2[i] = data[i].ID;
                info.Item3[i] = data[i].SerialisedLength;
            }

            return info;
        }

        public static int CalculateBufferLength(Tuple<int, int[], int[]> info)
        {
            return info.Item3.Sum() + info.Item1;
        }

        public static T GetObject<T>(byte index, ref byte[] data, ref Tuple<int, int[], int[]> info) where T : NetworkedDataObject, new()
        {
            int buffer_offset_index = info.Item3[..index].Sum();
            return NetworkedDataObject.FromBytes<T>(ref data, buffer_offset_index);
        }

        public static T GetObject<T>(int buffer_offset_index, ref byte[] data)
            where T : NetworkedDataObject, new()
            => NetworkedDataObject.FromBytes<T>(ref data, buffer_offset_index);

        public Tuple<Type[], NetworkedDataObject[]> GetAllObjects(ref byte[] data)
        {
            Tuple<int, int[], int[]> info = GetObjectsInfo(ref data);
            NetworkedDataObject[] objects = new NetworkedDataObject[info.Item2.Length];
            Type[] types = new Type[info.Item2.Length];
            int start_offset = info.Item1;
            for (byte i = 0; i < info.Item2.Length; i++)
            {
                types[i] = GetTypeFromId(info.Item2[i]);
                objects[i] = NetworkedDataObject.FromBytes(types[i], ref data, start_offset);
                start_offset += info.Item3[i];
            }
            return new Tuple<Type[], NetworkedDataObject[]>(types, objects);
        }

        public Type GetTypeFromId(int id)
        {
            return _registered_type_by_id[id];
        }

        public static void SerialiseObjects(ref NetworkedDataObject[] target_objects, ref byte[] buffer)
        {
            if (target_objects.Length > byte.MaxValue)
            {
                throw new ArgumentOutOfRangeException("More objects were passes than is permitted. Maximum number is 255.");
            }

            Tuple<int, int[], int[]> info = GetObjectsInfo(ref target_objects);

            if (buffer.Length < CalculateBufferLength(info))
            {
                throw new ArgumentOutOfRangeException("The provided buffer is not long enough.");
            }

            buffer[0] = (byte)target_objects.Length;
            int buffer_offset = 1 + (target_objects.Length * object_info_n_bytes);
            for (int i = 0; i < target_objects.Length; i++)
            {
                BitConverter.GetBytes(target_objects[i].ID).CopyTo(buffer, 1 + (i * object_info_n_bytes));
                BitConverter.GetBytes(target_objects[i].SerialisedLength).CopyTo(buffer, 1 + (i * object_info_n_bytes) + id_n_bytes);
                buffer_offset += target_objects[i].Serialise(ref buffer, ref buffer_offset);
            }
        }
    }
}
