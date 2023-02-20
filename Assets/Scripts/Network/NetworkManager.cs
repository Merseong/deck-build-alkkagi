using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Networking.Transport;
using Unity.Collections;

public class NetworkManager : SingletonBehavior<NetworkManager>
{
    [Header("old networking")]
    SocketClient m_client;
    private SocketClient Client => m_client;

    [SerializeField] private bool m_isNetworkMode;
    public bool IsNetworkMode => m_isNetworkMode;

    [Header("Server info")]
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 3333;


    [Header("Connection info")]
    [SerializeField] private uint networkId = 0;
    public uint NetworkId => networkId;
    [SerializeField] private int roomNumber = -1;
    
    public enum ConnectionStatusEnum
    {
        DISCONNECTED,
        CONNECTING,
        IDLE,
        MATCHMAKING,
        ROOM,
        INGAME,
    }
    public ConnectionStatusEnum ConnectionStatus;

    public delegate void ParsePacketDelegate(Packet packet);
    private ParsePacketDelegate ParsePacket;
    // temp: 만일 delegate의 삭제가 이름 순서로 이루어지면, 별도의 List<delegate>로 만들어 관리해야할듯

    [Header("Other data")]
    [SerializeField] private string m_messageToSend;
    public Action OnConnected;
    public UserDataPacket UserData = null;

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
    protected override void Awake()
    {
        base.Awake();

        DontDestroyOnLoad(gameObject);

        RefreshReceiveDelegate();

        if (!m_isNetworkMode) return;

        m_client = new SocketClient();
        ConnectionStatus = ConnectionStatusEnum.DISCONNECTED;

        // syncVarDataDict = new Dictionary<uint, byte[]>();
        // syncVarActionDict = new Dictionary<uint, Action>();
        syncVarDict = new Dictionary<uint, (byte[], Action)>();
    }

    private void Update()
    {
        if (!m_isNetworkMode) return;

        Client.Driver.ScheduleUpdate().Complete();

        if (!Client.Driver.IsCreated)
        {
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        while ((cmd = Client.Connection.PopEvent(Client.Driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("hello network!");
                OnConnected?.Invoke();

                // Send test data
                var packet = new Packet().Pack(PacketType.PACKET_TEST, new TestPacket
                {
                    message = "hello world!",
                });
                var byteArr = new NativeArray<byte>(packet.GetSendBytes(), Allocator.Temp);

                Client.Driver.BeginSend(Client.Connection, out var writer);
                writer.WriteBytes(byteArr);
                Client.Driver.EndSend(writer);
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                var value = new NativeArray<byte>(stream.Length, Allocator.Temp);
                stream.ReadBytes(value);
                Client.OnReceived(value);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.LogWarning("Client got disconnected from server.");
                Client.Connection = default;
            }
        }
    }

    private void LateUpdate()
    {
        if (m_isNetworkMode && m_client != null && ConnectionStatus != ConnectionStatusEnum.DISCONNECTED)
        {
            m_client.ProcessPackets(ParsePacket);
        }
    }

    private void OnApplicationQuit() => OnDestroy();

    private void OnDestroy()
    {
        if (!m_isNetworkMode) return;

        DisconnectServer();
        if (Client != null)
            Client.Driver.Dispose();
    }
#endregion

    public void RefreshUI(bool hideCanvas = false)
    {

    }

    public void ConnectServer()
    {
        if (ConnectionStatus == ConnectionStatusEnum.CONNECTING && m_client.Driver.IsCreated)
        { // 로그아웃 한 후 재호출되었을때
            OnConnected?.Invoke();
        }
        if (!m_isNetworkMode || ConnectionStatus != ConnectionStatusEnum.DISCONNECTED)
        {
            return;
        }

        m_client.Connect(host, port);
        ConnectionStatus = ConnectionStatusEnum.CONNECTING;
    }

    public void DisconnectServer()
    {
        if (!m_isNetworkMode || !Client.Driver.IsCreated)
        {
            return;
        }
        m_client.Disconnect();

        OnConnected = null;
        ConnectionStatus = ConnectionStatusEnum.DISCONNECTED;
    }

    public void SetNetworkId(uint id, bool reset = false)
    {
        if (reset) // 로그아웃시에도 사용
        {
            networkId = 0;
            UserData = null;
            ConnectionStatus = ConnectionStatusEnum.CONNECTING;
            return;
        }
        if (networkId != 0) return;
        networkId = id;
        ConnectionStatus = ConnectionStatusEnum.IDLE;
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

    #region User actions
    /// <summary>
    /// update self (local user)
    /// </summary>
    /// <returns></returns>
    public void UpdateUserData(Action<UserDataPacket> updatedCallback = null)
    {
        AddReceiveDelegate(UserInfoReceiveNetworkAction);
        SendData(new MessagePacket
        {
            senderID = NetworkId
        }, PacketType.USER_INFO);

        void UserInfoReceiveNetworkAction(Packet packet)
        {
            if (packet.Type != (short)PacketType.USER_INFO) return;
            var msg = UserDataPacket.Deserialize(packet.Data);

            if (msg.isSuccess)
            {
                UserData = msg;
                updatedCallback?.Invoke(msg);
            }
            else
            {
                Debug.LogError("[LOCAL] get local user info from server is failed");
            }

            RemoveReceiveDelegate(UserInfoReceiveNetworkAction);
        }
    }

    /// <summary>
    /// Get userdata about user with uid
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="updatedCallback"></param>
    public void UpdateUserData(uint uid, Action<UserDataPacket> otherUserCallback)
    {
        AddReceiveDelegate(OtherUserInfoReceiveNetworkAction);
        SendData(new MessagePacket
        {
            senderID = uid
        }, PacketType.USER_INFO);

        void OtherUserInfoReceiveNetworkAction(Packet packet)
        {
            if (packet.Type != (short)PacketType.USER_INFO) return;
            var msg = UserDataPacket.Deserialize(packet.Data);

            if (msg.isSuccess && msg.uid == uid)
            {
                otherUserCallback(msg);
            }
            else
            {
                Debug.LogError("[LOCAL] get other user info from server is failed");
            }

            RemoveReceiveDelegate(OtherUserInfoReceiveNetworkAction);
        }
    }
    #endregion

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

    public void RefreshReceiveDelegate()
    {
        ParsePacket = new ParsePacketDelegate((_) => { });
        ParsePacket += BasicProcessPacket;
    }
#endregion

    #region Send Actions

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
    public void BasicProcessPacket(Packet packet)
    {
        if ((PacketType)packet.Type != PacketType.PACKET_TEST) return;
        var message = TestPacket.Deserialize(packet.Data);
        Debug.Log($"[TESTPACKET] {message.message}");
    }

    public void ParsePacketAction(Packet packet)
    {
        switch ((PacketType)packet.Type)
        {
            case PacketType.ROOM_CONTROL:
                MessagePacket mp = MessagePacket.Deserialize(packet.Data);
                CheckMessageEntered(mp);
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

    private void CheckMessageEntered(MessagePacket mp)
    {
        var arr = mp.message.Split(" ");
        if (arr[0] == "ENTERED/")
        {
            roomNumber = int.Parse(arr[1]);
            Debug.Log($"[room{roomNumber}] room {roomNumber} matched!");
            ConnectionStatus = ConnectionStatusEnum.ROOM;

            StartCoroutine(ELoadScene());

            IEnumerator ELoadScene()
            {
                // 씬이름 바꿔야됨
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("DM");

                yield return new WaitUntil(() => asyncLoad.isDone);

                // 로드 완료 후 서버로 로드 완료 메세지 보냄
                var toSend = new MessagePacket();
                toSend.senderID = NetworkId;
                toSend.message = "LOADED";
                var sendPacket = new Packet().Pack(PacketType.ROOM_CONTROL, toSend);
                Client.Send(sendPacket);
            }
        }
        else if (arr[0] == "START/")
        {
            // 선턴ID 상대ID 내덱 상대덱장수
            // arr[1]의 플레이어가 선턴을 잡고 시작함
            Debug.Log($"[room{roomNumber}] game start! with first player {arr[1]}");
            Debug.Log(mp.message);
            ConnectionStatus = ConnectionStatusEnum.INGAME;
            GameManager.Inst.InitializeGame(NetworkId == int.Parse(arr[1]), arr[3], uint.Parse(arr[2]), arr[4]);
        }
        else if (arr[0] == "EXIT/")
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
