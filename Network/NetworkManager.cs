using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using LiteNetLib.Utils;
using MessagePack;
using MessagePack.Resolvers;
using UnityEngine;
using UnityEngine.Events;

public sealed class NetworkManager : MonoBehaviour
{
    public static NetworkManager Manager;
    public static MessagePackSerializerOptions SerializerOptions => MessagePackSerializer.DefaultOptions.WithSecurity(MessagePackSecurity.UntrustedData);
    public Transport activeTransport;
    public int playerIds = 0; // -1 is all or no players
    private Dictionary<int, PacketCallbackInfo<INetworkPacket>> callbacks = new Dictionary<int, PacketCallbackInfo<INetworkPacket>>();
    private Dictionary<string, int> packetIds = new Dictionary<string, int>();
    public bool IsServer => activeTransport.IsServer;
    public bool IsClient => activeTransport.IsClient;
    public bool IsActive => IsServer || IsClient;
    public NetworkPeer ServerPeer => activeTransport.ServerPeer;
    public int LocalPlayerId = -1;
    public Dictionary<int, NetworkPeer> peers = new Dictionary<int, NetworkPeer>();
    public UnityEvent ServerOnEventsInit = new UnityEvent();
    public UnityEvent ClientOnEventsInit = new UnityEvent();

    public void Init()
    {
        if (Manager != null)
            return;
        Manager = this;
        RegisterPacket<StringPacket>((pr, st, ch) => {});
        RegisterPacket<ByteArrayPacket>((pr, st, ch) => {});
        RegisterPacket<AssignPlayerIdPacket>(AssignPlayerId);
    }

    private void AssignPlayerId(NetworkPeer peer, AssignPlayerIdPacket packet, int channel)
    {
        if (IsClient)
        {
            LocalPlayerId = packet.Id;
        }
    }

    public static T DeserializePacket<T>(byte[] data)
    {
        return MessagePackSerializer.Deserialize<T>(data, SerializerOptions);
    }

    public static byte[] SerializePacket<T>(T data)
    {
        return MessagePackSerializer.Serialize<T>(data, SerializerOptions);
    }

    public static NetworkPacket SerializeSubPacket<T>(T data) where T : INetworkPacket, new()
    {
        return new NetworkPacket()
        {
            crc32PacketId = Manager.packetIds[typeof(T).Name],
            data = SerializePacket<T>(data)
        };
    }

    public static T DeserializeSubPacket<T>(NetworkPacket data)
    {
        return DeserializePacket<T>(data.data);
    }

    public void Send<T>(T data, int channel = 0, params NetworkPeer[] args) where T : INetworkPacket, new()
    {
        if (!packetIds.ContainsKey(typeof(T).Name))
        {
            Debug.LogError($"The packet {typeof(T).Name} was not registered.");
            return;
        }
        activeTransport.Send(SerializePacket<NetworkPacket>(new NetworkPacket()
        {
            crc32PacketId = packetIds[typeof(T).Name],
            data = SerializePacket<T>(data)
        }), channel, args);
    }

    public void Kick<T>(T data, NetworkPeer arg) where T : INetworkPacket, new()
    {
        if (!packetIds.ContainsKey(typeof(T).Name))
        {
            Debug.LogError($"The packet {typeof(T).Name} was not registered.");
            return;
        }
        activeTransport.Kick(SerializePacket<NetworkPacket>(new NetworkPacket()
        {
            crc32PacketId = packetIds[typeof(T).Name],
            data = SerializePacket<T>(data)
        }), arg);
    }

    public void RegisterPacket<T>(Action<NetworkPeer, T, int> callback) where T : INetworkPacket, new()
    {
        byte[] arr = Encoding.UTF8.GetBytes(typeof(T).Name);
        int crc32 = (int)CRC32C.Compute(arr, 0, arr.Length);
        if (callbacks.ContainsKey(crc32))
        {
            Debug.LogWarning($"{typeof(T).Name} is already registered.");
            return;
        }
        if (callback == null)
        {
            Debug.LogError($"The callback provided for {typeof(T).Name} was null.");
            return;
        }
        packetIds.Add(typeof(T).Name, crc32);
        callbacks.Add(crc32, new PacketCallbackInfo<INetworkPacket>((p, b, c) => callback.Invoke(p, DeserializePacket<T>(b), c), typeof(T).Name));
    }

    public void UnregisterPacket<T>() where T : INetworkPacket, new()
    {
        byte[] arr = Encoding.UTF8.GetBytes(typeof(T).Name);
        int crc32 = (int)CRC32C.Compute(arr, 0, arr.Length);
        if (callbacks.ContainsKey(crc32))
        {
            callbacks.Remove(crc32);
        }
        if (packetIds.ContainsKey(typeof(T).Name))
        {
            packetIds.Remove(typeof(T).Name);
        }
    }

    public bool IsPacketRegistered<T>() where T : INetworkPacket, new()
    {
        byte[] arr = Encoding.UTF8.GetBytes(typeof(T).Name);
        int crc32 = (int)CRC32C.Compute(arr, 0, arr.Length);
        if (callbacks.ContainsKey(crc32))
        {
            return true;
        }
        if (packetIds.ContainsKey(typeof(T).Name))
        {
            return true;
        }
        return false;
    }

    public void ResetCounters()
    {
        LocalPlayerId = -1;
        playerIds = 0;
        peers.Clear();
    }

    public void StopAll()
    {
        ResetCounters();
        activeTransport.ClearListeners();
        activeTransport.StopAll();
    }

    public void StartServer()
    {
        ResetCounters();
        activeTransport.ClearListeners();
        ServerOnEventsInit.Invoke();
        activeTransport.ServerOnPreConnected.AddListener(ServerConnected);
        activeTransport.ServerOnDisconnected.AddListener(ServerDisconnected);
        activeTransport.ServerOnPacket.AddListener(ServerPacket);
        activeTransport.StartServer();
    }

    public void StartClient()
    {
        ResetCounters();
        activeTransport.ClearListeners();
        ClientOnEventsInit.Invoke();
        activeTransport.ClientOnConnected.AddListener(ClientConnected);
        activeTransport.ClientOnDisconnected.AddListener(ClientDisconnected);
        activeTransport.ClientOnPacket.AddListener(ClientPacket);
        activeTransport.StartClient();
    }

    #region Client Callbacks

    private void ClientConnected()
    {
        Debug.Log("ClientConnected");
    }

    private void ClientDisconnected()
    {
        Debug.Log("ClientDisconnected");
    }

    private void ClientPacket(byte[] data, int channel)
    {
        NetworkPacket packet = DeserializePacket<NetworkPacket>(data);
        if (callbacks.ContainsKey(packet.crc32PacketId))
        {
            callbacks[packet.crc32PacketId].callback?.Invoke(null, packet.data, channel);
        }
        else
        {
            Debug.LogError($"{packet.crc32PacketId} wasn't found in the callbacks, did you forget to register a packet?");
        }
    }

    #endregion

    #region Server Callbacks

    private void ServerConnected(NetworkPeer peer)
    {
        peer.networkId = playerIds++;
        peers.Add(peer.networkId, peer);
        Send(new AssignPlayerIdPacket()
        {
            Id = peer.networkId
        }, 0, peer);
        Debug.Log($"ServerConnected({peer.endPoint})");
    }

    private void ServerDisconnected(NetworkPeer peer)
    {
        if (peers.ContainsKey(peer.networkId))
        {
            peers.Remove(peer.networkId);
        }
        Debug.Log($"ServerDisconnected({peer.endPoint})");
    }

    private void ServerPacket(NetworkPeer peer, byte[] data, int channel)
    {
        NetworkPacket packet = DeserializePacket<NetworkPacket>(data);
        if (callbacks.ContainsKey(packet.crc32PacketId))
        {
            callbacks[packet.crc32PacketId].callback?.Invoke(peer, packet.data, channel);
        }
        else // TODO: kick from server?
        {
            Debug.LogError($"{packet.crc32PacketId} from {peer.endPoint} wasn't found in the callbacks, did you forget to register a packet?");
        }
    }

    #endregion
}
