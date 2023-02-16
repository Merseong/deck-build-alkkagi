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

}
