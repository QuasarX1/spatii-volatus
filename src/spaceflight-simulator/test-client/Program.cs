using System.Net.Sockets;
using System.Net;

namespace test_client
{
    internal class Program
    {
        private const int PacketSize = 1380;

        static async Task Main(string[] args)
        {
            using var udpSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            // Get a cancel source that cancels when the user presses CTRL+C.
            var userExitSource = GetUserConsoleCancellationSource();

            var cancelToken = userExitSource.Token;

            // Discard our socket when the user cancels.
            using var cancelReg = cancelToken.Register(() => udpSocket.Dispose());



            IPAddress destination = IPAddress.Parse("192.168.0.20");
            Console.WriteLine($"Sending to {destination}:9999");
            await DoSendAsync(udpSocket, new IPEndPoint(destination, 9999), cancelToken);
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

        private static async Task DoSendAsync(Socket udpSocket, IPEndPoint destination, CancellationToken cancelToken)
        {
            // Taking advantage of pre-pinned memory here using the .NET 5 POH (pinned object heap).            
            byte[] buffer = GC.AllocateArray<byte>(PacketSize, pinned: true);
            Memory<byte> bufferMem = buffer.AsMemory();

            network_objects.NetworkedDataObject[] test_data = new network_objects.NetworkedDataObject[] { new network_objects.Vector3(50, 0, 0) };

            network_objects.SerialisationManager.SerialiseObjects(ref test_data, ref buffer);

            while (!cancelToken.IsCancellationRequested)
            {
                await udpSocket.SendToAsync(bufferMem, SocketFlags.None, destination, cancelToken);
            }
        }
    }
}