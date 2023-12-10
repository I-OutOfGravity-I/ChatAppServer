using System.Text.Json;
using System.Text.Json.Serialization;

[Serializable]
public class Message
{
    public MessageType Type { get; set; }
    public string Username { get; set; }
    public string Content { get; set; }

    [JsonConstructor]
    public Message(MessageType type, string username, string content)
    {
        Type = type;
        Username = username;
        Content = content;
    }

    public static string SerializeMessage(Message message)
    {
        return JsonSerializer.Serialize(message);
    }

    public static Message DeserializeMessage(string jsonString)
    {
        return JsonSerializer.Deserialize<Message>(jsonString);
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