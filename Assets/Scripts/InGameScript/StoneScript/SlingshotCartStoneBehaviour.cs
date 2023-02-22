using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlingshotCartStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        BelongingPlayer.OnTurnEnd += SlingShot;

        base.OnEnter(calledByPacket, options);
    }
    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        BelongingPlayer.OnTurnEnd -= SlingShot;

        base.OnExit(calledByPacket, options);
    }
    private void SlingShot()
    {
        Debug.Log(2);
        List<Card> handCard = BelongingPlayer.GetHandCard();
        List<Card> oneCostCard = new List<Card>();
        foreach (Card card in handCard)
        {
            if (card.CardData.cardCost == 1)
            {
                oneCostCard.Add(card);
            }
        }
        Debug.Log(1);
        
        if (oneCostCard.Count <= 0)
        {
            Debug.Log(0);
            return;
        }

        int randNum = UnityEngine.Random.Range(0, oneCostCard.Count);

        Card oneCard = oneCostCard[randNum];
        SpawnStone(oneCard);
        //var stoneId = BelongingPlayer.SpawnStone(oneCard.CardData, transform.position,-1,true);
    }

    private void SpawnStone(Card card)
    {
        CardData cardData = card.CardData;
        var stoneID = GameManager.Inst.LocalPlayer.SpawnStone(cardData, transform.position, -1, true);
        StoneBehaviour stone = GameManager.Inst.FindStone(stoneID);
        var bulletAkg = stone.GetComponent<AkgRigidbody>();
        var thisAkg = GetComponent<AkgRigidbody>();
        bulletAkg.IgnoreCollide.Add(thisAkg);
        thisAkg.IgnoreCollide.Add(bulletAkg);
        GameManager.Inst.LocalPlayer.RemoveHandCardAndArrange(card, transform.position, stoneID);
        LocalPlayerBehaviour local = GameManager.Inst.LocalPlayer as LocalPlayerBehaviour;
        bool isRotated = (BelongingPlayerEnum == GameManager.PlayerEnum.LOCAL) == local.IsLocalRotated;
        Vector3 shootVel = isRotated ? new Vector3(0, 0, -1500) : new Vector3(0, 0, 1500);
        stone._Shoot(shootVel*stone.GetComponent<AkgRigidbody>().Mass, isRotated);

        // TODO: 쏜 stone이 가만히 있음, 호출 시점의 velocity == 0
        //stone._Shoot(-akgRigidbody.velocity, true);
    }
}
