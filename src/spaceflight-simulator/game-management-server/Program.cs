using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace game_management_server
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
            Console.WriteLine("Listening on 0.0.0.0:9999");
            network_objects.DataReciver reciver = new network_objects.DataReciver(9999);

            await reciver.DoReciveAsync(new Action<Tuple<Type[], network_objects.NetworkedDataObject[], SocketReceiveFromResult>>(PrintX), CancelToken);
        }

        private static void PrintX(Tuple<Type[], network_objects.NetworkedDataObject[], SocketReceiveFromResult> data)
        {
            Console.WriteLine(((network_objects.Vector3)data.Item2[0]).x);
        }
    }
}