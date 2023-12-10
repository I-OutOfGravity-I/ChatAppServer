using System.Net.Sockets;
using System.Text;

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