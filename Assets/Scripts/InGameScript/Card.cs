using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Card : MonoBehaviour
{
    [SerializeField] private CardData cardData;
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
    }

    void OnMouseOver()
    {
        if(GameManager.Inst.isCancelOpened) return;
        EnlargeCard(true);
    }

    private void OnMouseExit()
    {
        EnlargeCard(false);
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

    void EnlargeCard(bool isEnlarge)
    {
        if(GameManager.Inst.isInformOpened)
        {
            MoveTransform(originRPS, false);
            return;   
        } 
        if (isEnlarge)
        {
            Vector3 enlargePos = new Vector3(originRPS.pos.x, 10.0f, -7f);
            MoveTransform(new RPS(enlargePos, Quaternion.identity, originRPS.scale * 2), false);
        }
        else
        {
            MoveTransform(originRPS, false);
        }
        SetMostFrontOrder(isEnlarge);
    }
}
