using UnityEngine;

[CreateAssetMenu(fileName = "Card Data",menuName = "Scriptable Object/Card Data")]
public class CardData : ScriptableObject
{
    public void SetCost(CardData cardData, int cardCost)
    {
        this.cardID = cardData.cardID;
        this.cardName = cardData.cardName;
        this.cardEngName = cardData.cardEngName;
        this.stoneSize = cardData.stoneSize;
        this.stoneWeight = cardData.stoneWeight;
        this.inDeckNumber = cardData.inDeckNumber;
        this.description = cardData.description;
        this.cardCost = cardCost;
    }

    // 고유ID
    [SerializeField]
    private int cardID;
    public int CardID => cardID;
    public string cardName;
    public string cardEngName;
    // 크기
    public enum StoneSize
    {
        Small,
        Medium,
        Large,
        SuperLarge
    }
    public StoneSize stoneSize;
    // 무게
    public enum StoneWeight
    {
        Light,
        Standard,
        Heavy
    }
    public StoneWeight stoneWeight;
    // 비용
    public int cardCost;
    // 덱 포함 개수
    public int inDeckNumber;
    // 설명
    public string description;
}
