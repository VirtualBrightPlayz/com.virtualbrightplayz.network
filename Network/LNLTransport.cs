using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using UnityEngine;

public class LNLTransport : Transport, INetEventListener
{
    public string address = "127.0.0.1";
    public int port = 27015;
    public string key;
    NetManager _manager;

    public override void StopAll()
    {
        if (_manager != null)
        {
            if (_manager.IsRunning)
            {
                _manager.Stop();
            }
            _manager = null;
            Stopped.Invoke();
            Debug.Log("Network stopped.");
        }
        IsServer = false;
        IsClient = false;
        ServerPeer = null;
    }

    public override void StartServer()
    {
        StopAll();
        IsServer = true;
        _manager = new NetManager(this);
        _manager.Start(port);
        Debug.Log($"Network server started on port {port}.");
    }

    public override void StartClient()
    {
        StopAll();
        IsClient = true;
        _manager = new NetManager(this);
        _manager.Start();
        _manager.Connect(address, port, key);
        Debug.Log($"Network client started ({address}:{port}).");
    }

    public override void Update()
    {
        base.Update();
        if (_manager != null && _manager.IsRunning)
            _manager.PollEvents();
    }

    public override void Send(byte[] data, int channel, NetworkPeer[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].tag is NetPeer peer)
            {
                peer.Send(data, channel == 0 ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Unreliable);
            }
        }
    }

    public override void Kick(byte[] data, NetworkPeer arg)
    {
        if (arg.tag is NetPeer peer)
        {
            peer.Disconnect(data);
        }
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        if (string.IsNullOrEmpty(key))
            request.Accept();
        else
            request.AcceptIfKey(key);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Debug.LogError($"{endPoint.ToString()} caused network error ({socketError.ToString()}).");
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        byte[] data = reader.GetRemainingBytes();
        if (IsServer)
        {
            ServerOnPacket.Invoke((NetworkPeer)peer.Tag, data, deliveryMethod == DeliveryMethod.ReliableOrdered ? 0 : 1);
        }
        if (IsClient)
        {
            ClientOnPacket.Invoke(data, deliveryMethod == DeliveryMethod.ReliableOrdered ? 0 : 1);
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        Debug.LogError($"{remoteEndPoint.ToString()} sent message while not connected ({messageType.ToString()}).");
    }

    public void OnPeerConnected(NetPeer peer)
    {
        peer.Tag = new NetworkPeer()
        {
            tag = peer,
            endPoint = peer.EndPoint.ToString()
        };
        if (IsServer)
        {
            ServerOnPreConnected.Invoke((NetworkPeer)peer.Tag);
            ServerOnConnected.Invoke((NetworkPeer)peer.Tag);
        }
        if (IsClient)
        {
            ServerPeer = (NetworkPeer)peer.Tag;
            ClientOnConnected.Invoke();
        }
        Debug.Log($"Peer connected from {peer.EndPoint.ToString()}.");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (IsServer)
        {
            ServerOnDisconnected.Invoke((NetworkPeer)peer.Tag);
        }
        if (IsClient)
        {
            ClientOnDisconnected.Invoke();
            StopAll();
        }
        peer.Tag = null;
        Debug.Log($"Peer disconnected from {peer.EndPoint.ToString()} ({disconnectInfo.Reason.ToString()}) ({disconnectInfo.SocketErrorCode.ToString()}).");
    }
}
