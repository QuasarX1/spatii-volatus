using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

using spaceflight_simulator.network_objects.datatypes;

namespace spaceflight_simulator.network_objects
{
    public sealed class UdpCommunicator : Communicator
    {
        private byte[] _send_buffer;
        private Memory<byte> _send_buffer_memory;

        private byte[] _recieve_buffer;
        private Memory<byte> _recieve_buffer_memory;

        private event Action OnPingReturn;

        private void _setup()
        {
            InternalSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            InternalSocket.Bind(new IPEndPoint(IPAddress.Any, Port));

            _send_buffer = GC.AllocateArray<byte>(length: NetworkSettings.PACKET_BYTES, pinned: true);
            _recieve_buffer = GC.AllocateArray<byte>(length: NetworkSettings.PACKET_BYTES, pinned: true);

            _send_buffer_memory = _send_buffer.AsMemory();
            _recieve_buffer_memory = _recieve_buffer.AsMemory();
    }

        public UdpCommunicator(int client_port, CancellationToken? cancelToken = null) : base(client_port, 0, IPAddress.Any, cancelToken)
        {
            _setup();
        }

        public UdpCommunicator(int client_port, int destination_port, IPAddress destination_address, CancellationToken? cancelToken = null) : base(client_port, destination_port, destination_address, cancelToken)
        {
            _setup();
        }



        public override async Task SendOneAsync(NetworkedDataObject[] data, CancellationToken cancelToken)
        {
            if (data.Length == 0)
            {
                await PingAsync(cancelToken);
                return;
            }

            if (IsDefaultAddressSet)
            {
                Array.Clear(_send_buffer);

                network_objects.SerialisationManager.SerialiseObjects(ref data, ref _send_buffer);

                await InternalSocket.SendToAsync(_send_buffer_memory, SocketFlags.None, DefaultDestinationEndpoint, cancelToken);
            }
            else
            {
                throw new InvalidOperationException("Object was not initialised with a default target.");
            }
        }

        public override async Task TargetedSendOneAsync(NetworkedDataObject[] data, IPEndPoint target, CancellationToken cancelToken)
        {
            if (data.Length == 0)
            {
                await TargetedPingAsync(target, cancelToken);
                return;
            }

            Array.Clear(_send_buffer);

            network_objects.SerialisationManager.SerialiseObjects(ref data, ref _send_buffer);

            await InternalSocket.SendToAsync(_send_buffer_memory, SocketFlags.None, target, cancelToken);
        }

        private bool GetPingResult()
        {
            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
            Action reply_handeler = () => completionSource.TrySetResult(true);
            OnPingReturn += reply_handeler;
            bool result = Task.WaitAll(new Task[] { completionSource.Task }, PingWaitMillis);
            OnPingReturn -= reply_handeler;
            return result;
        }

        public override async Task<bool> PingAsync(CancellationToken cancelToken, bool reply = false)
        {
            if (IsDefaultAddressSet)
            {
                return await TargetedPingAsync(DefaultDestinationEndpoint, cancelToken, reply);
            }
            else
            {
                throw new InvalidOperationException("Object was not initialised with a default target.");
            }
        }

        public override async Task<bool> TargetedPingAsync(IPEndPoint target, CancellationToken cancelToken, bool reply = false)
        {
            if (!reply && !RecieverRunning)
            {
                throw new InvalidOperationException("Unable to send a ping. This requires the communicator to be activley processing incoming data. Call StartReciever first.");
            }

            Array.Clear(_send_buffer);

            _send_buffer[0] = (byte)0;
            _send_buffer[1] = ((reply) ? byte.MaxValue : (byte)0);

            await InternalSocket.SendToAsync(_send_buffer_memory, SocketFlags.None, target, cancelToken);

            if (!reply)
            {
                return GetPingResult();
            }
            else
            {
                return true;
            }
        }

        public override async Task<Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult>> ReciveOneAsync(CancellationToken cancelToken)
        {
            var result = await InternalSocket.ReceiveFromAsync(_recieve_buffer_memory, SocketFlags.None, NetworkSettings.BLANK_IP_ENDPOINT, cancelToken);

            var object_data = InternalSerialisationManager.GetAllObjects(ref _recieve_buffer);

            return new Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult>(object_data.Item1, object_data.Item2, result);
        }

        private async void PingReply(SocketReceiveFromResult sender)
        {
            await TargetedPingAsync((IPEndPoint)sender.RemoteEndPoint, InternalCancellationToken, reply: true);
        }

        public override async Task DoReceiveAsync(Action<Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult>> handeler, CancellationToken cancelToken)
        {
            OnPing += PingReply;

            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    var result = await InternalSocket.ReceiveFromAsync(_recieve_buffer_memory, SocketFlags.None, NetworkSettings.BLANK_IP_ENDPOINT, cancelToken);

                    var object_data = InternalSerialisationManager.GetAllObjects(ref _recieve_buffer);

                    if (object_data.Item1.Length == 0)
                    {
                        if (_recieve_buffer[1] == byte.MaxValue)
                        {
                            // This is a reply to a ping. Notify
                            OnPingReturn?.Invoke();
                        }
                        else
                        {
                            // Reply to the ping
                            OnPing_Invoke(result);
                        }
                    }
                    else
                    {
                        handeler.Invoke(new Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult>(object_data.Item1, object_data.Item2, result));
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (SocketException) { break; }
            }

            OnPing -= PingReply;
        }
    }
}
