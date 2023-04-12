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

        static async Task Main(string[] args)
        {
            // Get a cancel source that cancels when the user presses CTRL+C.
            var userExitSource = GetUserConsoleCancellationSource();
            var cancelToken = userExitSource.Token;

            Console.WriteLine("Listening on 0.0.0.0:9999");
            network_objects.DataReciver reciver = new network_objects.DataReciver(9999);

            await reciver.DoReciveAsync(new Action<Tuple<Type[], network_objects.NetworkedDataObject[]>>(PrintX), cancelToken);
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

        private static void PrintX(Tuple<Type[], network_objects.NetworkedDataObject[]> data)
        {
            Console.WriteLine(((network_objects.Vector3)data.Item2[0]).x);
        }
    }
}