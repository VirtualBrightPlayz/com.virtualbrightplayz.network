using MessagePack;

[MessagePackObject]
public struct StringPacket : INetworkPacket
{
    [Key(0)]
    public string data;
}