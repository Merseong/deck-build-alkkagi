using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;
using MyNetworkData;

public class NetworkManager : SingletonBehavior<NetworkManager>
{

    [SerializeField]
    private bool m_isNetworkMode;
    SocketClient m_client;
    public SocketClient Client => m_client;

    [SerializeField]
    private int networkId = -1;
    public int NetworkId => networkId;

    public delegate void ParsePacketDelegate(Packet packet);
    public ParsePacketDelegate ParsePacket;

    [SerializeField]
    private string m_messageToSend;

    [SerializeField]
    private GameObject m_networkTestCanvas;

    // [SerializeField]
    // private Dictionary<uint, byte[]> syncVarDataDict;
    // public Dictionary<uint, byte[]> SyncVarDataDict => syncVarDataDict;

    // [SerializeField]
    // private Dictionary<uint, Action> syncVarActionDict;
    // public Dictionary<uint, Action> SyncVarActionDict => syncVarActionDict;

    [SerializeField]
    private Dictionary<uint, (byte[] data, Action callback)> syncVarDict;
    public Dictionary<uint, (byte[] data, Action callback)> SyncVarDict => syncVarDict;

    private void Awake()
    {
        if (!m_isNetworkMode) return;

        m_networkTestCanvas.SetActive(m_isNetworkMode);
        ParsePacket = new ParsePacketDelegate((_) => { });

        // syncVarDataDict = new Dictionary<uint, byte[]>();
        // syncVarActionDict = new Dictionary<uint, Action>();
        syncVarDict = new Dictionary<uint, (byte[], Action)>();
    }

    private void Start()
    {
        ParsePacket += ParsePacketAction;
    }

    private void LateUpdate()
    {
        if (m_isNetworkMode && m_client != null)
        {
            m_client.ProcessPackets(ParsePacket);
        }
    }

    public void SetNetworkId(int id, bool reset = false)
    {
        if (reset)
        {
            networkId = -1;
            return;
        }
        if (networkId > 0) return;
        networkId = id;
    }

    #region Send Actions
    public void InitSyncVar(uint netID, byte[] data, Action callback)
    {
        if (syncVarDict.ContainsKey(netID)) return;

        // syncVarDataDict[netID] = data;
        // syncVarActionDict[netID] = callback;
        syncVarDict[netID] = (data, callback);
    }

    public void ConnectToServer()
    {
        if (!m_isNetworkMode)
        {
            return;
        }

        m_client = new SocketClient();
        m_client.Connect("127.0.0.1", 3333);
    }

    public void DisconnectServer()
    {
        if (!m_isNetworkMode)
        {
            return;
        }
    }

    public void EnterGameRoom()
    {
        if (!m_isNetworkMode)
        {
            return;
        }

        var toSend = new MessagePacket();
        toSend.senderID = networkId;
        toSend.message = "ENTER";
        var sendPacket = new Packet().Pack(PacketType.ROOM_CONTROL, toSend);
        Client.Send(sendPacket);
    }

    public void SendMessageToOpponent()
    {
        if (!m_isNetworkMode)
        {
            return;
        }

        var toSend = new MessagePacket();
        toSend.senderID = networkId;
        toSend.message = m_messageToSend;
        var sendPacket = new Packet().Pack(PacketType.ROOM_OPPONENT, toSend);
        Client.Send(sendPacket);

        m_messageToSend += "a";
    }
    #endregion

    #region Receive Actions
    public void ParsePacketAction(Packet packet)
    {
        switch ((PacketType)packet.Type)
        {
            case PacketType.PACKET_TEST:
                ParseTestPacket(packet);
                break;
            case PacketType.PACKET_INFO:
                var mp = MessagePacket.Deserialize(packet.Data);
                NetworkManager.Inst.SetNetworkId(mp.senderID);
                break;
            case PacketType.ROOM_CONTROL:
                CheckMessageEntered(packet);
                InitServerSyncVar();
                break;
            case PacketType.ROOM_OPPONENT:
                ParseMessagePacket(packet);
                break;
            case PacketType.SYNCVAR_CHANGE:
                ParseSyncVarPacket(packet);
                break;
        }
    }

    private void ParseTestPacket(Packet packet)
    {
        var tp = TestPacket.Deserialize(packet.Data);

        Debug.Log("from server - " + tp.message);
    }

    private void ParseMessagePacket(Packet packet)
    {
        MessagePacket mp = MessagePacket.Deserialize(packet.Data);

        Debug.LogWarning("from " + mp.senderID + " - " + mp.message);
    }

    private void CheckMessageEntered(Packet packet)
    {
        MessagePacket mp = MessagePacket.Deserialize(packet.Data);
        var arr = mp.message.Split(" ");
        if (arr[1] == "ENTERED")
        {
            Debug.Log($"from server - room {arr[0]} matched!");
            var toSend = new MessagePacket();
            toSend.senderID = NetworkManager.Inst.NetworkId;
            toSend.message = "LOADED";
            var sendPacket = new Packet().Pack(PacketType.ROOM_CONTROL, toSend);
            Client.Send(sendPacket);
        }
        else if (arr[1] == "START")
        {
            Debug.Log($"from server - game start! with first player {arr[0]}");
        }
    }

    private void ParseSyncVarPacket(Packet packet)
    {
        var sp = SyncVarPacket.Deserialize(packet.Data);

        if (!NetworkManager.Inst.SyncVarDict.ContainsKey(sp.NetID)) return;

        var (_data, _callback) = NetworkManager.Inst.SyncVarDict[sp.NetID];
        _data = sp.Data;
        NetworkManager.Inst.SyncVarDict[sp.NetID] = (_data, _callback);
        _callback();
    }

    private void InitServerSyncVar()
    {
        foreach (var kv in NetworkManager.Inst.SyncVarDict)
        {
            var syncPacket = new SyncVarPacket();
            syncPacket.NetID = kv.Key;
            syncPacket.Data = kv.Value.data;
            var data = syncPacket.Serialize();
            var p = new Packet();
            p.Type = (Int16)PacketType.SYNCVAR_INIT;
            p.SetData(data, data.Length);
            Client.Send(p);
        }
    }
    #endregion
}
