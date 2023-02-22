using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlingshotCartStoneBehaviour : StoneBehaviour
{
    string option = "";
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        BelongingPlayer.OnTurnEnd += SlingShot;

        //base.OnEnter(calledByPacket, option);
    }
    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        BelongingPlayer.OnTurnEnd -= SlingShot;

        base.OnExit(calledByPacket, options);
    }
    public override void ParseActionString(string actionStr)
    {
        // "SPAWN {stoneID} {cardData.CardID}"
        if (!actionStr.StartsWith("SPAWN")) return;
        var actionArr = actionStr.Split(' ');
        StoneBehaviour stone = GameManager.Inst.FindStone(int.Parse(actionArr[1]));

        var spawnAkg = stone.GetComponent<AkgRigidbody>();
        var thisAkg = GetComponent<AkgRigidbody>();
        spawnAkg.IgnoreCollide.Add(thisAkg);
        thisAkg.IgnoreCollide.Add(spawnAkg);
    }
    private void SlingShot()
    {
        if (GameManager.Inst.TurnCount == 0)
            return;

        if (BelongingPlayerEnum == GameManager.PlayerEnum.LOCAL)
        {
            List<Card> handCard = BelongingPlayer.GetHandCard();
            List<Card> oneCostCard = new List<Card>();
            foreach (Card card in handCard)
            {
                if (card.CardData.cardCost == 1)
                {
                    oneCostCard.Add(card);
                }
            }

            if (oneCostCard.Count <= 0)
            {
                return;
            }

            int randNum = Random.Range(0, oneCostCard.Count);

            Card oneCard = oneCostCard[randNum];
            CardData cardData = oneCard.CardData;

            LocalPlayerBehaviour local = GameManager.Inst.LocalPlayer as LocalPlayerBehaviour;
            bool isRotated = (BelongingPlayerEnum == GameManager.PlayerEnum.LOCAL) == local.IsLocalRotated;
            GameManager.Inst.StartCoroutine(EShoot(isRotated, true));

            var stoneID = GameManager.Inst.LocalPlayer.SpawnStone(cardData, transform.position, -1, true);
            AkgPhysicsManager.Inst.rigidbodyRecorder.AppendEventRecord(StoneId, $"SPAWN {stoneID} {cardData.CardID}");
            GameManager.Inst.LocalPlayer.RemoveHandCardAndArrange(oneCard, transform.position, stoneID);
            StoneBehaviour stone = GameManager.Inst.FindStone(stoneID);

            var spawnAkg = stone.GetComponent<AkgRigidbody>();
            var thisAkg = GetComponent<AkgRigidbody>();
            spawnAkg.IgnoreCollide.Add(thisAkg);
            thisAkg.IgnoreCollide.Add(spawnAkg);
            StartCoroutine(ELeaveCollider(spawnAkg, thisAkg));


            Vector3 shootVel = isRotated ? new Vector3(0, 0, -100) : new Vector3(0, 0, 100);
            stone.ChildShoot(shootVel, isRotated, true);
        }
    }

    private IEnumerator ELeaveCollider(AkgRigidbody r1, AkgRigidbody r2)
    {
        yield return new WaitWhile(() => r1.CheckCollide(r2, out _));
        yield return new WaitForSeconds(0.1f);

        r1.IgnoreCollide.Remove(r2);
        r2.IgnoreCollide.Remove(r1);
    }
}
