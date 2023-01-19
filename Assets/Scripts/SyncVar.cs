using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// 동기화용 SyncVar 제너릭 클래스
// NetID 기반으로 SyncVar 구별함(NetID 같으면 같은 필드)
// 아직은 NetID 클라쪽에서 수동으로 설정해야함 (나중에 머성 선배가 고칠꺼임)
// OnChangeEventHandler로 데이터 변경시 콜백 관리
// OnReceiveData: 데이터가 변경될때
// OnSendData: 데이터를 변경할때
public class SyncVar<T> where T : struct
{
    public uint NetID;
    private byte[] data;
    public T Data
    {
        get
        {
            return Deserialize(data);
        }
        set
        {
            data = Serialize(value);
            UpdateData();
        }
    }

    public delegate void OnChangeEventHandler(T data);
    public event OnChangeEventHandler OnReceiveData;
    public event OnChangeEventHandler OnSendData;

    public SyncVar()
    {
        NetID = 0;

        NetworkManager.Inst.InitSyncVar(NetID, data, ChangeData);
    }

    private void UpdateData()
    {
        // 클라 사이드 SyncVarList 업데이트
        var (_data, _callback) = NetworkManager.Inst.SyncVarDict[NetID];
        _data = data;
        NetworkManager.Inst.SyncVarDict[NetID] = (_data, _callback);

        // 서버 사이드 SyncVarList 업데이트
        var toSend = new MyNetworkData.SyncVarPacket();
        toSend.NetID = NetID;
        toSend.Data = data;
        var sendData = toSend.Serialize();
        var packet = new MyNetworkData.Packet();
        packet.m_type = (Int16)MyNetworkData.PacketType.SYNCVAR_CHANGE;
        packet.SetData(sendData, sendData.Length);
        NetworkManager.Inst.Client?.Send(packet);

        if (OnSendData != null)
        {
            OnSendData(Deserialize(data));
        }
    }

    private void ChangeData()
    {
        data = NetworkManager.Inst.SyncVarDict[NetID].data;

        if (OnReceiveData != null)
        {
            OnReceiveData(Data);
        }
    }

    public byte[] Serialize(T data)
    {
        var size = Marshal.SizeOf(typeof(T));
        var array = new byte[size];
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(data, ptr, true);
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
