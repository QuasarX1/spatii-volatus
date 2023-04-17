using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;

using spaceflight_simulator.network_objects.datatypes;

namespace spaceflight_simulator.network_objects
{
    /// <summary>
    /// Based on https://enclave.io/high-performance-udp-sockets-net6/
    /// </summary>
    public abstract class Communicator
    {
        public int Port { get; set; }
        public bool IsDefaultAddressSet { get; private set; } = true;
        public IPEndPoint DefaultDestinationEndpoint { get; private set; }
        public IPAddress DefaultAddress => DefaultDestinationEndpoint.Address;
        public int DefaultPort => DefaultDestinationEndpoint.Port;

        public int PingWaitMillis { get; set; } = NetworkSettings.PING_DEFAULT_WAIT_MILLIS;

        protected CancellationTokenSource _internalCancellationSource { get; private set; } = new CancellationTokenSource();
        protected CancellationToken InternalCancellationToken => _internalCancellationSource.Token;

        protected Socket InternalSocket;
        protected SerialisationManager InternalSerialisationManager = new SerialisationManager();

//        private byte[] _send_buffer;
//        private Memory<byte> _send_buffer_memory;
        private Thread _sender_thread;
        public bool SenderRunning { get; private set; }

//        private byte[] _recieve_buffer;
//        private Memory<byte> _recieve_buffer_memory;
        private Thread _reciever_thread;
        public bool RecieverRunning { get; private set; }

        public event Action<SocketReceiveFromResult> OnPing;
        public event Action<Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult>> OnRecieveMessage;
        public event Action OnKill;
        public event Action OnKillComplete;

        private Queue<Tuple<IPEndPoint?, NetworkedDataObject[]>> message_queue = new Queue<Tuple<IPEndPoint?, NetworkedDataObject[]>>(10);



        public Communicator(int client_port, CancellationToken? cancelToken = null) : this(client_port, 0, IPAddress.Any, cancelToken)
        {
            IsDefaultAddressSet = false;
        }

        public Communicator(int client_port, int destination_port, IPAddress destination_address, CancellationToken? cancelToken = null)
        {
            cancelToken?.Register(Kill);

            Port = client_port;

            DefaultDestinationEndpoint = new IPEndPoint(destination_address, destination_port);

//            InternalSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
//            //_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//            InternalSocket.Bind(new IPEndPoint(IPAddress.Any, Port));

//            _send_buffer = GC.AllocateArray<byte>(length: NetworkSettings.PACKET_BYTES, pinned: true);
//            _recieve_buffer = GC.AllocateArray<byte>(length: NetworkSettings.PACKET_BYTES, pinned: true);

//            _send_buffer_memory = _send_buffer.AsMemory();
//            _recieve_buffer_memory = _recieve_buffer.AsMemory();

            _sender_thread = new Thread(_sender);
            _reciever_thread = new Thread(_reciever);
        }

        public void Kill()
        {
            _internalCancellationSource.Cancel();

            OnKill?.Invoke();

            Thread.Sleep(1000);// Wait to allow time for everything to stop on worker threads

            InternalSocket.Shutdown(SocketShutdown.Both);
            InternalSocket.Close();
            InternalSocket.Dispose();

            OnKillComplete?.Invoke();
        }

        ~Communicator()
        {
            Kill();
        }

        public async Task CompleteOnKill()
        {
            TaskCompletionSource completionSource = new TaskCompletionSource();
            Action completion_handeler = () => completionSource.SetResult();
            OnKillComplete += completion_handeler;
            Task.WaitAll(new Task[] { completionSource.Task });
            OnKillComplete -= completion_handeler;
        }



        public abstract Task SendOneAsync(NetworkedDataObject[] data, CancellationToken cancelToken);
        public abstract Task TargetedSendOneAsync(NetworkedDataObject[] data, IPEndPoint target, CancellationToken cancelToken);
        public abstract Task<bool> PingAsync(CancellationToken cancelToken, bool reply = false);
        public abstract Task<bool> TargetedPingAsync(IPEndPoint target, CancellationToken cancelToken, bool reply = false);
        public abstract Task<Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult>> ReciveOneAsync(CancellationToken cancelToken);
        public abstract Task DoReceiveAsync(Action<Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult>> handeler, CancellationToken cancelToken);



        private async void _reciever()
        {
            RecieverRunning = true;
            await DoReceiveAsync((Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult> message) => { OnRecieveMessage?.Invoke(message); }, InternalCancellationToken);
            RecieverRunning = false;
        }

        public void StartReciever()
        {
            if (!RecieverRunning)
            {
                _reciever_thread.Start();
            }
        }

        private async void _sender()
        {
            SenderRunning = true;

            Tuple<IPEndPoint?, NetworkedDataObject[]> message_data;
            while (!InternalCancellationToken.IsCancellationRequested)
            {
                if (message_queue.TryDequeue(out message_data))
                {
                    if (message_data.Item1 == null)
                    {
                        await SendOneAsync(message_data.Item2, InternalCancellationToken);
                    }
                    else
                    {
                        await TargetedSendOneAsync(message_data.Item2, message_data.Item1, InternalCancellationToken);
                    }
                }
            }

            SenderRunning = false;
        }

        public void StartSender()
        {
            if (!SenderRunning)
            {
                _sender_thread.Start();
            }
        }

        public void QueueMessage(Tuple<IPEndPoint?, NetworkedDataObject[]> message)
        {
            message_queue.EnsureCapacity(message_queue.Count + 1);
            message_queue.Enqueue(message);
        }

        public void QueueMessage(IPEndPoint target, NetworkedDataObject[] message_data)
            => QueueMessage(new Tuple<IPEndPoint?, NetworkedDataObject[]>(target, message_data));

        public void QueueMessage(IPEndPoint target, NetworkedDataObject message_data)
            => QueueMessage(new Tuple<IPEndPoint?, NetworkedDataObject[]>(target, new NetworkedDataObject[] { message_data }));

        public void QueueMessage(NetworkedDataObject[] message_data)
            => QueueMessage(new Tuple<IPEndPoint?, NetworkedDataObject[]>(null, message_data));

        public void QueueMessage(NetworkedDataObject message_data)
            => QueueMessage(new NetworkedDataObject[] { message_data });

        public void QueueMessage(IPEndPoint target, Message message_object)
            => QueueMessage(target, message_object.GetData());
        
        public void QueueMessage(Message message_object)
            => QueueMessage(message_object.GetData());

        protected void OnPing_Invoke(SocketReceiveFromResult sender_information)
        {
            OnPing?.Invoke(sender_information);
        }

        protected void ParseMessage(Tuple<Type[], NetworkedDataObject[], SocketReceiveFromResult> data, Dictionary<int, Type> message_type_lookup, Action<Message, SocketReceiveFromResult> callback)
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
    }
}
