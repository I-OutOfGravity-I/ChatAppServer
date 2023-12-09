using System.Net;
using System.Net.Sockets;
using System.Text;

public enum MessageType
{
    Login,
    ServerNotification,
    Message,
    ConnectedUsers,
    Image,
    Video
}

[Serializable]
public class Message
{
    public MessageType Type { get; set; }
    public string Username { get; set; }
    public string Content { get; set; }

    [System.Text.Json.Serialization.JsonConstructor]
    public Message(MessageType type, string username, string content)
    {
        Type = type;
        Username = username;
        Content = content;
    }

    public static string SerializeMessage(Message message)
    {
        string jsonString = System.Text.Json.JsonSerializer.Serialize(message);
        return jsonString;
    }

    public static Message DeserializeMessage(string jsonString)
    {
        Message deserializedMessage = System.Text.Json.JsonSerializer.Deserialize<Message>(jsonString);
        return deserializedMessage;
    }

    public static Message CreateMessage(MessageType type, string username, string content)
    {
        return new Message(type, username, content);
    }

    public static Message CreateServerMessage(MessageType type, string username, List<string> content)
    {
        return new Message(type, "", SerializeStringList(content));
    }
    public static Message CreateServerMessage(MessageType type, string username, string content)
    {
        return new Message(type, "", content);
    }
    public static string SerializeStringList(List<string> stringList)
    {
        // Manually create a JSON-formatted string for the list
        string jsonString = "[";
        for (int i = 0; i < stringList.Count; i++)
        {
            jsonString += $"\"{EscapeString(stringList[i])}\"";

            if (i < stringList.Count - 1)
            {
                jsonString += ",";  
            }
        }
        jsonString += "]";

        return jsonString;
    }

    public static string EscapeString(string input)
    {
        return input.Replace("\"", "\\\"");
    }

    public static string UnescapeString(string input)
    {
        return input.Replace("\\\"", "\"");
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

    internal ClientHandler GetClientHandler() => clientHandler;
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
            string registerData = reader.ReadLine();
            Message registerMessage = Message.DeserializeMessage(registerData);
            client.Username = registerMessage.Username;
            server.Log($"{registerMessage.Username} joined the chat. {client.GetClientHandler().client.Client.RemoteEndPoint}");
            server.BroadcastMessage(Message.CreateServerMessage(MessageType.ServerNotification, "", $"{registerMessage.Username} joined the chat."));

            while (true)
            {

                string stringData = reader.ReadLine();
                if (stringData == null)
                    break;
                Message message = Message.DeserializeMessage(stringData);
                //hier kann man dann mitlauschen höhö
                Message sendBackMessage = Message.CreateMessage(message.Type, message.Username, message.Content);
                server.BroadcastMessage(sendBackMessage);
            }
        }
        catch (IOException ex)
        {
            server.Log($"{client.Username} disconnected: {ex.Message}");
        }
        catch (Exception ex)
        {
            server.Log($"An error occurred in HandleClient: {ex.Message}");
        }
        finally
        {
            try
            {
                server.RemoveClient(client);
                reader.Close();
                writer.Close();
                stream.Close();
            }
            catch (Exception ex)
            {
                server.Log($"Error while closing client resources: {ex.Message}");
            }

            string tempUsername = client.Username;
            server.BroadcastMessage(Message.CreateMessage(MessageType.ServerNotification, "", $"{tempUsername} disconnected."));
            server.Log("Client disconnected.");
        }
    }

    internal void SendMessage(Message message)
    {
        try
        {
            if (client.Connected)
            {
                string sendData = Message.SerializeMessage(message);
                writer.WriteLine(sendData);
                writer.Flush();
            }
        }
        catch (IOException ex)
        {
            server.Log($"Error sending message: {ex.Message}");
        }
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
