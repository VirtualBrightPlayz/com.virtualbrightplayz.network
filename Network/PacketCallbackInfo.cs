using System;
using System.Text;
using LiteNetLib.Utils;

public class PacketCallbackInfo<T> where T : INetworkPacket
{
    public string name;
    public int crc32;
    public Func<T> create;
    public Action<NetworkPeer, byte[], int> callback;

    public PacketCallbackInfo()
    {}

    public PacketCallbackInfo(Action<NetworkPeer, byte[], int> callback)
    {
        this.create = () => Activator.CreateInstance<T>();
        this.callback = callback;
        this.name = typeof(T).Name;
        byte[] arr = Encoding.UTF8.GetBytes(this.name);
        this.crc32 = (int)CRC32C.Compute(arr, 0, arr.Length);
    }

    public PacketCallbackInfo(Action<NetworkPeer, byte[], int> callback, string name)
    {
        this.create = () => Activator.CreateInstance<T>();
        this.callback = callback;
        this.name = name;
        byte[] arr = Encoding.UTF8.GetBytes(this.name);
        this.crc32 = (int)CRC32C.Compute(arr, 0, arr.Length);
    }
}