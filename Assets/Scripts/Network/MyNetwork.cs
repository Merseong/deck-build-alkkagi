using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System;

namespace MyNetwork
{
    using MyNetworkData;

    public class SocketClient
    {
        Socket m_socket;

        /// For receive data
        SocketAsyncEventArgs m_receive_event_args;
        MessageResolver m_message_resolver;
        LinkedList<Packet> m_receive_packet_list;
        private object m_mutex_receive_packet_list = new object();
        GamePacketHandler m_game_packet_handler;
        byte[] m_receive_buffer;

        public Socket Socket => m_socket;

        void InitReceive()
        {
            m_receive_packet_list = new LinkedList<Packet>();
            m_receive_buffer = new byte[4096]; // socket buffer size
            m_message_resolver = new MessageResolver();

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
                InitReceive();
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

        public void ProcessPackets()
        {
            lock (m_mutex_receive_packet_list)
            {
                foreach (Packet packet in m_receive_packet_list)
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


        // TODO: 아래의 ParsePacket부분을 전부 event(delegate)로 돌리기
        public void ParsePacket(Packet packet)
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
                m_network.Send(sendPacket);
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
                m_network.Send(p);
            }
        }
    }
}
