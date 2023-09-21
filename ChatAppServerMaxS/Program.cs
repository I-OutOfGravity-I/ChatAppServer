using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class client
{
    private readonly ClientHandler clientHandler;
    private string username;

    public client(ClientHandler clientHandler, string username)
    {
        this.clientHandler = clientHandler;
        Username = username;
    }

    public string Username { get => username; set => username = value; }

    public ClientHandler ClientHandler => clientHandler;
}
public class ChatServer
{
    private readonly List<client> clients = new List<client>();
    private readonly TcpListener listener;
    

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
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("Client connected.");

            ClientHandler clientHandler = new ClientHandler(client, this);
            clients.Add(new client(clientHandler, null));

            Thread clientThread = new Thread(() => clientHandler.HandleClient(clients[clients.Count - 1]));
            clientThread.Start();
        }
    }

    public void BroadcastMessage(string message, ClientHandler sender)
    {
        foreach (var client in clients)
        {
            client.ClientHandler.SendMessage(message);
        }
    }

    public void RemoveClient(client client)
    {
        clients.Remove(client);
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

    public void HandleClient(client client)
    {
        try
        {
            client.Username = reader.ReadLine();
            Console.WriteLine($"{client.Username} joined the chat.");
            server.BroadcastMessage($"{client.Username} joined the chat.", this);

            while (true)
            {
                string message = reader.ReadLine();
                if (message == null)
                    break;

                Console.WriteLine($"{client.Username}: {message}");
                server.BroadcastMessage($"{client.Username}: {message}", this);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            server.BroadcastMessage($"{client.Username} disconnected.", this);
            server.RemoveClient(client);
            this.client.Close();
            Console.WriteLine("Client disconnected.");
        }
    }

    public void SendMessage(string message)
    {
        writer.WriteLine(message);
        writer.Flush();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        int port = 12345;
        ChatServer server = new ChatServer(ipAddress, port);
        server.Start();
    }
}
