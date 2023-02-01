using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : SingletonBehavior<CardManager>
{
    [SerializeField] GameObject cardPrefab;
    [SerializeField] List<Card> handPile;
    [SerializeField] Transform cardSpawnPoint;
    [SerializeField] Transform handPileLeft;
    [SerializeField] Transform handPileRight;

    [SerializeField]List<Card> deck;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            AddCard();
        }
    }

    public Card DrawCard()
    {
        if (deck.Count == 0)
        {
            Debug.LogError("Card 부족");
            return null;
        }
        Card card = deck[0];
        deck.RemoveAt(0);
        return card;
    }

    void AddCard()
    {
        Card drawCard = DrawCard();
        if (drawCard == null)
        {
            return;
        }

        var cardObject = Instantiate(cardPrefab, cardSpawnPoint.position, Quaternion.identity);
        var card = cardObject.GetComponent<Card>();
        card.Setup(drawCard);
        handPile.Add(card);

        SetOriginOrder();
        CardAlignment();
    }

    void SetOriginOrder()
    {
        int count = handPile.Count;
        for (int i = 0; i < count; i++)
        {
            var targetCard = handPile[i];
            targetCard?.GetComponent<Card>().SetOriginOrder(i);
        }
    }

    void CardAlignment()
    {
        List<RPS> originCardRPSs = new List<RPS>();
        originCardRPSs = RoundAlignment(handPileLeft, handPileRight, handPile.Count, 0.5f, new Vector3(2.0f, 0.5f, 3.0f));


        var targetCards = handPile;

        for (int i = 0; i < targetCards.Count; i++)
        {
            var targetCard = targetCards[i];

            targetCard.originRPS = originCardRPSs[i];
            targetCard.MoveTransform(targetCard.originRPS, true, 0.7f);
        }
    }

    List<RPS> RoundAlignment(Transform leftTr, Transform rightTr, int objCount, float height, Vector3 scale)
    {
        float[] objLerps = new float[objCount];
        List<RPS> results = new List<RPS>(objCount);

        switch (objCount) 
        {
            case 1: objLerps = new float[] { 0.5f }; break;
            case 2: objLerps = new float[] { 0.27f, 0.73f }; break;
            case 3: objLerps = new float[] { 0.1f, 0.5f, 0.9f }; break;
            default:
                float interval = 1f / (objCount - 1);
                for (int i = 0; i < objCount; i++)
                {
                    objLerps[i] = interval * i;
                }
                break;
        }
        

        for (int i = 0; i < objCount; i++)
        {
            var targetPos = Vector3.Lerp(leftTr.position, rightTr.position, objLerps[i]);
            var targetRot = Quaternion.identity;
            if (objCount >= 4)
            {
                float curve = Mathf.Sqrt(Mathf.Pow(height, 2) - Mathf.Pow(objLerps[i] - 0.5f, 2));
                curve = height >= 0 ? curve : -curve;
                targetPos.z += curve;
                targetRot = Quaternion.Slerp(leftTr.rotation, rightTr.rotation, objLerps[i]);
            }
            results.Add(new RPS(targetPos, targetRot, scale));
        }

        return results;
    }
}
