using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public enum MessageType
{
    ServerNotification,
    Message,
    ConnectedUsers,
    Image,
    Video
}

public class Message
{
    public MessageType Type { get; set; }
    public byte[] Content { get; set; }

    public Message (MessageType type, string content)
    {
        Type = type;
        Content = Encoding.UTF8.GetBytes (content);
    }
    public Message(MessageType type, List<string> content)
    {
        Type = type;    
        Content = ConvertListToByteArray(content);
    }
    public Message(MessageType type, byte[] content)
    {
        Type = type;
        Content = content;
    }
    public string Serialize()
    {
        return $"{(int)Type}#{Convert.ToBase64String(Content)}";
    }
    static byte[] ConvertListToByteArray(List<string> stringList)
    {
        // Use UTF-8 encoding to convert each string to bytes
        List<byte[]> stringBytesList = new List<byte[]>();
        foreach (string str in stringList)
        {
            byte[] strBytes = Encoding.UTF8.GetBytes(str);
            stringBytesList.Add(strBytes);
        }

        // Concatenate the byte arrays into a single byte array
        int totalLength = stringBytesList.Sum(arr => arr.Length);
        byte[] result = new byte[totalLength];
        int offset = 0;
        foreach (byte[] strBytes in stringBytesList)
        {
            Buffer.BlockCopy(strBytes, 0, result, offset, strBytes.Length);
            offset += strBytes.Length;
        }

        return result;
    }
}

public class Client
{
    private readonly ClientHandler clientHandler;
    private string username;

    public Client(ClientHandler clientHandler, string username)
    {
        this.clientHandler = clientHandler;
        Username = username;
    }

    public string Username { get => username; set => username = value; }

    public ClientHandler ClientHandler => clientHandler;
}

public class ChatServer
{
    private readonly List<Client> clients = new List<Client>();
    private readonly TcpListener listener;
    private readonly object clientsLock = new object();

    public ChatServer(IPAddress ipAddress, int port)
    {
        listener = new TcpListener(ipAddress, port);
    }

    public void Start()
    {
        listener.Start();
        Console.WriteLine("Server started. Waiting for clients...");

        while (true)
        {
            TcpClient tcpClient = listener.AcceptTcpClient();
            Console.WriteLine("Client connected.");

            ClientHandler clientHandler = new ClientHandler(tcpClient, this);

            lock (clientsLock)
            {
                clients.Add(new Client(clientHandler, null));
            }

            Thread clientThread = new Thread(() => clientHandler.HandleClient(clients[clients.Count - 1]));
            clientThread.Start();
        }
    }

    public void BroadcastMessage(Message message, ClientHandler sender)
    {
        lock (clientsLock)
        {
            foreach (var client in clients)
            {
                client.ClientHandler.SendMessage(message);
            }
        }
    }

    public void RemoveClient(Client client)
    {
        lock (clientsLock)
        {
            clients.Remove(client);
        }
    }
}

public class ClientHandler
{
    private readonly TcpClient client;
    private readonly ChatServer server;
    private readonly NetworkStream stream;
    private readonly StreamReader reader;
    private readonly StreamWriter writer;

    public ClientHandler(TcpClient client, ChatServer server)
    {
        this.client = client;
        this.server = server;
        stream = client.GetStream();
        reader = new StreamReader(stream, Encoding.UTF8);
        writer = new StreamWriter(stream, Encoding.UTF8);
    }

    public void HandleClient(Client client)
    {
        try
        {
            client.Username = reader.ReadLine();
            Console.WriteLine($"{client.Username} joined the chat. {client.ClientHandler.client.Client.RemoteEndPoint}");
            server.BroadcastMessage(new Message(MessageType.ServerNotification, $"{client.Username} joined the chat." ), this);

            while (true)
            {
                string message = reader.ReadLine();
                if (message == null)
                    break;
                Message messagee = new Message(MessageType.Message, $"{client.Username}: {message}");
                Console.WriteLine(message);
                Console.WriteLine(messagee.Type);
                Console.WriteLine(messagee.Content);
                server.BroadcastMessage(messagee, this);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            string tempUsername = client.Username;
            server.RemoveClient(client);

            // Properly close network resources
            reader.Close();
            writer.Close();
            stream.Close();

            server.BroadcastMessage(new Message(MessageType.ServerNotification, $"{tempUsername} disconnected."), this);
            Console.WriteLine("Client disconnected.");
        }
    }

    public void SendMessage(Message message)
    {
        string serializedMessage = JsonConvert.SerializeObject(message);
        writer.WriteLine(serializedMessage);
        writer.Flush();

        //writer.WriteLine($"{message.Type}#{message.Content}");
        //writer.Flush();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        int port = 12345;
        ChatServer server = new ChatServer(ipAddress, port);
        Thread t = new Thread(server.Start);
        t.Start();
    }
}
