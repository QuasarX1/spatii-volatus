﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace network_objects
{
    public sealed class SerialisationManager
    {
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

        public NetworkedDataObject[] GetAllObjects(ref byte[] data)
        {
            Tuple<int, int[], int[]> info = GetObjectsInfo(ref data);
            NetworkedDataObject[] objects = new NetworkedDataObject[info.Item2.Length];
            int start_offset = info.Item1;
            for (byte i = 0; i < info.Item2.Length; i++)
            {
                objects[i] = NetworkedDataObject.FromBytes(GetTypeFromId(info.Item2[i]), ref data, start_offset);
                start_offset += info.Item3[i];
            }
            return objects;
        }

        public Type GetTypeFromId(int id)
        {
            return typeof(Vector3);//TODO: lookup
        }

        public static byte[] SerialiseObjects(ref NetworkedDataObject[] target_objects, ref byte[] buffer)
        {
            if (target_objects.Length > byte.MaxValue)
            {
                throw new ArgumentOutOfRangeException("More objects were passes than is permitted. Maximum number is 2^8.");
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
                buffer_offset += target_objects[i].Serialise(ref buffer, ref buffer_offset);
            }
            return buffer;
        }
    }
}