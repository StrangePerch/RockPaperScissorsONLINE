using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NetLib
{
    public record Client
    {
        public TcpClient TcpClient { get; set; }
        public string Username { get; set; }

        public Client(TcpClient client, string username) => (TcpClient, Username) = (client, username);
    }

    public enum Commands { Ping, UserData, Ok, Host, Error, RequestGames, Games, Move,
        Connect, GameInfo, PlayerInfo, Close}

    public enum Errors { NameIsTaken }
    
    public enum Move { Rock, Paper, Scissors }
    
    [Serializable]
    public record BasePacket
    {
        public Commands Command { get; set; }

        public BasePacket(Commands command) => Command = command;

        public BasePacket() { }
    }

    [Serializable]
    public record ConnectPacket : BasePacket
    {
        public string GameName { get; set; }

        public ConnectPacket(string gameName) : base(Commands.Connect) => GameName = gameName;

        public ConnectPacket() { }
    }

    [Serializable]
    public record ErrorPacket : BasePacket
    {
        public Errors Error { get; set; }

        public ErrorPacket(Errors error) : base(Commands.Error) => Error = error;

        public ErrorPacket() { }
    }

    [Serializable]
    public record MovePacket : BasePacket
    {
        public Move Move { get; set; }
        
        public string GameName { get; set; }

        public bool Host { get; set; }

        public MovePacket(Move move, string gameName, bool host) : base(Commands.Move) => (Move, GameName, Host) = (move, gameName, host);

        public MovePacket() { }
    }

    [Serializable]
    public record UserDataPacket : BasePacket
    {
        public string Username { get; set; }

        public UserDataPacket(string username) : base(Commands.UserData) => Username = username;
        
        public UserDataPacket() { }
    }

    [Serializable]
    public record HostGamePacket : BasePacket
    {
        public string Name { get; set; }
        public int MaxScore { get; set; }

        public HostGamePacket(string name, int score) : base(Commands.Host) => (Name, MaxScore) = (name, score);

        public HostGamePacket() { }
    }

    [Serializable]
    public record CloseGamePacket : BasePacket
    {
        public string Name { get; set; }

        public CloseGamePacket(string name) : base(Commands.Close) => Name = name;

        public CloseGamePacket() { }
    }

    [Serializable]
    public record GamesPacket : BasePacket
    {
        public List<GamePacket> Games { get; set; }

        public GamesPacket() : base(Commands.Games)
        {
            Games = new List<GamePacket>();
        }
    }
    
    [Serializable]
    public record GamePacket : BasePacket
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public int MaxScore { get; set; }


        public GamePacket(string name, string host, int score) : base(Commands.GameInfo)
        {
            Name = name;
            Host = host;
            MaxScore = score;
        }
    }

    [Serializable]
    public record PlayerPacket : BasePacket
    {
        public string Name { get; set; }

        public PlayerPacket(string name) : base(Commands.PlayerInfo)
        {
            Name = name;
        }
    }

    public static class Protocol
    {
        public static BinaryFormatter Formatter = new ();
        public static void SendTcp(TcpClient client, BasePacket data)
        {
            var stream = client.GetStream();
            stream.Flush();
            //XmlSerializer serializer = new XmlSerializer(typeof(BasePacket));
            //serializer.Serialize(stream, data);
            Formatter.Serialize(stream,data);
        }
        
        public static BasePacket ReceiveTcp(TcpClient client)
        {
            client.ReceiveTimeout = -1;
            var stream = client.GetStream();
            //XmlSerializer serializer = new XmlSerializer(typeof(BasePacket));
            //return (BasePacket)serializer.Deserialize(stream);
            return (BasePacket)Formatter.Deserialize(stream);
        }

        public static async void Ping(Client client, ICollection<Client> collection)
        {
            await Task.Run(() =>
            {
                try
                {
                    SendTcp(client.TcpClient, new BasePacket(Commands.Ping));
                    client.TcpClient.ReceiveTimeout = 3000;
                    return;
                }
                catch
                {
                    // ignored
                }

                collection.Remove(client);
                Console.WriteLine($"{client.Username} Disconnected!");
            });
        }
    }
}
