using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

using spaceflight_simulator.network_objects.datatypes;

namespace spaceflight_simulator.network_objects
{
    public enum TcpCommunicatorMode
    {
        Client,
        Server
    }

    public class TcpCommunicator : Communicator
    {
        public readonly TcpCommunicatorMode Mode;

        private byte[] _send_buffer;
        private Memory<byte> _send_buffer_memory;
 
        private byte[] _recieve_buffer;
        private Memory<byte> _recieve_buffer_memory;

        private readonly Integer16 message_received_successfully = 0;
        private readonly Integer16 message_error = 1;
 
//        private event Action OnPingReturn;

        private void _setup()
        {
            InternalSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (Mode == TcpCommunicatorMode.Server)
            {
                InternalSocket.Bind(new IPEndPoint(IPAddress.Any, Port));
                InternalSocket.Listen(NetworkSettings.TCP_SERVER_BACKLOG_MAX);
            }

            _send_buffer = GC.AllocateArray<byte>(length: NetworkSettings.PACKET_BYTES, pinned: true);
            _recieve_buffer = GC.AllocateArray<byte>(length: NetworkSettings.PACKET_BYTES, pinned: true);

            _send_buffer_memory = _send_buffer.AsMemory();
            _recieve_buffer_memory = _recieve_buffer.AsMemory();
    }

        public TcpCommunicator(int server_port, CancellationToken? cancelToken = null) : base(server_port, 0, IPAddress.Any, cancelToken)
        {
            Mode = Mode = TcpCommunicatorMode.Server;
            _setup();
        }

        public TcpCommunicator(int client_port, int destination_port, IPAddress destination_address, CancellationToken? cancelToken = null) : base(client_port, destination_port, destination_address, cancelToken)
        {
            Mode = TcpCommunicatorMode.Client;
            _setup();
        }



        // Client Only

        private IOException ConnectionFailed() => new IOException("Failed to connect to server.", 0);
        private IOException BadDisconnect() => new IOException("Disconnection rendered TCP socket not reusable.", 1);
        private IOException MissingReplyData() => new IOException("Server reply missing data.", 2);
        private IOException ServerError(string? message = null)
        {
            return new IOException((message == null) ? "Server error - no information avalible." : $"Server error:\n{message}", 3);
        }
        private IOException PingFormatIncorrect() => new IOException("Ping reply not formatted correctly.", 4);
        private IOException PingReplyIncorrect() => new IOException("Expected reply ping - got somthing else or corrupted packet.", 5);

        public override async Task SendOneAsync(NetworkedDataObject[] data, CancellationToken cancelToken)
        {
            if (Mode == TcpCommunicatorMode.Server)
            {
                throw new InvalidOperationException("TCP servers must explicitly specify send targets.");
            }

            if (data.Length == 0)
            {
                await PingAsync(cancelToken);
                return;
            }



            ValueTask connection_task = InternalSocket.ConnectAsync(DefaultDestinationEndpoint, cancelToken);

            Array.Clear(_send_buffer);

            SerialisationManager.SerialiseObjects(ref data, ref _send_buffer);

            await connection_task;
            if (!connection_task.IsCompletedSuccessfully)
            {
                ConnectionFailed();
            }

            await InternalSocket.SendAsync(_send_buffer_memory, SocketFlags.None, cancelToken);

            var result = await InternalSocket.ReceiveAsync(_recieve_buffer_memory, SocketFlags.None, cancelToken);

            SocketAsyncEventArgs disconnectArgs = new SocketAsyncEventArgs();
            CancellationTokenSource cancellationSource = new();
            disconnectArgs.Completed += (sender, args) => cancellationSource.Cancel();
            if (InternalSocket.DisconnectAsync(disconnectArgs))
            {
                while (!cancellationSource.IsCancellationRequested) { }
            }

            if (!disconnectArgs.DisconnectReuseSocket)
            {
                BadDisconnect();
            }

            var object_data = InternalSerialisationManager.GetAllObjects(ref _recieve_buffer);
            if (object_data.Item1.Length == 0)
            {
                MissingReplyData();
            }
            else if (object_data.Item2[0] == message_error)
            {
                if (object_data.Item2[1].GetType() == typeof(datatypes.String))
                {
                    ServerError((string?)object_data.Item2[1]);
                }
                else
                {
                    ServerError();
                }
            }
        }

        public async Task SendReceiveOneAsync(NetworkedDataObject[] data, Action<Type[], NetworkedDataObject[]> callback, CancellationToken cancelToken)
        {
            if (Mode == TcpCommunicatorMode.Server)
            {
                throw new InvalidOperationException("Only TCP clients can send and get a responce in one opperation.");
            }

            if (data.Length == 0)
            {
                throw new InvalidOperationException("Data required for send and receive opperations.");
            }



            ValueTask connection_task = InternalSocket.ConnectAsync(DefaultDestinationEndpoint, cancelToken);

            Array.Clear(_send_buffer);

            SerialisationManager.SerialiseObjects(ref data, ref _send_buffer);

            await connection_task;
            if (!connection_task.IsCompletedSuccessfully)
            {
                throw ConnectionFailed();
            }

            await InternalSocket.SendAsync(_send_buffer_memory, SocketFlags.None, cancelToken);

            var result = await InternalSocket.ReceiveAsync(_recieve_buffer_memory, SocketFlags.None, cancelToken);

            SocketAsyncEventArgs disconnectArgs = new SocketAsyncEventArgs();
            CancellationTokenSource cancellationSource = new();
            disconnectArgs.Completed += (sender, args) => cancellationSource.Cancel();
            if (InternalSocket.DisconnectAsync(disconnectArgs))
            {
                while (!cancellationSource.IsCancellationRequested) { }
            }

            if (!disconnectArgs.DisconnectReuseSocket)
            {
                throw BadDisconnect();
            }

            var object_data = InternalSerialisationManager.GetAllObjects(ref _recieve_buffer);
            if (object_data.Item1.Length == 0)
            {
                throw MissingReplyData();
            }

            callback?.Invoke(object_data.Item1, object_data.Item2);
        }

        public override async Task<bool> PingAsync(CancellationToken cancelToken, bool reply = false)
        {
            if (Mode == TcpCommunicatorMode.Server)
            {
                throw new NotSupportedException("Servers cannot issue TCP pings.");
            }

            if (reply)
            {
                throw new NotSupportedException("TCP reply pings are integrated into the server mode and can't be generated manually.");
            }



            ValueTask connection_task = InternalSocket.ConnectAsync(DefaultDestinationEndpoint, cancelToken);

            Array.Clear(_send_buffer);

            _send_buffer[0] = (byte)0;
            _send_buffer[1] = (byte)0;

            bool connection_result = Task.WaitAll(new Task[] { connection_task.AsTask() }, PingWaitMillis);
            if (!connection_task.IsCompletedSuccessfully || !connection_result)
            {
                return false;
            }

            await InternalSocket.SendAsync(_send_buffer_memory, SocketFlags.None, cancelToken);

            var result = await InternalSocket.ReceiveAsync(_recieve_buffer_memory, SocketFlags.None, cancelToken);

            SocketAsyncEventArgs disconnectArgs = new SocketAsyncEventArgs();
            CancellationTokenSource cancellationSource = new();
            disconnectArgs.Completed += (sender, args) => cancellationSource.Cancel();
            if (InternalSocket.DisconnectAsync(disconnectArgs))
            {
                while (!cancellationSource.IsCancellationRequested) { }
            }

            if (!disconnectArgs.DisconnectReuseSocket)
            {
                throw BadDisconnect();
            }

            if (_recieve_buffer[0] == 0)
            {
                if (_recieve_buffer[1] == byte.MaxValue)
                {
                    return true;
                }
                else
                {
                    throw PingReplyIncorrect();
                }
            }
            else
            {
                throw PingFormatIncorrect();
            }
        }



        // Server Only

        //TODO: \/ (may need to change SocketReceiveFromResult to IPEndPoint in parent class!!!
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
            if (Mode == TcpCommunicatorMode.Client)
            {
                throw new InvalidOperationException("TCP clients cannot use a connection parsing loop. This is only avalible for servers when using TCP.");
            }

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
        //TODO: /\



        // Not Supported Methods

        public override async Task TargetedSendOneAsync(NetworkedDataObject[] data, IPEndPoint target, CancellationToken cancelToken)
        {
            throw new NotSupportedException("Standalone sending to a specific target is not supported for TCP.");
        }

        public override async Task<bool> TargetedPingAsync(IPEndPoint target, CancellationToken cancelToken, bool reply = false)
        {
            throw new NotSupportedException("TCP pings can only be sent by clients and explicit targets are not supported.");
        }
    }
}
