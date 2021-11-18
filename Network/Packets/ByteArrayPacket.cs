using MessagePack;

[MessagePackObject]
public struct ByteArrayPacket : INetworkPacket
{
    [Key(0)]
    public byte[] data;
}