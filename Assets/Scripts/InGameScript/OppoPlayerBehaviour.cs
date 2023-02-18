using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OppoPlayerBehaviour : PlayerBehaviour
{
    [Header("Opponent Settings")]
    //Temp
    public GameObject StonePrefab;

    private void Start()
    {
        NetworkManager.Inst.AddReceiveDelegate(PlayCardReceiveNetworkAction);    
    }

    private void OnDestroy()
    {
        if (NetworkManager.IsEnabled)
            NetworkManager.Inst.RemoveReceiveDelegate(PlayCardReceiveNetworkAction);
    }

    public override void InitPlayer(GameManager.PlayerEnum pEnum)
    {
        base.InitPlayer(pEnum);
        if (pEnum != GameManager.PlayerEnum.OPPO)
        {
            Debug.LogError("[OPPO] player enum not matched!!");
        }
    }

    public override int SpawnStone(CardData cardData, Vector3 spawnPosition, int stoneId = -1)
    {
        //FIXME : 카드에 맞는 스톤을 런타임에 생성해줘야 함
        GameObject spawnedStone = Instantiate(StonePrefab, spawnPosition, Quaternion.identity);
        var stoneBehaviour = spawnedStone.GetComponent<StoneBehaviour>();
        var newStoneId = AddStone(stoneBehaviour);
        stoneBehaviour.SetCardData(cardData, newStoneId, Player);

        //temp code
        spawnedStone.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = stoneBehaviour.GetSpriteState("Idle");
        spawnedStone.transform.GetChild(4).GetComponent<SpriteRenderer>().material.color = Color.red;
        spawnedStone.transform.localScale = new Vector3(Util.GetRadiusFromStoneSize(cardData.stoneSize), .15f, Util.GetRadiusFromStoneSize(cardData.stoneSize));
        spawnedStone.GetComponent<AkgRigidbody>().Init(Util.GetMassFromStoneWeight(cardData.stoneSize, cardData.stoneWeight));

        return newStoneId;
    }

    private void PlayCardReceiveNetworkAction(Packet packet)
    {
        if (packet.Type != (short)PacketType.ROOM_OPPONENT) return;

        var msg = MessagePacket.Deserialize(packet.Data);

        if (!msg.message.StartsWith("PLAYCARD/")) return;

        Debug.Log($"[OPPO] {msg.message}");

        // parse message
        var dataArr = msg.message.Split(' ');
        SpawnStone
        (
            GameManager.Inst.GetCardDataById(int.Parse(dataArr[1])),
            StringToVector3(dataArr[2]),
            int.Parse(dataArr[3])
        );

        Cost = ushort.Parse(dataArr[4]);
        RemoveCards(0);
        
        //if (GameManager.Inst.OppoPlayer.Cost != int.Parse(dataArr[4]))
        //{
        //    // 상대의 남은 코스트와 내가 계산한 코스트가 안맞음
        //    Debug.LogError("[OPPO] PLAYCARD cost not matched!");
        //    return;
        //}
    }

    public override void RefreshUI()
    {
        base.RefreshUI();
        IngameUIManager.Inst.SetEnemyData(this);
    }

    public void SetCostHandValue(short cost, short hand)
    {
        Cost = (ushort)cost;
        HandCount = (ushort)hand;
    }
}
