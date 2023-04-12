using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace game_management_server
{
    internal class Program
    {
        private const int PacketSize = 1380;
        private static readonly IPEndPoint _blankEndpoint = new IPEndPoint(IPAddress.Any, 0);

        //static void Main(string[] args)
        //{
        //    Console.WriteLine("Hello, World!");
        //}

        static async Task Main(string[] args)
        {
            using var udpSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            // Get a cancel source that cancels when the user presses CTRL+C.
            var userExitSource = GetUserConsoleCancellationSource();

            var cancelToken = userExitSource.Token;

            // Discard our socket when the user cancels.
            using var cancelReg = cancelToken.Register(() => udpSocket.Dispose());



            udpSocket.Bind(new IPEndPoint(IPAddress.Any, 9999));

            Console.WriteLine("Listening on 0.0.0.0:9999");
            await DoReceiveAsync(udpSocket, cancelToken);
        }

        private static CancellationTokenSource GetUserConsoleCancellationSource()
        {
            var cancellationSource = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                cancellationSource.Cancel();
            };

            return cancellationSource;
        }

        private static async Task DoReceiveAsync(Socket udpSocket, CancellationToken cancelToken)
        {
            // Taking advantage of pre-pinned memory here using the .NET5 POH (pinned object heap).
            byte[] buffer = GC.AllocateArray<byte>(length: 65527, pinned: true);
            Memory<byte> bufferMem = buffer.AsMemory();

            network_objects.SerialisationManager manager = new network_objects.SerialisationManager();

            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    var result = await udpSocket.ReceiveFromAsync(bufferMem, SocketFlags.None, _blankEndpoint);

                    var objects = manager.GetAllObjects(ref buffer);//TODO: double check that the buffer variable gets updated by bufferMem

                    Console.WriteLine(((network_objects.Vector3)objects[0]).x);
                }
                catch (SocketException)
                {
                    // Socket exception means we are finished.
                    break;
                }
            }
        }

        private static async Task DoSendAsync(Socket udpSocket, IPEndPoint destination, CancellationToken cancelToken)
        {
            // Taking advantage of pre-pinned memory here using the .NET 5 POH (pinned object heap).            
            byte[] buffer = GC.AllocateArray<byte>(PacketSize, pinned: true);
            Memory<byte> bufferMem = buffer.AsMemory();

            // Put something approaching meaningful data in the buffer.
            for (var idx = 0; idx < PacketSize; idx++)
            {
                bufferMem.Span[idx] = (byte)idx;
            }

            while (!cancelToken.IsCancellationRequested)
            {
                network_objects.NetworkedDataObject[] test_data = new network_objects.NetworkedDataObject[] { new network_objects.Vector3(50, 0, 0) };

                network_objects.SerialisationManager.SerialiseObjects(ref test_data, ref buffer);

                await udpSocket.SendToAsync(bufferMem, SocketFlags.None, destination, cancelToken);
            }
        }
    }
}