using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace spaceflight_simulator.network_objects
{
    public static class NetworkSettings
    {
        // Server Ports
        public const int SERVER_PORT_UDP = 2000;
        public const int SERVER_PORT_TCP = SERVER_PORT_UDP + 100;

        // CLient Ports
        public const int CLIENT_DEFAULT_PORT_UDP = 3000;
        public const int CLIENT_DEFAULT_PORT_TCP = CLIENT_DEFAULT_PORT_UDP + 100;

        // IP Addresses
        public static readonly IPEndPoint BLANK_IP_ENDPOINT = new IPEndPoint(IPAddress.Any, 0);

        // Packets
        public const int PACKET_BYTES = 1024;
        public const int TCP_SERVER_BACKLOG_MAX = 100;

        // Ping
        public const int PING_DEFAULT_WAIT_MILLIS = 5000;
    }
}
