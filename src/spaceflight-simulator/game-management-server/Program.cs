using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using static network_objects.NetworkSettings;

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

        private const int PORT = SERVER_PORT;
        private static network_objects.Communicator communicator;

        static async Task Main(string[] args)
        {
            communicator = new network_objects.Communicator(PORT, CancelToken);
            communicator.OnRecieveMessage += PrintX;
            communicator.StartReciever();
            communicator.StartSender();
            await communicator.CompleteOnKill();
        }

        private static void PrintX(Tuple<Type[], network_objects.NetworkedDataObject[], SocketReceiveFromResult> data)
        {
            Console.WriteLine(data.Item1[0]);
            Console.WriteLine(((network_objects.Vector3)data.Item2[0]).x);
            Console.WriteLine(data.Item1[1]);
            Console.WriteLine(((network_objects.String)data.Item2[1]).Data);
        }
    }
}