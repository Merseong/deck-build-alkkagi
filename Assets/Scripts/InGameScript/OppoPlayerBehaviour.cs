using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OppoPlayerBehaviour : PlayerBehaviour
{
    [Header("Opponent Settings")]
    private bool test;

    public override void InitPlayer(GameManager.PlayerEnum pEnum)
    {
        base.InitPlayer(pEnum);
        if (pEnum != GameManager.PlayerEnum.OPPO)
        {
            Debug.LogError("[OPPO] player enum not matched!!");
        }
    }

    public override void DrawCards(int number)
    {
        throw new System.NotImplementedException();
    }

    protected override void RemoveCards(int idx)
    {
        base.RemoveCards(idx);
    }

    public override int SpawnStone(CardData cardData, Vector3 spawnPosition, int stoneId = -1)
    {
        throw new System.NotImplementedException();
    }

    private void PlayCardReceiveNetworkAction(MyNetworkData.Packet packet)
    {
        if (packet.Type != (short)MyNetworkData.PacketType.ROOM_OPPONENT) return;

        var msg = MyNetworkData.MessagePacket.Deserialize(packet.Data);

        if (!msg.message.StartsWith("PLAYCARD/")) return;

        Debug.Log($"[OPPO] {msg.message}");

        // parse message
        var dataArr = msg.message.Split(' ');
        // TODO: Oppo의 playcard를 불러야하긴한데
        SpawnStone
        (
            GameManager.Inst.GetCardDataById(int.Parse(dataArr[1])),
            StringToVector3(dataArr[2]),
            int.Parse(dataArr[3])
        );
        //if (GameManager.Inst.OppoPlayer.Cost != Int16.Parse(dataArr[4]))
        //{
        //    // 상대의 남은 코스트와 내가 계산한 코스트가 안맞음
        //    Debug.LogError("[OPPO] PLAYCARD cost not matched!");
        //    return;
        //}
    }
}
