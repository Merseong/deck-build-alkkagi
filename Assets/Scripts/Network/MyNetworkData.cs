using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MyNetworkData
{
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
    [StructLayout(LayoutKind.Sequential, Pack = 1)] // 1byte 단위
    public class Data<T> where T : class
    {
        public Data() { }

        public byte[] Serialize()
        {
            var size = Marshal.SizeOf(typeof(T));
            var array = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(this, ptr, true);
            Marshal.Copy(ptr, array, 0, size);
            Marshal.FreeHGlobal(ptr);
            return array;
        }

        public static T Deserialize(byte[] array)
        {
            var size = Marshal.SizeOf(typeof(T));
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(array, 0, ptr, size);
            var s = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
            return s;
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    public class PacketRes : Data<PacketRes>
    {
        public bool isSucess;
        public int testIntValue;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
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
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TestPacket : Data<TestPacket>
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string message;

        public TestPacket() { }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class MessagePacket : Data<MessagePacket>
    {
        public int senderID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string message;

        public MessagePacket() { }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SyncVarPacket : Data<SyncVarPacket>
    {
        public uint NetID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public byte[] Data;

        public SyncVarPacket() { }
    }


    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ShootStonePacket : Data<ShootStonePacket>
    {
        public int senderID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
        public VelocityRecord[] velocityRecords = new VelocityRecord[50];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        public PositionRecord[] positionRecords = new PositionRecord[30];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        public EventRecord[] eventRecords = new EventRecord[30];

        public short velocityCount;
        public short positionCount;
        public short eventCount;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VelocityRecord
    {
        public float time;
        public int stoneId;
        public float xVelocity;
        public float zVelocity;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PositionRecord
    {
        public int stoneId;
        public float xPosition;
        public float zPosition;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    // collide, drop out, stone power
    public struct EventRecord
    {
        public float time;
        public int stoneId;
        public EventEnum eventEnum;
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

        /// For receive data
        SocketAsyncEventArgs m_receive_event_args;
        MessageResolver m_message_resolver;
        LinkedList<Packet> m_receive_packet_list;
        private object m_mutex_receive_packet_list = new object();
        byte[] m_receive_buffer;

        void InitReceive()
        {
            m_receive_packet_list = new LinkedList<Packet>();
            m_receive_buffer = new byte[4096]; // socket buffer size
            m_message_resolver = new MessageResolver();

            m_receive_event_args = new SocketAsyncEventArgs();
            m_receive_event_args.Completed += OnReceiveCompleted;
            m_receive_event_args.UserToken = this;
            m_receive_event_args.SetBuffer(m_receive_buffer, 0, 1024 * 4);
        }

        public void Connect(string address, int port)
        {
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
            }
        }

        public void Disconnect()
        {
            if (m_socket == null) return;

            var toSend = new Packet().Pack(PacketType.PACKET_USER_CLOSED, new TestPacket());
            Send(toSend);

            if (m_socket.Connected)
            {
                m_socket.Disconnect(false);
            }
            m_socket.Dispose();
        }

        void StartReceive()
        {
            bool pending = m_socket.ReceiveAsync(m_receive_event_args);
            if (!pending)
            {
                OnReceiveCompleted(this, m_receive_event_args);
            }
        }

        public void Send(Packet packet)
        {
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
            }
        }

        void OnConnected(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success) // 연결성공
            {
                Debug.Log("hello network!");
                StartReceive();

                // Send test data
                var toSend = new TestPacket();
                toSend.message = "hello world!";
                var sendPacket = new Packet();
                sendPacket.Type = (short)PacketType.PACKET_TEST;
                var toSendSerial = toSend.Serialize();
                sendPacket.SetData(toSendSerial, toSendSerial.Length);
                Send(sendPacket);
            }
            else // 연결 실패
            {
                Debug.LogError("Failed");
            }
        }

        void OnReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success) // 수신 성공
            {
                m_message_resolver.OnReceive(e.Buffer, e.Offset, e.BytesTransferred, OnMessageCompleted);
                StartReceive();
            }
            else // 수신 실패
            {
                Debug.LogError(e.SocketError);
            }
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

        void OnSendCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success) // 전송 성공
            {
                Debug.Log("Done send");
            }
            else // 전송 실패
            {

            }

            //사용했던 SocketAsyncEventArgs객체를 풀에 다시 넣어준다.
            //풀에 넣기 전에 해당 객체를 초기화 해 준다.
            e.Completed -= OnSendCompleted;
            //SocketAsyncEventArgsPool.Instance.Push(e);
        }
    }
}

