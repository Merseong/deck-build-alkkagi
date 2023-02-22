using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEngine;

public class SocketClient
{
    [Header("new network with transport")]
    public NetworkDriver Driver;
    public NetworkPipeline Pipeline;
    public NetworkConnection Connection;

    /// For receive data
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
            disconnectTimeoutMS: 600000,
            heartbeatTimeoutMS: 10000);
        settings.WithFragmentationStageParameters(payloadCapacity: 1048576);
        settings.WithReliableStageParameters(windowSize: 64);
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
        //Debug.Log($"{writer.Capacity} {byteArr.Length}");
        var endStatus = Driver.EndSend(writer);

        //Debug.Log($"{sendStatus} {endStatus}");

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
