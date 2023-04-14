using System.Diagnostics;
using System.Net.Sockets;
using System.Net;

using static network_objects.NetworkSettings;

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



        private const int PORT = DEFAULT_CLIENT_PORT;
        private static network_objects.Communicator communicator;

        static async Task Main(string[] args)
        {
            communicator = new network_objects.Communicator(PORT, SERVER_PORT, IPAddress.Parse("192.168.0.20"));
            //communicator.OnRecieveMessage += ;
            communicator.StartReciever();
            communicator.StartSender();

            network_objects.NetworkedDataObject[] test_data = new network_objects.NetworkedDataObject[] { new network_objects.Vector3(50, 0, 0), new network_objects.String("Hello World!") };

            Stopwatch stopwatch = new Stopwatch();

            while (!CancelToken.IsCancellationRequested)
            {
                Console.Write("Ping ");
                stopwatch.Start();
                if (await communicator.PingAsync(CancelToken))
                {
                    stopwatch.Stop();
                    Console.WriteLine($"Pong ({stopwatch.Elapsed.TotalMilliseconds} ms)");
                    stopwatch.Reset();
                }
                //Thread.Sleep(100);
            }

            communicator.Kill();
        }
    }
}