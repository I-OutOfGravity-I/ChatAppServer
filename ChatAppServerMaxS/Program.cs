using System.Net;

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
