using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
                    Server.Connect(IPAddress.Parse("212.92.229.142"), 57650);
                    break;
                }
                catch
                {
                    // ignored
                }
            }
        }
        
        
        public static BasePacket SendWithResult(BasePacket data)
        {
            Protocol.SendTcp(Server, data);
            while (true)
            {
                var result = Protocol.ReceiveTcp(Server);

                if (result.Command == Commands.Ping)
                {
                    Protocol.SendTcp(Server, new BasePacket(Commands.Ping));
                    continue;
                }
                
                return result;
            }
        }

        public static BasePacket Receive()
        {
            while (true)
            {
                var command = Protocol.ReceiveTcp(Server);
                if (command.Command == Commands.Ping)
                {
                    Protocol.SendTcp(Server, new BasePacket(Commands.Ping));
                    continue;
                }

                return command;
            }
        }

        public static void Send(BasePacket data)
        {
            Protocol.SendTcp(Server, data);
        }
        
        public static void SendWithErrorAnswer(BasePacket data)
        {
            Protocol.SendTcp(Server, data);
            while (true)
            {
                var command = Protocol.ReceiveTcp(Server);
                if (command.Command == Commands.Ok) return;
                if (command.Command == Commands.Error)
                    if (((ErrorPacket) command).Error == Errors.NameIsTaken)
                        throw new Exception(((ErrorPacket) command).Error.ToString());

                if (command.Command == Commands.Ping)
                {
                    Protocol.SendTcp(Server, new BasePacket(Commands.Ping));
                    continue;
                }
                
                throw new Exception("Unexpected command received");
            }
        }
    }
}
