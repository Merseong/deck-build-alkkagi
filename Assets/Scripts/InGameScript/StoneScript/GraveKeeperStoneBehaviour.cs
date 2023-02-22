using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraveKeeperStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        if(BelongingPlayer.Player == GameManager.PlayerEnum.LOCAL)
        {
            SpawnStone();
        }

        base.OnEnter(calledByPacket, options);
    }

    private void SpawnStone()
    {
        if(GameManager.Inst.localDeadStones.Count == 0) return;
        var temp = Random.Range(0, GameManager.Inst.localDeadStones.Count);
        var stone = GameManager.Inst.localDeadStones[temp];
        GameManager.Inst.localDeadStones.Remove(stone);

        List<Transform> spawnablePos = GameManager.Inst.GameBoard.GetPossiblePutTransform(GameManager.PlayerEnum.LOCAL, CardData);
        CardData cardData = Util.GetCardDataFromID(stone, GameManager.Inst.CardDatas);

        if(spawnablePos.Count == 0 || cardData == null) 
        {
            Debug.Log("Unable to spawn Stone!");
            return;    
        }
        Vector3 spawnPos = spawnablePos[Random.Range(0, spawnablePos.Count-1)].position;
        var stoneID = GameManager.Inst.LocalPlayer.SpawnStone(cardData, spawnPos);
        (BelongingPlayer as LocalPlayerBehaviour).PlayCardSendNetworkAction(cardData, spawnPos, stoneID);
    }
}