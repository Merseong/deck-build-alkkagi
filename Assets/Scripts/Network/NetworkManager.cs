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
    SocketClient m_client;
    private SocketClient Client => m_client;

    [SerializeField]
    private int networkId = -1;
    public int NetworkId => networkId;
    private int roomNumber = -1;
    
    enum ConnectionStatusEnum
    {
        DISCONNECTED,
        CONNECTING,
        IDLE,
        ROOM,
    }
    private ConnectionStatusEnum _connectionStatus;
    private string connectionStatusString;
    private ConnectionStatusEnum ConnectionStatus
    {
        get => _connectionStatus;
        set
        {
            _connectionStatus = value;

            switch(value)
            {
                case ConnectionStatusEnum.DISCONNECTED:
                    testStatusText.text = "Disconnected";
                    break;
                case ConnectionStatusEnum.CONNECTING:
                    testStatusText.text = "Connecting...";
                    break;
                case ConnectionStatusEnum.IDLE:
                    testStatusText.text = $"Connected, ID: {NetworkId}";
                    break;
                case ConnectionStatusEnum.ROOM:
                    testStatusText.text = $"Room {roomNumber}, {connectionStatusString}";
                    break;
            }
        }
    }

    public delegate void ParsePacketDelegate(Packet packet);
    private ParsePacketDelegate ParsePacket;
    // temp: 만일 delegate의 삭제가 이름 순서로 이루어지면, 별도의 List<delegate>로 만들어 관리해야할듯

    // temp: 
    [SerializeField]
    private bool m_isNetworkMode;
    [SerializeField]
    private string m_messageToSend;

    // temp: canvas
    [SerializeField]
    private GameObject m_networkTestCanvas;
    [SerializeField]
    private TMPro.TextMeshProUGUI testStatusText;

    // [SerializeField]
    // private Dictionary<uint, byte[]> syncVarDataDict;
    // public Dictionary<uint, byte[]> SyncVarDataDict => syncVarDataDict;

    // [SerializeField]
    // private Dictionary<uint, Action> syncVarActionDict;
    // public Dictionary<uint, Action> SyncVarActionDict => syncVarActionDict;

    [SerializeField]
    private Dictionary<uint, (byte[] data, Action callback)> syncVarDict;
    public Dictionary<uint, (byte[] data, Action callback)> SyncVarDict => syncVarDict;

    #region Unity functions
    private void Awake()
    {
        ParsePacket = new ParsePacketDelegate((_) => { });

        if (!m_isNetworkMode) return;

        m_networkTestCanvas.SetActive(m_isNetworkMode);
        ConnectionStatus = ConnectionStatusEnum.DISCONNECTED;

        // syncVarDataDict = new Dictionary<uint, byte[]>();
        // syncVarActionDict = new Dictionary<uint, Action>();
        syncVarDict = new Dictionary<uint, (byte[], Action)>();
    }

    private void Start()
    {
        ParsePacket += BasicProcessPacket;
        ParsePacket += ParsePacketAction;
    }

    private void LateUpdate()
    {
        if (m_isNetworkMode && m_client != null && ConnectionStatus != ConnectionStatusEnum.DISCONNECTED)
        {
            m_client.ProcessPackets(ParsePacket);
        }
    }

    private void OnDestroy()
    {
        DisconnectServer();
    }
    #endregion

    public void ConnectServer()
    {
        if (!m_isNetworkMode || ConnectionStatus != ConnectionStatusEnum.DISCONNECTED)
        {
            return;
        }

        m_client = new SocketClient();
        m_client.Connect("127.0.0.1", 3333);
        ConnectionStatus = ConnectionStatusEnum.CONNECTING;
    }

    public void DisconnectServer()
    {
        if (!m_isNetworkMode || m_client == null)
        {
            return;
        }
        m_client.Disconnect();

        ConnectionStatus = ConnectionStatusEnum.DISCONNECTED;
        m_client = null;
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

    public void SendData<T>(Data<T> data, PacketType type) where T : class
    {
        if (!m_isNetworkMode) return;
        if (Client == null)
        {
            Debug.LogError("[NETWORK] not connected");
        }
        
        var sendPacket = new Packet().Pack(type, data);
        Client.Send(sendPacket);
    }

    public void InitSyncVar(uint netID, byte[] data, Action callback)
    {
        if (syncVarDict.ContainsKey(netID)) return;

        // syncVarDataDict[netID] = data;
        // syncVarActionDict[netID] = callback;
        syncVarDict[netID] = (data, callback);
    }

    #region Delegate Control functions
    public void AddReceiveDelegate(ParsePacketDelegate func)
    {
        ParsePacket += func;
    }

    /// <summary>
    /// 주의사항, 여러개의 func가 있을 경우, 가장 마지막이 삭제됨
    /// </summary>
    public void RemoveReceiveDelegate(ParsePacketDelegate func)
    {
        ParsePacket -= func;
    }
    #endregion

    #region Send Actions

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

    /// <summary>
    /// 임시, 겜매니저로 옮기든 할듯
    /// </summary>
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

    #region Receive Actions
    /// <summary>
    /// 
    /// </summary>
    public void BasicProcessPacket(Packet packet)
    {
        switch((PacketType)packet.Type)
        {
            case PacketType.PACKET_USER_CLOSED:
                DisconnectServer();
                break;
            case PacketType.PACKET_TEST:
                var message = TestPacket.Deserialize(packet.Data);
                Debug.Log($"[TESTPACKET] {message.message}");
                break;
            case PacketType.PACKET_INFO:
                var mp = MessagePacket.Deserialize(packet.Data);
                SetNetworkId(mp.senderID);
                ConnectionStatus = ConnectionStatusEnum.IDLE;
                break;
        }
    }

    public void ParsePacketAction(Packet packet)
    {
        switch ((PacketType)packet.Type)
        {
            case PacketType.ROOM_CONTROL:
                CheckMessageEntered(packet);
                InitServerSyncVar();
                break;
            /*case PacketType.ROOM_OPPONENT:
                var mp = MessagePacket.Deserialize(packet.Data);
                Debug.LogWarning($"[{mp.senderID}] {mp.message}");
                break;*/
            case PacketType.SYNCVAR_CHANGE:
                ParseSyncVarPacket(packet);
                break;
        }
    }

    private void CheckMessageEntered(Packet packet)
    {
        MessagePacket mp = MessagePacket.Deserialize(packet.Data);
        var arr = mp.message.Split(" ");
        if (arr[0] == "ENTERED")
        {
            roomNumber = int.Parse(arr[1]);
            connectionStatusString = arr[0];
            Debug.Log($"[room{roomNumber}] room {roomNumber} matched!");
            ConnectionStatus = ConnectionStatusEnum.ROOM;

            // TODO:
            //  씬 이동, 로딩 작업 진행
            //

            // 로드 완료 후 서버로 로드 완료 메세지 보냄
            var toSend = new MessagePacket();
            toSend.senderID = NetworkId;
            toSend.message = "LOADED";
            var sendPacket = new Packet().Pack(PacketType.ROOM_CONTROL, toSend);
            Client.Send(sendPacket);
        }
        else if (arr[0] == "START")
        {
            connectionStatusString = mp.message;
            // arr[1]의 플레이어가 선턴을 잡고 시작함
            Debug.Log($"[room{roomNumber}] game start! with first player {arr[1]}");
            ConnectionStatus = ConnectionStatusEnum.ROOM;
            GameManager.Inst.isLocalGoFirst = (NetworkId == int.Parse(arr[1]));
            GameManager.Inst.InitializeTurn();
        }
        else if (arr[0] == "EXIT")
        {
            Debug.Log($"[room{roomNumber}] room breaked! by {arr[1]}");
            ConnectionStatus = ConnectionStatusEnum.IDLE;
        }
    }

    private void ParseSyncVarPacket(Packet packet)
    {
        var sp = SyncVarPacket.Deserialize(packet.Data);

        if (!SyncVarDict.ContainsKey(sp.NetID)) return;

        var (_data, _callback) = SyncVarDict[sp.NetID];
        _data = sp.Data;
        SyncVarDict[sp.NetID] = (_data, _callback);
        _callback();
    }
    #endregion
}
