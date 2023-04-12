using System.Net.Sockets;
using System.Net;

namespace test_client
{
    internal class Program
    {
        private const int PacketSize = 1380;

        static async Task Main(string[] args)
        {
            // Get a cancel source that cancels when the user presses CTRL+C.
            var userExitSource = GetUserConsoleCancellationSource();
            var cancelToken = userExitSource.Token;



            IPAddress destination = IPAddress.Parse("192.168.0.20");
            network_objects.DataSender sender = new network_objects.DataSender(destination, 9999);
            network_objects.NetworkedDataObject[] test_data = new network_objects.NetworkedDataObject[] { new network_objects.Vector3(50, 0, 0) };
            Console.WriteLine($"Sending to {destination}:9999");
            while (!cancelToken.IsCancellationRequested)
            {
                await sender.DoSendOneAsync(test_data, cancelToken);
            }
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
    }
}