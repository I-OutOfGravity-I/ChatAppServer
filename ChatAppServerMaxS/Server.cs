using System.Net.Sockets;
using System.Net;


public class ChatServer
{
    private readonly List<Client> clients = new List<Client>();
    private readonly TcpListener listener;
    private readonly object clientsLock = new object();

    public ChatServer(IPAddress ipAddress, int port)
    {
        listener = new TcpListener(ipAddress, port);
    }

    private void SendConnectedUser()
    {
        while (true)
        {
            Thread.Sleep(1000);
            if (clients.Count > 0)
            {
                Thread.Sleep(200);
                List<string> usernameAndAddress = new List<string>();

                foreach (var client in clients)
                {
                    usernameAndAddress.Add(client.Username);
                }
                BroadcastMessage(Message.CreateServerMessage(MessageType.ConnectedUsers, "", usernameAndAddress));
            }
        }
    }

    public void Start()
    {
        listener.Start();
        Log("Server started. Waiting for clients...");

        Thread sendUserThread = new Thread(() => SendConnectedUser());
        sendUserThread.Start();

        while (true)
        {
            TcpClient tcpClient = listener.AcceptTcpClient();
            Log("Client connected.");

            ClientHandler clientHandler = new ClientHandler(tcpClient, this);

            lock (clientsLock)
            {
                clients.Add(new Client(clientHandler, null));
            }

            Thread clientThread = new Thread(() => clientHandler.HandleClient(clients[clients.Count - 1]));
            clientThread.Start();
        }
    }

    internal void BroadcastMessage(Message message)
    {
        lock (clientsLock)
        {
            foreach (var client in clients)
            {
                client.GetClientHandler().SendMessage(message);
            }
        }
    }

    internal void RemoveClient(Client client)
    {
        lock (clientsLock)
        {
            clients.Remove(client);
        }
    }

    public void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
    }
}

