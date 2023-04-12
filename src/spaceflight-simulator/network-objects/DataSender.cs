using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace network_objects
{
    /// <summary>
    /// Based on https://enclave.io/high-performance-udp-sockets-net6/
    /// </summary>
    public class DataSender
    {
        public bool DefaultAddressSet { get; private set; }
        public IPAddress DestinationAddress { get; private set; }
        public int Port { get; private set; }

        private Socket _socket;
        private IPEndPoint _destination_endpoint;
        private network_objects.SerialisationManager _manager = new network_objects.SerialisationManager();
        private byte[] _buffer;
        private Memory<byte> _buffer_memory;

        public DataSender(int port, IPAddress? destination_address = null)
        {
            DefaultAddressSet = destination_address != null;
            DestinationAddress = (!DefaultAddressSet) ? IPAddress.Any : destination_address;
            Port = port;
            _destination_endpoint = new IPEndPoint(DestinationAddress, Port);

            _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            _buffer = GC.AllocateArray<byte>(length: 64, pinned: true);
            _buffer_memory = _buffer.AsMemory();
        }

        public void Kill()
        {
            _socket.Close();//TODO: nessessary???
            _socket.Dispose();
        }

        ~DataSender()
        {
            Kill();
        }

        public async Task DoSendOneAsync(network_objects.NetworkedDataObject[] data, CancellationToken cancelToken)
        {
            if (DefaultAddressSet)
            {
                network_objects.SerialisationManager.SerialiseObjects(ref data, ref _buffer);

                await _socket.SendToAsync(_buffer_memory, SocketFlags.None, _destination_endpoint, cancelToken);
            }
            else
            {
                throw new InvalidOperationException("Object was not initialised with a default target.");
            }
        }

        public async Task DoTargetedSendOneAsync(network_objects.NetworkedDataObject[] data, IPEndPoint target, CancellationToken cancelToken)
        {
            network_objects.SerialisationManager.SerialiseObjects(ref data, ref _buffer);

            await _socket.SendToAsync(_buffer_memory, SocketFlags.None, target, cancelToken);
        }
    }
}
