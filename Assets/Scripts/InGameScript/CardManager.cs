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

       
    }

    


    public void CardMouseOver(Card card)
    {
        EnlargeCard(true, card);
    }
    public void CardMouseExit(Card card)
    {
        EnlargeCard(false, card);
    }

    void EnlargeCard(bool isEnlarge, Card card)
    {
        if (isEnlarge)
        {
            Vector3 enlargePos = new Vector3(card.originRPS.pos.x, 10.0f, -7f);
            card.MoveTransform(new RPS(enlargePos, Quaternion.identity, card.originRPS.scale * 2), false);
        }
        else
        {
            card.MoveTransform(card.originRPS, false);
        }
        card.SetMostFrontOrder(isEnlarge);
    }
}
