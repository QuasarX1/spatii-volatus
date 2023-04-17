using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using spaceflight_simulator.network_objects;
using static spaceflight_simulator.network_objects.NetworkSettings;
using spaceflight_simulator.messages;

namespace spaceflight_simulator.game_management_server
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

        private const int PORT = SERVER_PORT_UDP;
        private static Communicator communicator;

        static async Task Main(string[] args)
        {
            var message_types = Message.MakeLookup(new Type[] { typeof(TestServerMessage), typeof(LoginMessage) });

            communicator = new UdpCommunicator(PORT, CancelToken);
            communicator.ParseMessageOnReceive(message_types, PrintTestMessage);
            communicator.StartReciever();
            communicator.StartSender();

            //TODO: perhaps implement blocking command entry here to avoid waisting a thread on checking the exit condition?
            //TODO: use `communicator.OnKill` to trigger a cancelation token if the server decides to shut itsself

            Task exit_check = communicator.CompleteOnKill();

            //TODO: implement other cleanup here

            await exit_check;
        }

        private static void PrintTestMessage(Message message, SocketReceiveFromResult source)
        {
            Console.WriteLine(message);
        }
    }
}