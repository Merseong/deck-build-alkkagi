using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    [SerializeField] private CardData cardData;
    // public CardData CardData { set { cardData = value; } }
    public CardData CardData => cardData;
}
