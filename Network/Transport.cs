using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class Transport : MonoBehaviour
{
    [HideInInspector]
    public UnityEvent<NetworkPeer> ServerOnConnected = new UnityEvent<NetworkPeer>();
    [HideInInspector]
    public UnityEvent<NetworkPeer> ServerOnPreConnected = new UnityEvent<NetworkPeer>();
    [HideInInspector]
    public UnityEvent<NetworkPeer> ServerOnDisconnected = new UnityEvent<NetworkPeer>();
    [HideInInspector]
    public UnityEvent<NetworkPeer, byte[], int> ServerOnPacket = new UnityEvent<NetworkPeer, byte[], int>();
    [HideInInspector]
    public UnityEvent ClientOnConnected = new UnityEvent();
    [HideInInspector]
    public UnityEvent ClientOnDisconnected = new UnityEvent();
    [HideInInspector]
    public UnityEvent<byte[], int> ClientOnPacket = new UnityEvent<byte[], int>();
    [HideInInspector]
    public UnityEvent Stopped = new UnityEvent();
    [HideInInspector]
    public bool IsServer = false;
    [HideInInspector]
    public bool IsClient = false;
    [HideInInspector]
    public NetworkPeer ServerPeer;

    public virtual void Start()
    {}
    public virtual void Update()
    {}

    public virtual void ClearListeners()
    {
        ServerOnConnected.RemoveAllListeners();
        ServerOnDisconnected.RemoveAllListeners();
        ServerOnPacket.RemoveAllListeners();
        ClientOnConnected.RemoveAllListeners();
        ClientOnDisconnected.RemoveAllListeners();
        ClientOnPacket.RemoveAllListeners();
        Stopped.RemoveAllListeners();
    }

    public abstract void StartServer();
    public abstract void StartClient();
    public abstract void StopAll();
    public abstract void Send(byte[] data, int channel, NetworkPeer[] args);
    public abstract void Kick(byte[] data, NetworkPeer arg);
}
