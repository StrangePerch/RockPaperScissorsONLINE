#nullable enable
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NetLib;

namespace Server
{
    static class Program
    {
        private static readonly List<Client> ConnectedClients = new ();

        private static readonly Dictionary<string?, Game> Games = new ();

        private static Timer _pingTimer = new (PingAll, null, 0, 1000);

        private static TcpListener? _listener;
        
        static void Main(string[] args)
        {
            Task[] tasks = new Task[1];
            _listener = TcpListener.Create(57650);
            _listener.Start();
            tasks[0] = AcceptClients();
            Console.WriteLine("Server started on 57650");
            Task.WaitAll(tasks);
        }

        static void PingAll(object? obj)
        {
            foreach (var client in ConnectedClients)
            {
                Protocol.Ping(client, ClientDisconnected);
            }
        }

        static void ClientDisconnected(Client client)
        {
            ClientSurrender(client);
            ConnectedClients.Remove(client);
            Console.WriteLine($"{client.Username} Disconnected!");
        }

        static void ClientSurrender(Client client)
        {
            foreach (var keyValuePair in Games)
            {
                var valueHost = keyValuePair.Value.Host;
                var valueClient = keyValuePair.Value.Client;

                if(valueHost == null || valueClient == null) continue;
                if (valueHost == client)
                {
                    Protocol.SendTcp(valueClient.TcpClient, new Surrender());
                }
                else if (valueClient == client)
                {
                    Protocol.SendTcp(valueHost.TcpClient, new Surrender());
                }
            }
        }
        static async Task AcceptCommands(Client client)
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    while (client.TcpClient.Available == 0)
                    {
                        
                    }
                    var command = Protocol.ReceiveTcp(client.TcpClient);
                    bool usernameIsTaken = false;
                    switch (command)
                    {
                        case UserDataPacket data:
                            usernameIsTaken = false;
                            foreach (var client1 in ConnectedClients)
                            {
                                if (client1.Username == data?.Username)
                                {
                                    usernameIsTaken = true;
                                    break;
                                }
                            }

                            if (!usernameIsTaken)
                            {
                                client.Username = data?.Username;
                                Protocol.SendTcp(client.TcpClient, new Ok());
                                Console.WriteLine($"{client.Username} connected!");
                            }
                            else
                            {
                                Protocol.SendTcp(client.TcpClient, new ErrorPacket(Errors.NameIsTaken));
                            }

                            break;
                        case HostGamePacket hostData:

                            usernameIsTaken = false;
                            foreach (var game in Games)
                            {
                                if (hostData?.Name == game.Key)
                                {
                                    usernameIsTaken = true;
                                    break;
                                }
                            }

                            if (!usernameIsTaken)
                            {
                                Games.Add(hostData?.Name, new Game(hostData?.Name, client, hostData.MaxScore));
                                Protocol.SendTcp(client.TcpClient, new Ok());
                                Console.WriteLine($"{client.Username} hosted game {hostData?.Name}!");
                            }
                            else
                            {
                                Protocol.SendTcp(client.TcpClient, new ErrorPacket(Errors.NameIsTaken));
                            }
                            break;
                        case CloseGamePacket close:
                            Games.Remove(close.Name);
                            break;
                        case GamesPacket packet:
                            foreach (var game in Games)
                            {
                                if(game.Value.Client == null)
                                    packet.Games.Add(game.Value.ToGamePacket());
                            }
                            Protocol.SendTcp(client.TcpClient, packet);
                            break;
                        case PingPacket:
                            break;
                        case MovePacket move:

                            switch (move.Host)
                            {
                                case true when Games[move.GameName].Client != null:
                                    Protocol.SendTcp(Games[move.GameName].Client.TcpClient, move);
                                    break;
                                case false when Games[move.GameName].Host != null:
                                    Protocol.SendTcp(Games[move.GameName].Host.TcpClient, move);
                                    break;
                            }

                            break;
                        case Surrender:
                            ClientSurrender(client);
                            break;
                        case ConnectPacket connect:
                            if (connect == null) break;
                            Games[connect.GameName].Client = client;
                            Protocol.SendTcp(client.TcpClient, Games[connect.GameName].ToGamePacket());
                            Protocol.SendTcp(Games[connect.GameName].Host.TcpClient, new PlayerPacket(client.Username));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            });
        }

        static async Task AcceptClients()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    var tcpClient = _listener?.AcceptTcpClient();
                    var client = new Client(tcpClient, null);
                    AcceptCommands(client);
                    ConnectedClients.Add(client);
                    Console.WriteLine($"{client.TcpClient.Client.RemoteEndPoint} connected");
                }
            });
        }
    }

    internal record Game
    {
        public Game(string? name, Client host, int score)
        {
            Host = host;
            Name = name;
            MaxScore = score;
        }

        public string Name { get; set; }
        public int MaxScore { get; set; }
        public Client Host { get; set; }
        public Client Client { get; set; }

        public GamePacket ToGamePacket()
        {
            return new(Name, Host.Username, MaxScore);
        }
    }
}
