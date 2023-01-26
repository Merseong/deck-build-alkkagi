using System;
using System.Collections;
using System.Collections.Generic;
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
}

