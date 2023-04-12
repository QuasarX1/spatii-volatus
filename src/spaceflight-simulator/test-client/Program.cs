using System.Net.Sockets;
using System.Net;

namespace test_client
{
    internal class Program
    {
        public static readonly CancellationTokenSource ConsoleExitTokenSource = GetUserConsoleCancellationSource();
        public static readonly CancellationToken CancelToken = ConsoleExitTokenSource.Token;

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
        static async Task Main(string[] args)
        {
            IPAddress destination = IPAddress.Parse("192.168.0.20");
            network_objects.DataSender sender = new network_objects.DataSender(9999, destination);
            network_objects.NetworkedDataObject[] test_data = new network_objects.NetworkedDataObject[] { new network_objects.Vector3(50, 0, 0) };
            Console.WriteLine($"Sending to {destination}:9999");
            while (!CancelToken.IsCancellationRequested)
            {
                await sender.DoSendOneAsync(test_data, CancelToken);
            }
        }
    }
}