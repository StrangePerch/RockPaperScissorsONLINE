#nullable enable
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NetLib;

namespace Server
{
    class Program
    {
        private static readonly List<Client> ConnectedClients = new ();

        private static readonly Dictionary<string, Game> Games = new ();

        private static Timer _pingTimer = new (PingAll, null, 0, 1000);

        private static TcpListener Listener;
        
        static void Main(string[] args)
        {
            Task[] tasks = new Task[1];
            Listener = TcpListener.Create(57650);
            Listener.Start();
            tasks[0] = AcceptClients();
            Console.WriteLine("Server started on 57650");
            Task.WaitAll(tasks);
        }

        static void PingAll(object? obj)
        {
            foreach (var client in ConnectedClients)
            {
                Protocol.Ping(client, ConnectedClients);
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
                    switch (command.Command)
                    {
                        case Commands.UserData:
                            var data = command as UserDataPacket;
                            usernameIsTaken = false;
                            foreach (var client in ConnectedClients)
                            {
                                if (client.Username == data.Username)
                                {
                                    usernameIsTaken = true;
                                    break;
                                }
                            }

                            if (!usernameIsTaken)
                            {
                                client.Username = data?.Username;
                                Protocol.SendTcp(client.TcpClient, new BasePacket(Commands.Ok));
                                Console.WriteLine($"{client.Username} connected!");
                            }
                            else
                            {
                                Protocol.SendTcp(client.TcpClient, new ErrorPacket(Errors.NameIsTaken));
                            }

                            break;
                        case Commands.Host:
                            
                            var hostData = command as HostGamePacket;

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
                                Protocol.SendTcp(client.TcpClient, new BasePacket(Commands.Ok));
                                Console.WriteLine($"{client.Username} hosted game {hostData?.Name}!");
                            }
                            else
                            {
                                Protocol.SendTcp(client.TcpClient, new ErrorPacket(Errors.NameIsTaken));
                            }
                            break;
                        case Commands.Close:
                            var close = command as CloseGamePacket;
                            Games.Remove(close.Name);
                            break;
                        case Commands.RequestGames:
                            GamesPacket packet = new GamesPacket();
                            foreach (var game in Games)
                            {
                                packet.Games.Add(game.Value.ToGamePacket());
                            }
                            Protocol.SendTcp(client.TcpClient, packet);
                            break;
                        case Commands.Ping:
                            break;
                        case Commands.Move:
                            
                            MovePacket? move = command as MovePacket;
                            if(move.Host && Games[move.GameName].Client != null)
                                Protocol.SendTcp(Games[move.GameName].Client.TcpClient, move);
                            else if (!move.Host && Games[move.GameName].Host != null)
                                Protocol.SendTcp(Games[move.GameName].Host.TcpClient, move);

                            break;
                        case Commands.Connect:
                            ConnectPacket connect = command as ConnectPacket;
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
                    var tcpClient = Listener.AcceptTcpClient();
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
