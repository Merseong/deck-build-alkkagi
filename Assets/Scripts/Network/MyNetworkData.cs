using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System.Text;

public class Packet
{
    public Int16 Type { get; set; }
    public byte[] Data { get; set; }
    public Packet() { }

    public Packet Pack<T>(PacketType type, Data<T> data) where T : class
    {
        Type = (short)type;
        var serializedData = data.Serialize();
        SetData(serializedData, serializedData.Length);

        return this;
    }

    public void SetData(byte[] data, int len)
    {
        Data = new byte[len];
        Array.Copy(data, Data, len);
    }

    public byte[] GetSendBytes()
    {
        byte[] type_bytes = BitConverter.GetBytes(Type);
        int header_size = (int)(Data.Length);
        byte[] header_bytes = BitConverter.GetBytes(header_size);
        byte[] send_bytes = new byte[header_bytes.Length + type_bytes.Length + Data.Length];

        //헤더 복사. 헤더 == 데이터의 크기
        Array.Copy(header_bytes, 0, send_bytes, 0, header_bytes.Length);

        //타입 복사
        Array.Copy(type_bytes, 0, send_bytes, header_bytes.Length, type_bytes.Length);

        //데이터 복사
        Array.Copy(Data, 0, send_bytes, header_bytes.Length + type_bytes.Length, Data.Length);

        return send_bytes;
    }
}

[Serializable]
public class Data<T> where T : class
{
    public Data() { }

    public byte[] Serialize()
    {
        var json = JsonUtility.ToJson(this);
        return Encoding.UTF8.GetBytes(json);
        /**
        var formatter = new BinaryFormatter();
        // 클래스를 직렬화하여 보관할 데이터
        byte[] data;
        using (MemoryStream stream = new())
        {
            formatter.Serialize(stream, this);
            data = new byte[stream.Length];
            //스트림을 byte[] 데이터로 변환한다.
            data = stream.GetBuffer();
        }
        return data;*/
        /**
        var array = new byte[size];
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(this, ptr, true);
        Marshal.Copy(ptr, array, 0, size);
        Marshal.FreeHGlobal(ptr);
        return array;*/
    }

    public static T Deserialize(byte[] array)
    {
        var data = Encoding.UTF8.GetString(array);

        return (T)JsonUtility.FromJson(data, typeof(T));

        /**
        var formatter = new BinaryFormatter();
        using MemoryStream stream = new(array, 0, array.Length);
        // byte를 읽어들인다.
        stream.Write(array, 0, array.Length);
        // Stream seek을 맨 처음으로 돌린다.
        stream.Seek(0, SeekOrigin.Begin);
        stream.Position = 0;
        // 클래스를 역직렬화
        var data = formatter.Deserialize(stream);*/
        /**
        var size = Marshal.SizeOf(typeof(T));
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(array, 0, ptr, size);
        var s = (T)Marshal.PtrToStructure(ptr, typeof(T));
        Marshal.FreeHGlobal(ptr);
        return s;*/
    }
}

[Serializable]
public class PacketRes : Data<PacketRes>
{
    public bool isSucess;
    public int testIntValue;

    public string message = "";

    public PacketRes() { }
}

public enum PacketType
{
    UNDEFINED,
    PACKET_USER_CLOSED,
    PACKET_TEST,
    /// <summary>
    /// MessagePacket(임시)<br/>
    /// 접속한 유저에게 기본 정보를 알려주기 위해 사용
    /// </summary>
    PACKET_INFO,
    ROOM_BROADCAST,
    ROOM_OPPONENT,
    /// <summary>
    /// ShootStonePacket<br/> 돌의 움직임에 관한 패킷
    /// </summary>
    ROOM_OPPO_SHOOTSTONE,
    /// <summary>
    /// MessagePacket<br/>
    /// 룸 컨트롤(참여, 퇴장 등)시 사용
    /// </summary>
    ROOM_CONTROL,
    SYNCVAR_INIT,
    SYNCVAR_CHANGE,
    PACKET_COUNT
}

[Serializable]
public class TestPacket : Data<TestPacket>
{
    public string message;

    public TestPacket() { }
}

[Serializable]
public class MessagePacket : Data<MessagePacket>
{
    public int senderID;
    public string message;

    public MessagePacket() { }
    public MessagePacket(MessagePacket msg)
    {
        senderID = msg.senderID;
        message = msg.message;
    }
}

[Serializable]
public class SyncVarPacket : Data<SyncVarPacket>
{
    public uint NetID;
    public byte[] Data;

    public SyncVarPacket() { }
}


[Serializable]
public class ShootStonePacket : Data<ShootStonePacket>
{
    public int senderID;

    public VelocityRecord[] velocityRecords;
    public PositionRecord[] positionRecords;
    public EventRecord[] eventRecords;

    public short velocityCount;
    public short positionCount;
    public short eventCount;
}

[Serializable]
public struct VelocityRecord
{
    public float time;
    public int stoneId;
    public float xVelocity;
    public float zVelocity;
}

[Serializable]
public struct PositionRecord
{
    public int stoneId;
    public float xPosition;
    public float zPosition;
}

[Serializable]
// collide, drop out, stone power
public struct EventRecord
{
    public float time;
    public int stoneId;
    public EventEnum eventEnum;
    public float xPosition;
    public float zPosition;
}

[Serializable]
public enum EventEnum
{
    COLLIDE,
    DROPOUT,
    POWER,
    GUARDCOLLIDE,
    COUNT,
}

public class MessageResolver
{
    public delegate void CompletedMessageCallback(Packet packet);

    int mMessageSize;
    byte[] mMessageBuffer = new byte[1024 * 2000];
    byte[] mHeaderBuffer = new byte[4];
    byte[] mTypeBuffer = new byte[2];

    PacketType mPreType;

    int mHeadPosition;
    int mTypePosition;
    int mCurrentPosition;

    short mMessageType;
    int mRemainBytes;

    bool mHeadCompleted;
    bool mTypeCompleted;
    bool mCompleted;

    CompletedMessageCallback mCompletedCallback;

    public MessageResolver()
    {
        ClearBuffer();
    }

    public void OnReceive(byte[] buffer, int offset, int transffered, CompletedMessageCallback callback)
    {
        // 현재 들어온 데이터의 위치
        int srcPosition = offset;

        // 콜백함수 설정
        mCompletedCallback = callback;

        // 남은 데이터 양
        mRemainBytes = transffered;

        if (!mHeadCompleted)
        {
            mHeadCompleted = ReadHead(buffer, ref srcPosition);

            if (!mHeadCompleted) return;

            mMessageSize = GetBodySize();

            // 데이터 무결성 검사
            if (mMessageSize < 0)
                return;
        }

        if (!mTypeCompleted)
        {
            mTypeCompleted = ReadType(buffer, ref srcPosition);

            if (!mTypeCompleted)
                return;

            mMessageType = BitConverter.ToInt16(mTypeBuffer, 0);

            if (mMessageType < 0 ||
                mMessageType > (int)PacketType.PACKET_COUNT - 1)
                return;

            mPreType = (PacketType)mMessageType;
        }

        if (!mCompleted)
        {
            mCompleted = ReadBody(buffer, ref srcPosition);
            if (!mCompleted)
                return;
        }

        // 데이터가 완성되면 패킷으로 변환
        Packet packet = new Packet();
        packet.Type = mMessageType;
        packet.SetData(mMessageBuffer, mMessageSize);

        mCompletedCallback(packet);

        ClearBuffer();
    }

    public void ClearBuffer()
    {
        Array.Clear(mMessageBuffer, 0, mMessageBuffer.Length);
        Array.Clear(mHeaderBuffer, 0, mHeaderBuffer.Length);
        Array.Clear(mTypeBuffer, 0, mTypeBuffer.Length);

        mMessageSize = 0;
        mHeadPosition = 0;
        mTypePosition = 0;
        mCurrentPosition = 0;
        mMessageType = 0;
        mRemainBytes = 0;

        mHeadCompleted = false;
        mTypeCompleted = false;
        mCompleted = false;
    }

    private bool ReadHead(byte[] buffer, ref int srcPosition)
    {
        return ReadUntil(buffer, ref srcPosition, mHeaderBuffer, ref mHeadPosition, 4);
    }

    private bool ReadType(byte[] buffer, ref int srcPosition)
    {
        return ReadUntil(buffer, ref srcPosition, mTypeBuffer, ref mTypePosition, 2);
    }

    private bool ReadBody(byte[] buffer, ref int srcPosition)
    {
        return ReadUntil(buffer, ref srcPosition, mMessageBuffer, ref mCurrentPosition, mMessageSize);
    }

    private bool ReadUntil(byte[] buffer, ref int srcPosition, byte[] destBuffer, ref int destPosition, int toSize)
    {
        if (mRemainBytes < 0)
            return false;

        int copySize = toSize - destPosition;
        if (mRemainBytes < copySize)
            copySize = mRemainBytes;

        Array.Copy(buffer, srcPosition, destBuffer, destPosition, copySize);

        // 시작 위치를 옮겨준다
        srcPosition += copySize;
        destPosition += copySize;
        mRemainBytes -= copySize;

        return !(destPosition < toSize);
    }

    // 헤더로부터 데이터 전체 크기를 읽어온다
    private int GetBodySize()
    {
        return BitConverter.ToInt16(mHeaderBuffer, 0);
    }
}

public class SocketClient
{
    Socket m_socket;

    [Header("new network with transport")]
    public NetworkDriver Driver;
    public NetworkPipeline Pipeline;
    public NetworkConnection Connection;

    /// For receive data
    SocketAsyncEventArgs m_receive_event_args;
    MessageResolver m_message_resolver;
    LinkedList<Packet> m_receive_packet_list;
    private object m_mutex_receive_packet_list = new object();
    byte[] m_receive_buffer;

    public SocketClient()
    {
        var settings = new NetworkSettings();
        settings.WithNetworkConfigParameters(
            connectTimeoutMS: 1000,
            maxConnectAttempts: 10,
            disconnectTimeoutMS: 600000);
#if UNITY_EDITOR
        Driver = NetworkDriver.Create(settings);
#else
        Driver = NetworkDriver.Create(new WebSocketNetworkInterface(), settings);
#endif

        Pipeline = Driver.CreatePipeline(
            typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
    }

    void InitReceive()
    {
        m_receive_packet_list = new LinkedList<Packet>();
        m_receive_buffer = new byte[4096]; // socket buffer size
        m_message_resolver = new MessageResolver();

        /**
        m_receive_event_args = new SocketAsyncEventArgs();
        m_receive_event_args.Completed += OnReceiveCompleted;
        m_receive_event_args.UserToken = this;
        m_receive_event_args.SetBuffer(m_receive_buffer, 0, 1024 * 4);*/
    }

    public void Connect(string address, int port)
    {
        InitReceive();
        NetworkEndpoint endpoint;
        if (IPAddress.TryParse(address, out _))
        {
            endpoint = NetworkEndpoint.Parse(address, (ushort)port);
            Connection = Driver.Connect(endpoint);
            return;
        }
        else
        {
            var host = Dns.GetHostEntry(address);
            if (NetworkEndpoint.TryParse(host.AddressList[0].ToString(), (ushort)port, out endpoint))
            {
                Connection = Driver.Connect(endpoint);
                return;
            }
        }

        Debug.LogError("[CLIENT] Connection failed, Check address!");
        /**
        // TCP 통신
        m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // 그때그때 전송
        m_socket.NoDelay = true;

        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(address), port);

        // 비동기 접속
        SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs();
        eventArgs.Completed += OnConnected;
        eventArgs.RemoteEndPoint = endPoint;

        InitReceive();

        bool pending = m_socket.ConnectAsync(eventArgs);

        if (!pending)
        {
            OnConnected(null, eventArgs);
        }*/
    }

    public void Disconnect()
    {
        Connection.Disconnect(Driver);
        Connection = default;
        /**
        if (m_socket == null) return;

        var toSend = new Packet().Pack(PacketType.PACKET_USER_CLOSED, new TestPacket());
        Send(toSend);

        if (m_socket.Connected)
        {
            m_socket.Disconnect(false);
        }
        m_socket.Dispose();*/
    }

    public void Send(Packet packet)
    {
        var byteArr = new NativeArray<byte>(packet.GetSendBytes(), Allocator.Temp);

        var sendStatus = Driver.BeginSend(Pipeline, Connection, out var writer);
        writer.WriteBytes(byteArr);
        var endStatus = Driver.EndSend(writer);

        /**
        if (m_socket == null || !m_socket.Connected)
        {
            return;
        }

        // 이부분은 풀로 만들어서 써도 됨, SocketAsyncEventArgsPool.Instance.Pop();
        SocketAsyncEventArgs send_event_args = new SocketAsyncEventArgs();
        if (send_event_args == null)
        {
            Debug.LogError("new SocketAsyncEventArgs() result is null");
            return;
        }

        send_event_args.Completed += OnSendCompleted;
        send_event_args.UserToken = this;

        byte[] send_data = packet.GetSendBytes();
        send_event_args.SetBuffer(send_data, 0, send_data.Length);

        bool pending = m_socket.SendAsync(send_event_args);
        if (!pending)
        {
            OnSendCompleted(null, send_event_args);
        }*/
    }

    public void OnReceived(NativeArray<byte> value)
    {
        m_message_resolver.OnReceive(value.ToArray(), 0, value.Length, OnMessageCompleted);
    }

    void OnMessageCompleted(Packet packet)
    {
        PushPacket(packet);
    }

    void PushPacket(Packet packet)
    {
        lock (m_mutex_receive_packet_list)
        {
            m_receive_packet_list.AddLast(packet);
        }
    }

    public void ProcessPackets(NetworkManager.ParsePacketDelegate parser)
    {
        lock (m_mutex_receive_packet_list)
        {
            foreach (Packet packet in m_receive_packet_list)
            {
                parser(packet);
            }
            m_receive_packet_list.Clear();
        }
    }
}

