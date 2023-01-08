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
    Socket m_socket;

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

    void OnConnected(object sender, SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success) // 연결성공
        {
            Debug.Log("hello network!");
        }
        else // 연결 실패
        {
            Debug.LogError("Failed");
        }
    }

    private void Start()
    {
        if (m_isNetworkMode)
        {
            Connect("127.0.0.1", 3333);
        }
    }
}

namespace myNetwork
{
    public class myPacket
    {
        public Int16 m_type { get; set; }
        public byte[] m_data { get; set; }

        public myPacket() { }

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
    public class myData<T> where T : class
    {
        public myData() { }

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
    public class TestPacket : myData<TestPacket>
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string m_message;

        public TestPacket() { }
    }
}
