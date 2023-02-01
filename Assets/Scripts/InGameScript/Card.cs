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
}
