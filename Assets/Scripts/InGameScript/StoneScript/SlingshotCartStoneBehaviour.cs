using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlingshotCartStoneBehaviour : StoneBehaviour
{
    string option = "";
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        if(calledByPacket)
        {
            if(!options.Equals(""))
            {
                BelongingPlayer.OnTurnEnd += SlingShot_Oppo;
            }
        }
        else
        {
            BelongingPlayer.OnTurnEnd += SlingShot;
        } 
            
        base.OnEnter(calledByPacket, option);
    }
    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        BelongingPlayer.OnTurnEnd -= SlingShot;
        BelongingPlayer.OnTurnEnd -= SlingShot_Oppo;

        base.OnExit(calledByPacket, options);
    }
    private void SlingShot()
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

        var stoneID = GameManager.Inst.LocalPlayer.SpawnStone(cardData, transform.position, -1, true);
        StoneBehaviour stone = GameManager.Inst.FindStone(stoneID);
        

        var spawnAkg = stone.GetComponent<AkgRigidbody>();
        var thisAkg = GetComponent<AkgRigidbody>();
        spawnAkg.IgnoreCollide.Add(thisAkg);
        thisAkg.IgnoreCollide.Add(spawnAkg);

        GameManager.Inst.LocalPlayer.RemoveHandCardAndArrange(oneCard, transform.position, stoneID);
        
        LocalPlayerBehaviour local = GameManager.Inst.LocalPlayer as LocalPlayerBehaviour;
        bool isRotated = (BelongingPlayerEnum == GameManager.PlayerEnum.LOCAL) == local.IsLocalRotated;
        Vector3 shootVel = isRotated ? new Vector3(0, 0, -1500) : new Vector3(0, 0, 1500);
        stone._Shoot(shootVel*stone.GetComponent<AkgRigidbody>().Mass, isRotated);
    }

    private void SlingShot_Oppo()
    {
        StoneBehaviour stone = GameManager.Inst.FindStone(int.Parse(option));

        var spawnAkg = stone.GetComponent<AkgRigidbody>();
        var thisAkg = GetComponent<AkgRigidbody>();
        spawnAkg.IgnoreCollide.Add(thisAkg);
        thisAkg.IgnoreCollide.Add(spawnAkg);   
    }
}
