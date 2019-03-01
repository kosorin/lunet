namespace Lunet.Data
{
    public interface INetBuffer
    {
        byte[] Data { get; }

        int Offset { get; }

        int Length { get; }
    }
}
