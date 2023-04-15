using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;

namespace network_objects
{
    /// <summary>
    /// Based on https://enclave.io/high-performance-udp-sockets-net6/
    /// </summary>
    public class Communicator
    {
        public static readonly IPEndPoint BlankEndpoint = new IPEndPoint(IPAddress.Any, 0);

        public int Port { get; set; }
        public bool IsDefaultAddressSet { get; private set; } = true;
        public IPEndPoint DefaultDestinationEndpoint { get; private set; }
        public IPAddress DefaultAddress => DefaultDestinationEndpoint.Address;
        public int DefaultPort => DefaultDestinationEndpoint.Port;

        public int PingWaitMillis { get; set; } = 5000;

        private CancellationTokenSource internalCancellationSource = new CancellationTokenSource();
        private CancellationToken internalCancellationToken => internalCancellationSource.Token;

        private Socket _socket;
        private SerialisationManager _manager = new SerialisationManager();

        private byte[] _send_buffer;
        private Memory<byte> _send_buffer_memory;
        private Thread _sender_thread;
        public bool SenderRunning { get; private set; }

        private byte[] _recieve_buffer;
        private Memory<byte> _recieve_buffer_memory;
        private Thread _reciever_thread;
        public bool RecieverRunning { get; private set; }

        public event Action<SocketReceiveFromResult> OnPing;
        private event Action OnPingReturn;
        public event Action<Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult>> OnRecieveMessage;
        public event Action OnKill;

        private Queue<Tuple<IPEndPoint?, NetworkedDataObject[]>> message_queue = new Queue<Tuple<IPEndPoint?, NetworkedDataObject[]>>(10);



        public Communicator(int client_port, CancellationToken? cancelToken = null) : this(client_port, 0, IPAddress.Any, cancelToken)
        {
            IsDefaultAddressSet = false;
        }

        public Communicator(int client_port, int destination_port, IPAddress destination_address, CancellationToken? cancelToken = null)
        {
            if (cancelToken != null)
            {
                ((CancellationToken)cancelToken).Register(Kill);
            }

            Port = client_port;

            DefaultDestinationEndpoint = new IPEndPoint(destination_address, destination_port);

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.Bind(new IPEndPoint(IPAddress.Any, Port));

            _send_buffer = GC.AllocateArray<byte>(length: NetworkSettings.PACKET_BYTES, pinned: true);
            _recieve_buffer = GC.AllocateArray<byte>(length: NetworkSettings.PACKET_BYTES, pinned: true);

            _send_buffer_memory = _send_buffer.AsMemory();
            _recieve_buffer_memory = _recieve_buffer.AsMemory();

            _sender_thread = new Thread(_sender);
            _reciever_thread = new Thread(_reciever);
        }

        public void Kill()
        {
            internalCancellationSource.Cancel();
            Thread.Sleep(1000);// Wait to allow time for everything to stop
            _socket.Close();//TODO: nessessary???
            _socket.Dispose();
            OnKill?.Invoke();
        }

        ~Communicator()
        {
            Kill();
        }

        public async Task CompleteOnKill()
        {
            TaskCompletionSource completionSource = new TaskCompletionSource();
            Action completion_handeler = () => completionSource.SetResult();
            OnKill += completion_handeler;
            Task.WaitAll(new Task[] { completionSource.Task });
            OnKill -= completion_handeler;
        }



        public async Task SendOneAsync(NetworkedDataObject[] data, CancellationToken cancelToken)
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

                await _socket.SendToAsync(_send_buffer_memory, SocketFlags.None, DefaultDestinationEndpoint, cancelToken);
            }
            else
            {
                throw new InvalidOperationException("Object was not initialised with a default target.");
            }
        }

        public async Task TargetedSendOneAsync(network_objects.NetworkedDataObject[] data, IPEndPoint target, CancellationToken cancelToken)
        {
            if (data.Length == 0)
            {
                await TargetedPingAsync(target, cancelToken);
                return;
            }

            Array.Clear(_send_buffer);

            network_objects.SerialisationManager.SerialiseObjects(ref data, ref _send_buffer);

            await _socket.SendToAsync(_send_buffer_memory, SocketFlags.None, target, cancelToken);
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

        public async Task<bool> PingAsync(CancellationToken cancelToken, bool reply = false)
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

        public async Task<bool> TargetedPingAsync(IPEndPoint target, CancellationToken cancelToken, bool reply = false)
        {
            if (!reply && !RecieverRunning)
            {
                throw new InvalidOperationException("Unable to send a ping. This requires the communicator to be activley processing incoming data. Call StartReciever first.");
            }

            Array.Clear(_send_buffer);

            _send_buffer[0] = (byte)0;
            _send_buffer[1] = ((reply) ? byte.MaxValue : (byte)0);

            await _socket.SendToAsync(_send_buffer_memory, SocketFlags.None, target, cancelToken);

            if (!reply)
            {
                return GetPingResult();
            }
            else
            {
                return true;
            }
        }

        private void ParseMessage(Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult> data, Dictionary<int, Type> message_type_lookup, Action<Message, SocketReceiveFromResult> callback)
        {
            Message new_message;

            var constructor = message_type_lookup[((Integer16)data.Item2[0]).Value].GetConstructor(Type.EmptyTypes);
            if (constructor != null)
            {
                new_message = (Message)constructor.Invoke(null);
            }
            else
            {
                throw new InvalidOperationException("Type provided is not compatible. It must define a blank constructor.");
            }

            Message.Populate(new_message, data.Item1[1..], data.Item2[1..]);
            
            callback.Invoke(new_message, data.Item3);
        }

        public void ParseMessageOnReceive(Dictionary<int, Type> message_type_lookup, Action<Message, SocketReceiveFromResult> callback)
        {
            OnRecieveMessage += (Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult> data) => ParseMessage(data, message_type_lookup, callback);
        }

        public async Task<Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult>> ReciveOneAsync(CancellationToken cancelToken)
        {
            var result = await _socket.ReceiveFromAsync(_recieve_buffer_memory, SocketFlags.None, BlankEndpoint, cancelToken);

            var object_data = _manager.GetAllObjects(ref _recieve_buffer);

            return new Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult>(object_data.Item1, object_data.Item2, result);
        }

        public async Task DoReceiveAsync(Action<Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult>> handeler, CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _socket.ReceiveFromAsync(_recieve_buffer_memory, SocketFlags.None, BlankEndpoint, cancelToken);

                    var object_data = _manager.GetAllObjects(ref _recieve_buffer);

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
                            OnPing?.Invoke(result);
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
        }



        private async void PingReply(SocketReceiveFromResult sender)
        {
            await TargetedPingAsync((IPEndPoint)sender.RemoteEndPoint, internalCancellationToken, reply: true);
        }

        private async void _reciever()
        {
            OnPing += PingReply;
            await DoReceiveAsync((Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult> message) => { OnRecieveMessage?.Invoke(message); }, internalCancellationToken);
            OnPing -= PingReply;
        }

        public void StartReciever()
        {
            if (_reciever_thread.ThreadState == ThreadState.Unstarted)
            {
                _reciever_thread.Start();
                RecieverRunning = true;
            }
        }

        private async void _sender()
        {
            Tuple<IPEndPoint?, network_objects.NetworkedDataObject[]> message_data;
            while (!internalCancellationToken.IsCancellationRequested)
            {
                if (message_queue.TryDequeue(out message_data))
                {
                    if (message_data.Item1 == null)
                    {
                        await SendOneAsync(message_data.Item2, internalCancellationToken);
                    }
                    else
                    {
                        await TargetedSendOneAsync(message_data.Item2, message_data.Item1, internalCancellationToken);
                    }
                }
            }
        }

        public void StartSender()
        {
            if (_sender_thread.ThreadState == ThreadState.Unstarted)
            {
                _sender_thread.Start();
                SenderRunning = true;
            }
        }

        public void QueueMessage(Tuple<IPEndPoint?, NetworkedDataObject[]> message)
        {
            message_queue.EnsureCapacity(message_queue.Count + 1);
            message_queue.Enqueue(message);
        }

        public void QueueMessage(IPEndPoint target, NetworkedDataObject[] message_data)
        {
            QueueMessage(new Tuple<IPEndPoint?, NetworkedDataObject[]>(target, message_data));
        }

        public void QueueMessage(IPEndPoint target, NetworkedDataObject message_data)
        {
            QueueMessage(new Tuple<IPEndPoint?, NetworkedDataObject[]>(target, new NetworkedDataObject[] { message_data }));
        }

        public void QueueMessage(NetworkedDataObject[] message_data)
        {
            QueueMessage(new Tuple<IPEndPoint?, NetworkedDataObject[]>(null, message_data));
        }

        public void QueueMessage(NetworkedDataObject message_data)
        {
            QueueMessage(new NetworkedDataObject[] { message_data });
        }

        public void QueueMessage(Message message_object)
        {
            QueueMessage(message_object.GetData());
        }
    }
}
