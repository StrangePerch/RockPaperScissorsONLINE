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

    public enum Errors { NameIsTaken }
    
    public enum Move { Rock, Paper, Scissors }
    
    public interface IBasePacket { }

    [Serializable]
    public record Ok : IBasePacket { }
    
    [Serializable]
    public record Surrender : IBasePacket { }
    
    [Serializable]
    public record PingPacket : IBasePacket { }

    [Serializable]
    public record ConnectPacket : IBasePacket
    {
        public string GameName { get; set; }

        public ConnectPacket(string gameName) => GameName = gameName;
        public ConnectPacket() { }
    }

    [Serializable]
    public record ErrorPacket : IBasePacket
    {
        public Errors Error { get; set; }
        public ErrorPacket(Errors error) => Error = error;
        public ErrorPacket() { }
    }

    [Serializable]
    public record MovePacket : IBasePacket
    {
        public Move Move { get; set; }
        
        public string GameName { get; set; }

        public bool Host { get; set; }

        public MovePacket(Move move, string gameName, bool host) => (Move, GameName, Host) = (move, gameName, host);

        public MovePacket() { }
    }

    [Serializable]
    public record UserDataPacket : IBasePacket
    {
        public string Username { get; set; }

        public UserDataPacket(string username) => Username = username;
        
        public UserDataPacket() { }
    }

    [Serializable]
    public record HostGamePacket : IBasePacket
    {
        public string Name { get; set; }
        public int MaxScore { get; set; }

        public HostGamePacket(string name, int score) => (Name, MaxScore) = (name, score);

        public HostGamePacket() { }
    }

    [Serializable]
    public record CloseGamePacket : IBasePacket
    {
        public string Name { get; set; }

        public CloseGamePacket(string name) => Name = name;

        public CloseGamePacket() { }
    }

    [Serializable]
    public record GamesPacket : IBasePacket
    {
        public List<GamePacket> Games { get; set; }

        public GamesPacket()
        {
            Games = new List<GamePacket>();
        }
    }
    
    [Serializable]
    public record GamePacket : IBasePacket
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public int MaxScore { get; set; }


        public GamePacket(string name, string host, int score)
        {
            Name = name;
            Host = host;
            MaxScore = score;
        }
    }

    [Serializable]
    public record PlayerPacket : IBasePacket
    {
        public string Name { get; set; }

        public PlayerPacket(string name)
        {
            Name = name;
        }
    }

    public static class Protocol
    {
        private static readonly BinaryFormatter Formatter = new ();
        public static void SendTcp(TcpClient client, IBasePacket data)
        {
            var stream = client.GetStream();
            stream.Flush();
            //XmlSerializer serializer = new XmlSerializer(typeof(BasePacket));
            //serializer.Serialize(stream, data);
            Formatter.Serialize(stream,data);
        }
        
        public static IBasePacket ReceiveTcp(TcpClient client)
        {
            client.ReceiveTimeout = -1;
            var stream = client.GetStream();
            //XmlSerializer serializer = new XmlSerializer(typeof(BasePacket));
            //return (BasePacket)serializer.Deserialize(stream);
            return (IBasePacket)Formatter.Deserialize(stream);
        }

        public static async void Ping(Client client, Action<Client> handler)
        {
            await Task.Run(() =>
            {
                try
                {
                    SendTcp(client.TcpClient, new PingPacket());
                    client.TcpClient.ReceiveTimeout = 3000;
                    return;
                }
                catch
                {
                    // ignored
                }
                
                handler(client);
            });
        }
    }
}
