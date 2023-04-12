using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace network_objects
{
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
            Address = (address == null) ? IPAddress.Any : address;
            Port = port;

            _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            _socket.Bind(new IPEndPoint(Address, Port));

            _buffer = GC.AllocateArray<byte>(length: 64, pinned: true);
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

        public async Task<Tuple<Type[], network_objects.NetworkedDataObject[]>> DoReciveOneAsync(CancellationToken cancelToken)
        {
            var result = await _socket.ReceiveFromAsync(_buffer_memory, SocketFlags.None, _blankEndpoint, cancelToken);

            return _manager.GetAllObjects(ref _buffer);
        }

        public async Task DoReciveAsync(Action<Tuple<Type[], network_objects.NetworkedDataObject[]>> handeler, CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _socket.ReceiveFromAsync(_buffer_memory, SocketFlags.None, _blankEndpoint, cancelToken);

                    handeler.Invoke(_manager.GetAllObjects(ref _buffer));
                }
                catch (SocketException)
                {
                    // Socket exception means we are finished.
                    break;
                }
            }
        }
    }
}
