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