using MessagePack;

[MessagePackObject]
public struct NetworkPacket
{
    [Key(0)]
    public int crc32PacketId;
    [Key(1)]
    public byte[] data;
}