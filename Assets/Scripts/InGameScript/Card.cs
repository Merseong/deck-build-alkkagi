using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class Card : MonoBehaviour
{
    [SerializeField] private CardData cardData;
    [SerializeField] private SpriteRenderer stoneSize;
    [SerializeField] private SpriteRenderer stoneWeight;
    [SerializeField] private SpriteRenderer character;
    [SerializeField] private TextMeshPro costText;
    [SerializeField] private TextMeshPro nameText;
    // public CardData CardData { set { cardData = value; } }
    public CardData CardData { get { return cardData; } set { cardData = value; } }

    public RPS originRPS;

    int originOrder;

    /// <summary>
    /// carddata값을 바탕으로 기본세팅
    /// </summary>
    public void Setup(Card card)
    {
        this.CardData = card.CardData;
        SetSprite(cardData);
    }

    public void SetSprite(CardData cardData)
    {
        //stoneSize.sprite = ;
        //stoneWeight.sprite = ;
        character.sprite = Util.GetSpriteState(cardData,"Idle",GameManager.Inst.stoneAtlas);
        costText.text = cardData.cardCost.ToString();
        nameText.text = cardData.cardName;
    }


    void OnMouseOver()
    {
        // EnlargeCard(true);
    }

    private void OnMouseExit()
    {
        // EnlargeCard(false);
    }

    public void SetOriginOrder(int originOrder)
    {
        this.originOrder = originOrder;
        SetOrder(originOrder);
    }

    public void SetMostFrontOrder(bool isMostFront)
    {
        SetOrder(isMostFront ? 100 : originOrder);
    }

    public void SetOrder(int order)
    {
        int mulOrder = order * 10;
    }

    public void MoveTransform(RPS rps, bool useDotween, float dotweenTime = 0)
    {
        if (useDotween)
        {
            transform.DOMove(rps.pos, dotweenTime);
            transform.DORotateQuaternion(rps.rot, dotweenTime);
            transform.DOScale(rps.scale, dotweenTime);
        }
        else
        {
            transform.position = rps.pos;
            transform.rotation = rps.rot;
            transform.localScale = rps.scale;
        }
    }

    public void EnlargeCard(bool isEnlarge)
    {
        if(IngameUIManager.Inst.isThereActivatedUI())
        {
            MoveTransform(originRPS, false);
            return;   
        } 
        if (isEnlarge)
        {
            float enlargeZ = (GameManager.Inst.LocalPlayer as LocalPlayerBehaviour).IsLocalRotated ? 7f : -7f;
            Vector3 enlargePos = new Vector3(originRPS.pos.x, 10.0f, enlargeZ);
            MoveTransform(new RPS(enlargePos, Quaternion.identity, originRPS.scale * 2), false);
        }
        else
        {
            MoveTransform(originRPS, false);
        }
        SetMostFrontOrder(isEnlarge);
    }
}
