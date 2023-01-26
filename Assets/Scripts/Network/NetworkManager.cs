using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;

public class NetworkManager : SingletonBehavior<NetworkManager>
{
    [SerializeField]
    private bool m_isNetworkMode;
    MyNetwork.SocketClient m_client;
    public MyNetwork.SocketClient Client => m_client;

    [SerializeField]
    private int networkId = -1;
    public int NetworkId => networkId;

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
        m_networkTestCanvas.SetActive(m_isNetworkMode);

        // syncVarDataDict = new Dictionary<uint, byte[]>();
        // syncVarActionDict = new Dictionary<uint, Action>();
        syncVarDict = new Dictionary<uint, (byte[], Action)>();
    }

    private void FixedUpdate()
    {
        if (m_client != null)
        {
            m_client.ProcessPackets();
        } 
    }

    public void SetNetworkId(int id)
    {
        if (networkId > 0) return;
        networkId = id;
    }

    public void ResetNetworkId()
    {
        networkId = -1;
    }

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

        m_client = new MyNetwork.SocketClient();
        m_client.Connect("127.0.0.1", 3333);
    }

    public void EnterGameRoom()
    {
        if (!m_isNetworkMode)
        {
            return;
        }

        var toSend = new MyNetworkData.MessagePacket();
        toSend.senderID = networkId;
        toSend.message = "ENTER";
        var sendPacket = new MyNetworkData.Packet().Pack(MyNetworkData.PacketType.ROOM_CONTROL, toSend);
        m_client.Send(sendPacket);
    }

    public void SendMessageToOpponent()
    {
        if (!m_isNetworkMode)
        {
            return;
        }

        var toSend = new MyNetworkData.MessagePacket();
        toSend.senderID = networkId;
        toSend.message = m_messageToSend;
        var sendPacket = new MyNetworkData.Packet().Pack(MyNetworkData.PacketType.ROOM_OPPONENT, toSend);
        m_client.Send(sendPacket);

        m_messageToSend += "a";
    }
}
