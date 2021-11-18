using MessagePack;

[MessagePackObject]
public struct AssignPlayerIdPacket : INetworkPacket
{
    [Key(0)]
    public int Id;
}