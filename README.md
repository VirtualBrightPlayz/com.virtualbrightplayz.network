# com.virtualbrightplayz.network

A networking framework using https://github.com/neuecc/MessagePack-CSharp and https://github.com/RevenantX/LiteNetLib

A lot of this is subject to change with time. Keep in mind this is a networking framework, not library. In other words, it's very low-level compared to things like PUN or Mirror or MLAPI.

If you want to, feel free to port this to other engines, it shouldn't be too hard ^^

## Notes

- Although there is a singleton option, most of the library will probably work without it, so long as your code has a reference to the `NetworkManager` component.
- Packets are required to implement the `INetworkPacket` interface AND add attributes for MessagePack, like `MessagePackObject` and `Key`.
- Packets should be structs, as I don't know if classes work yet.

## Install

First install the required dependencies
- https://github.com/RevenantX/LiteNetLib
- - Make sure to install the source of the Library, as there is Unity specific #IF defines
- https://github.com/neuecc/MessagePack-CSharp
- - Make sure to install the unitypackage file from the releases

Next,
- Clone this repository into the `Packages` folder in the root of your Unity project's folder
- Using the Unity Package manager, you can install this as a git package.
- - SSH: `git@github.com:VirtualBrightPlayz/com.virtualbrightplayz.network.git`

## Usage

### Register a packet

Packets can be called from both the server or the client.

Example using `MyPacket`

```cs
using MessagePack;

[MessagePackObject]
public struct MyPacket : INetworkPacket
{
    [Key(0)]
    public int My;
    [Key(1)]
    public string Data;
    [Key(2)]
    public bool Goes;
    [Key(3)]
    public byte Here;
}

// Register "MyPacket" and a callback
NetworkManager.Manager.RegisterPacket<MyPacket>(OnMyPacket);

private void OnMyPacket(NetworkPeer peer, MyPacket packet, int channel)
{
    // Your code for this packet.
    // At the time of writing, peer is always null on the client.
    // LNLTransport channels:
    // Channel 0 = ReliableOrdered
    // Channel 1 = Unreliable
}
```

### Send a Packet

#### Server send to all

```cs
NetworkManager.Manager.Send(new MyPacket()
{
    My = -1,
    Data = "MyPacket-1",
    Goes = true,
    Here = 255
}, 0, NetworkManager.Manager.peers.Select(x => x.Value).ToArray());
```

#### Client send to server

```cs
NetworkManager.Manager.Send(new MyPacket()
{
    My = -1,
    Data = "MyPacket-1",
    Goes = true,
    Here = 255
}, 0, NetworkManager.Manager.ServerPeer);
```