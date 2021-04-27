using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NetLib;

namespace Client
{
    public static class ConnectionManager
    {
        public static TcpClient Server { get; set; }
        public static string ClientName { get; set; }
        public static void Connect()
        {
            Server = new TcpClient();
            while (true)
            {
                try
                {
                    Server.Connect(IPAddress.Parse("127.0.0.1"), 57650);
                    break;
                }
                catch
                {
                    // ignored
                }
            }
        }
        
        
        public static IBasePacket SendWithResult(IBasePacket data)
        {
            Protocol.SendTcp(Server, data);
            while (true)
            {
                var result = Protocol.ReceiveTcp(Server);

                if (result is PingPacket)
                {
                    Protocol.SendTcp(Server, new PingPacket());
                    continue;
                }
                
                return result;
            }
        }

        public static IBasePacket Receive()
        {
            while (true)
            {
                var command = Protocol.ReceiveTcp(Server);
                if (command is PingPacket)
                {
                    Protocol.SendTcp(Server, new PingPacket());
                    continue;
                }

                return command;
            }
        }

        public static void Send(IBasePacket data)
        {
            Protocol.SendTcp(Server, data);
        }
        
        public static void SendWithErrorAnswer(IBasePacket data)
        {
            Protocol.SendTcp(Server, data);
            while (true)
            {
                var packet = Protocol.ReceiveTcp(Server);
                switch (packet)
                {
                    case PingPacket:
                        Protocol.SendTcp(Server, new PingPacket());
                        continue;
                    case ErrorPacket error:
                        throw new Exception(error.Error.ToString());
                        break;
                }

                if(((CommandPacket) packet).Command == Commands.Ok) return;

                throw new Exception("Unexpected command received");
            }
        }
    }
}
