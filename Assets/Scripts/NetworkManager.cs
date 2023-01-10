using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    [SerializeField]
    private bool m_isNetworkMode;
    MyNetwork.SocketClient m_client;

    [SerializeField]
    private string m_messageToSend;

    [SerializeField]
    private GameObject m_networkTestCanvas;

    private void Start()
    {
        m_networkTestCanvas.SetActive(m_isNetworkMode);
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

        var toSend = new MyNetworkData.TestPacket();
        var sendPacket = new MyNetworkData.Packet();
        sendPacket.m_type = (short)MyNetworkData.PacketType.ROOM_ENTER;
        var toSendSerial = toSend.Serialize();
        sendPacket.SetData(toSendSerial, toSendSerial.Length);
        m_client.Send(sendPacket);
    }

    public void SendMessageToOpponent()
    {
        if (!m_isNetworkMode)
        {
            return;
        }

        var toSend = new MyNetworkData.MessagePacket();
        toSend.m_senderid = -1;
        toSend.m_message = m_messageToSend;
        var sendPacket = new MyNetworkData.Packet();
        sendPacket.m_type = (short)MyNetworkData.PacketType.ROOM_OPPONENT;
        var toSendSerial = toSend.Serialize();
        sendPacket.SetData(toSendSerial, toSendSerial.Length);
        m_client.Send(sendPacket);

        m_messageToSend += "a";
    }
}

namespace MyNetworkData
{
    public class Packet
    {
        public Int16 m_type { get; set; }
        public byte[] m_data { get; set; }

        public Packet() { }

        public void SetData(byte[] data, int len)
        {
            m_data = new byte[len];
            Array.Copy(data, m_data, len);
        }

        public byte[] GetSendBytes()
        {
            byte[] type_bytes = BitConverter.GetBytes(m_type);
            int header_size = (int)(m_data.Length);
            byte[] header_bytes = BitConverter.GetBytes(header_size);
            // 헤더 + 패킷 타입 + 데이터(객체의 직렬화)
            byte[] send_bytes = new byte[header_bytes.Length + type_bytes.Length + m_data.Length];

            // 헤더 복사 0 ~ header len
            Array.Copy(header_bytes, 0, send_bytes, 0, header_bytes.Length);

            // 타입 복사 header len ~ header len + type len 
            Array.Copy(type_bytes, 0, send_bytes, header_bytes.Length, type_bytes.Length);

            // 데이터 복사
            Array.Copy(m_data, 0, send_bytes, header_bytes.Length + type_bytes.Length, m_data.Length);

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
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TestPacket : Data<TestPacket>
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string m_message;

        public TestPacket() { }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class MessagePacket : Data<MessagePacket>
    {
        public int m_senderid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string m_message;

        public MessagePacket() { }
    }

    public enum PacketType
    {
        UNDEFINED,
        PACKET_USER_CLOSED,
        TEST_PACKET,
        ROOM_BROADCAST,
        ROOM_OPPONENT,
        ROOM_ENTER,
        PACKET_COUNT
    }

    public class MessageResolver
    {
        public delegate void CompletedMessageCallback(Packet packet);

        int m_message_size;
        byte[] m_message_buffer = new byte[1024 * 2000];
        byte[] m_header_buffer = new byte[4];
        byte[] m_type_buffer = new byte[2];

        PacketType m_pre_type;

        int m_head_position;
        int m_type_position;
        int m_current_position;

        short m_message_type;
        int m_remain_bytes;

        bool m_head_completed;
        bool m_type_completed;
        bool m_completed;

        CompletedMessageCallback m_completed_callback;

        public MessageResolver()
        {
            ClearBuffer();
        }

        public void ClearBuffer()
        {
            Array.Clear(m_message_buffer, 0, m_message_buffer.Length);
            Array.Clear(m_header_buffer, 0, m_header_buffer.Length);
            Array.Clear(m_type_buffer, 0, m_type_buffer.Length);

            m_message_size = 0;
            m_head_position = 0;
            m_type_position = 0;
            m_current_position = 0;
            m_message_type = 0;

            m_head_completed = false;
            m_type_completed = false;
            m_completed = false;
        }

        public void OnReceive(byte[] buffer, int offset, int transferred, CompletedMessageCallback callback)
        {
            int src_position = offset; // 현재 들어온 데이터의 위치
            m_completed_callback = callback; // 메세지가 완성된 경우 호출하는 콜백
            m_remain_bytes = transferred; // 남은 처리할 메세지 양

            if (!m_head_completed)
            {
                m_head_completed = readHead(buffer, ref src_position);

                if (!m_head_completed)
                    return;

                m_message_size = getBodySize();

                if (m_message_size < 0 || m_message_size > 1024*2000)
                {
                    return;
                }
            }

            if (!m_type_completed)
            {
                //남은 데이터가 있다면, 타입 정보를 완성한다.
                m_type_completed = readType(buffer, ref src_position);

                //타입 정보를 완성하지 못했다면, 다음 메시지 전송을 기다린다.
                if (!m_type_completed)
                    return;

                //타입 정보를 완성했다면, 패킷 타입을 정의한다. (enum type)
                m_message_type = BitConverter.ToInt16(m_type_buffer, 0);


                //잘못된 데이터인지 확인
                if (m_message_type < 0 ||
                   m_message_type > (int)PacketType.PACKET_COUNT - 1)
                {
                    return;
                }

                //데이터가 미완성일 경우, 다음에 전송되었을 때를 위해 저장해 둔다.
                m_pre_type = (PacketType)m_message_type;
            }


            if (!m_completed)
            {
                //남은 데이터가 있다면, 데이터 완성과정을 진행한다.
                m_completed = readBody(buffer, ref src_position);
                if (!m_completed)
                    return;
            }

            //데이터가 완성 되었다면, 패킷으로 만든다.
            Packet packet = new Packet();
            packet.m_type = m_message_type;
            packet.SetData(m_message_buffer, m_message_size);

            //패킷이 완성 되었음을 알린다.
            m_completed_callback(packet);

            //패킷을 만드는데, 사용한 버퍼를 초기화 해준다.
            ClearBuffer();
        }

        private bool readHead(byte[] buffer, ref int src_position)
        {
            return readUntil(buffer, ref src_position, m_header_buffer, ref m_head_position, 4);
        }

        private bool readType(byte[] buffer, ref int src_position)
        {
            return readUntil(buffer, ref src_position, m_type_buffer, ref m_type_position, 2);
        }

        private bool readBody(byte[] buffer, ref int src_position)
        {
            return readUntil(buffer, ref src_position, m_message_buffer, ref m_current_position, m_message_size);
        }

        bool readUntil(byte[] buffer, ref int src_position, byte[] dest_buffer, ref int dest_position, int to_size)
        {
            //남은 데이터가 없다면, 리턴
            if (m_remain_bytes < 0)
                return false;

            int copy_size = to_size - dest_position;
            if (m_remain_bytes < copy_size)
                copy_size = m_remain_bytes;

            Array.Copy(buffer, src_position, dest_buffer, dest_position, copy_size);

            //시작 위치를 옮겨준다.
            src_position += copy_size;
            dest_position += copy_size;
            m_remain_bytes -= copy_size;

            return !(dest_position < to_size);
        }

        int getBodySize()
        {
            Type type = ((Int16)1).GetType();
            if (type.Equals(typeof(Int16)))
            {
                return BitConverter.ToInt16(m_header_buffer, 0);
            }

            return BitConverter.ToInt32(m_header_buffer, 0);
        }
    }
}

namespace MyNetwork
{
    public class SocketClient
    {
        Socket m_socket;

        /// For receive data
        SocketAsyncEventArgs m_receive_event_args;
        MyNetworkData.MessageResolver m_message_resolver;
        LinkedList<MyNetworkData.Packet> m_receive_packet_list;
        private object m_mutex_receive_packet_list = new object();
        GamePacketHandler m_game_packet_handler;
        byte[] m_receive_buffer;

        void InitReceive()
        {
            m_receive_packet_list = new LinkedList<MyNetworkData.Packet>();
            m_receive_buffer = new byte[4096]; // socket buffer size
            m_message_resolver = new MyNetworkData.MessageResolver();

            m_game_packet_handler = new GamePacketHandler(this);

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

            bool pending = m_socket.ConnectAsync(eventArgs);

            if (!pending)
            {
                OnConnected(null, eventArgs);
            }
        }

        void StartReceive()
        {
            bool pending = m_socket.ReceiveAsync(m_receive_event_args);
            if (!pending)
            {
                OnReceiveCompleted(this, m_receive_event_args);
            }
        }

        public void Send(MyNetworkData.Packet packet)
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
                InitReceive();
                StartReceive();

                // Send test data
                var toSend = new MyNetworkData.TestPacket();
                toSend.m_message = "hello world!";
                var sendPacket = new MyNetworkData.Packet();
                sendPacket.m_type = (short)MyNetworkData.PacketType.TEST_PACKET;
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

        void OnMessageCompleted(MyNetworkData.Packet packet)
        {
            PushPacket(packet);
        }

        void PushPacket(MyNetworkData.Packet packet)
        {
            lock(m_mutex_receive_packet_list)
            {
                m_receive_packet_list.AddLast(packet);
            }

            ProcessPackets();
        }

        public void ProcessPackets()
        {
            lock (m_mutex_receive_packet_list)
            {
                foreach(MyNetworkData.Packet packet in m_receive_packet_list)
                {
                    m_game_packet_handler.ParsePacket(packet);
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

    public class GamePacketHandler
    {
        SocketClient m_network;

        public GamePacketHandler(SocketClient network)
        {
            m_network = network;
        }

        public void ParsePacket(MyNetworkData.Packet packet)
        {
            switch ((MyNetworkData.PacketType)packet.m_type)
            {
                case MyNetworkData.PacketType.TEST_PACKET:
                    ParseTestPacket(packet);
                    break;
                case MyNetworkData.PacketType.ROOM_OPPONENT:
                    ParseMessagePacket(packet);
                    break;
            }
        }

        public void ParseTestPacket(MyNetworkData.Packet packet)
        {
            MyNetworkData.TestPacket tp = MyNetworkData.TestPacket.Deserialize(packet.m_data);

            Debug.Log("from server - " + tp.m_message);
        }

        public void ParseMessagePacket(MyNetworkData.Packet packet)
        {
            MyNetworkData.MessagePacket mp = MyNetworkData.MessagePacket.Deserialize(packet.m_data);

            Debug.LogWarning("from " + mp.m_senderid + " - " + mp.m_message);
        }
    }
}
