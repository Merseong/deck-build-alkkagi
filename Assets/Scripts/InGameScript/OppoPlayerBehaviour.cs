using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OppoPlayerBehaviour : PlayerBehaviour
{
    [Header("Opponent Settings")]
    //Temp
    public GameObject StonePrefab;
    public UserDataPacket oppoUserData;

    private void Start()
    {
        NetworkManager.Inst.AddReceiveDelegate(PlayCardReceiveNetworkAction);    
    }

    private void OnDestroy()
    {
        if (NetworkManager.IsEnabled)
            NetworkManager.Inst.RemoveReceiveDelegate(PlayCardReceiveNetworkAction);
    }

    public override void InitPlayer(GameManager.PlayerEnum pEnum, uint uid)
    {
        base.InitPlayer(pEnum, uid);
        if (pEnum != GameManager.PlayerEnum.OPPO)
        {
            Debug.LogError("[OPPO] player enum not matched!!");
        }

        NetworkManager.Inst.UpdateUserData(uid, (oppoUser) =>
        {
            oppoUserData = oppoUser;
            IngameUIManager.Inst.SetEnemyInfo(oppoUser.nickname, oppoUser.win + oppoUser.honorWin, oppoUser.lose + oppoUser.honorLose);
        });

        if (!NetworkManager.Inst.IsNetworkMode)
        {
            oppoUserData = new UserDataPacket
            {
                nickname = "test",
                rating = 1000,
                uid = 12345,
            };
        }
    }

    public override int SpawnStone(CardData cardData, Vector3 spawnPosition, int stoneId = -1, bool ignoreSpawnPos = false)
    {
        //FIXME : 카드에 맞는 스톤을 런타임에 생성해줘야 함
        GameObject spawnedStone = Instantiate(StonePrefab, spawnPosition, Quaternion.identity);
        //var stoneBehaviour = spawnedStone.GetComponent<StoneBehaviour>();
        Type stoneType = StoneBehaviour.GetStoneWithID(cardData.CardID);
        Debug.Log(stoneType);
        var stoneBehaviour = spawnedStone.AddComponent(stoneType) as StoneBehaviour;
        stoneBehaviour.enabled = true;
        var newStoneId = AddStone(stoneBehaviour);
        stoneBehaviour.SetCardData(cardData, newStoneId, Player);
        stoneBehaviour.InitProperty();
        stoneBehaviour.PrintProperty();

        //temp code
        stoneBehaviour.ChangeSpriteAndRot("Idle", !GameManager.Inst.isLocalGoFirst);
        spawnedStone.transform.GetChild(3).GetComponent<SpriteRenderer>().sprite = IngameUIManager.Inst.UIAtlas.GetSprite("UI_Stone_r");

        var radius = Util.GetRadiusFromStoneSize(cardData.stoneSize);
        spawnedStone.transform.localScale = new Vector3(radius * 2, .15f, radius * 2);
        spawnedStone.GetComponent<AkgRigidbody>().Init(radius, Util.GetMassFromStoneWeight(cardData.stoneSize, cardData.stoneWeight));

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
        this.cost = (ushort)cost;
        this.handCount = (ushort)hand;
        RefreshUI();
    }
}
