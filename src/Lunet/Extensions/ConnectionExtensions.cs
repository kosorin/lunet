namespace Lunet.Extensions;

public static class ConnectionExtensions
{
    public static void SendMessage(this Connection connection, byte[] data)
    {
        connection.SendMessage(0, data);
    }

    public static void SendMessage<TEnum>(this Connection connection, TEnum channel, byte[] data) where TEnum : Enum
    {
        connection.SendMessage(Convert.ToByte(channel), data);
    }
}
