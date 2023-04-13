using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace network_objects
{
    /// <summary>
    /// Based on https://enclave.io/high-performance-udp-sockets-net6/
    /// </summary>
    public class DataReciver
    {
        public IPAddress Address { get; private set; }
        public int Port { get; private set; }
        
        private Socket _socket;
        private static readonly IPEndPoint _blankEndpoint = new IPEndPoint(IPAddress.Any, 0);
        private network_objects.SerialisationManager _manager = new network_objects.SerialisationManager();
        private byte[] _buffer;
        private Memory<byte> _buffer_memory;

        public DataReciver(int port, IPAddress? address = null)
        {
            //Console.WriteLine(_manager.testDict[0].ToString());
            Address = address ?? IPAddress.Any;
            Port = port;

            _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            _socket.Bind(new IPEndPoint(Address, Port));

            _buffer = GC.AllocateArray<byte>(length: 1024, pinned: true);
            _buffer_memory = _buffer.AsMemory();
        }

        public void Kill()
        {
            _socket.Close();//TODO: nessessary???
            _socket.Dispose();
        }

        ~DataReciver()
        {
            Kill();
        }

        public async Task<Tuple<Type[], network_objects.NetworkedDataObject[], SocketReceiveFromResult>> DoReciveOneAsync(CancellationToken cancelToken)
        {
            var result = await _socket.ReceiveFromAsync(_buffer_memory, SocketFlags.None, _blankEndpoint, cancelToken);

            var object_data = _manager.GetAllObjects(ref _buffer);

            return new Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult>(object_data.Item1, object_data.Item2, result);
        }

        public async Task DoReciveAsync(Action<Tuple<Type[], network_objects.NetworkedDataObject[], SocketReceiveFromResult>> handeler, CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _socket.ReceiveFromAsync(_buffer_memory, SocketFlags.None, _blankEndpoint, cancelToken);

                    var object_data = _manager.GetAllObjects(ref _buffer);

                    handeler.Invoke(new Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult>(object_data.Item1, object_data.Item2, result));
                }
                catch (OperationCanceledException) { break; }
                catch (SocketException) { break; }
            }
        }
    }
}
