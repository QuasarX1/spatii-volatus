using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Numerics;

using spaceflight_simulator.network_objects;
using static spaceflight_simulator.network_objects.NetworkSettings;
using spaceflight_simulator.messages;

namespace spaceflight_simulator.ping_test_client
{
    internal class Program
    {
        public const int N_pings = 1000;



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



        private const int PORT = CLIENT_DEFAULT_PORT_UDP;
        private static Communicator communicator;

        static async Task Main(string[] args)
        {
            Console.WriteLine($"Connectingto server on {((args.Length > 0) ? args[0] : "192.168.0.20")}:{PORT}");
            communicator = new UdpCommunicator(PORT, SERVER_PORT_UDP, IPAddress.Parse((args.Length > 0) ? args[0] : "192.168.0.20"));
            communicator.PingWaitMillis = 1000;
            //communicator.OnRecieveMessage += ;
            communicator.StartReciever();
            communicator.StartSender();

            //network_objects.NetworkedDataObject[] test_data = new network_objects.NetworkedDataObject[] { new System.Numerics.Vector3(50, 0, 0), "Hello World!" };
            TestServerMessage testMessage = new TestServerMessage("Hello World!", new Vector3(0, 0, 0), new Vector3(10, 0, 0));
            communicator.QueueMessage(testMessage);

            

            Stopwatch stopwatch = new Stopwatch();

            int successes = 0;
            int failiours = 0;
            double[] times = new double[N_pings];
            for (int i = 0; i < N_pings; i++)
            {
                if (CancelToken.IsCancellationRequested)
                {
                    break;
                }

                Console.Write("Ping ");
                stopwatch.Start();
                if (await communicator.PingAsync(CancelToken))
                {
                    stopwatch.Stop();
                    Console.WriteLine($"Pong ({stopwatch.Elapsed.TotalMilliseconds} ms)");
                    times[successes] = stopwatch.Elapsed.TotalMilliseconds;
                    successes++;
                }
                else
                {
                    failiours++;
                }
                stopwatch.Reset();
            }

            Console.WriteLine();
            Console.WriteLine($"Total number of pings: {successes + failiours}");
            if (successes + failiours > 0)
            {
                Console.WriteLine($"Successfull pings: {successes}");
                Console.WriteLine($"Packet loss: {(double)100 * (double)failiours / ((double)successes + (double)failiours)}");
                if (successes > 0)
                {
                    Console.WriteLine($"Average responce time: {times[0..successes].Sum() / (double)successes} ms");
                }
            }

            communicator.Kill();
        }
    }
}